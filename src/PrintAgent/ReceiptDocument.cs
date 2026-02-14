using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MannaHp.PrintAgent;

public class ReceiptDocument : IDocument
{
	private readonly Order _order;
	private readonly Dictionary<string, string> _storeSettings;

	// Receipt paper width: 80mm thermal printer ≈ 216 points
	private const float PageWidth = 216f;

	public ReceiptDocument(Order order, Dictionary<string, string> storeSettings)
	{
		_order = order;
		_storeSettings = storeSettings;
	}

	public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

	public void Compose(IDocumentContainer container)
	{
		container.Page(page =>
		{
			page.ContinuousSize(PageWidth, Unit.Point);
			page.MarginHorizontal(8);
			page.MarginVertical(12);
			page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Courier New"));

			page.Content().Column(col =>
			{
				col.Spacing(2);

				ComposeHeader(col);
				ComposeDivider(col);
				ComposeItems(col);
				ComposeDivider(col);
				ComposeTotals(col);
				ComposeFooter(col);
			});
		});
	}

	private void ComposeHeader(ColumnDescriptor col)
	{
		var storeName = GetSetting("StoreName", "Manna + HP");
		var storeAddress = GetSetting("StoreAddress", "317 S Main St");
		var storeCity = GetSetting("StoreCity", "Lindsay, OK 73052");
		var storePhone = GetSetting("StorePhone", "(405) 208-2271");

		col.Item().AlignCenter().Text(storeName).Bold().FontSize(14);
		col.Item().AlignCenter().Text(storeAddress).FontSize(8);
		col.Item().AlignCenter().Text(storeCity).FontSize(8);
		col.Item().AlignCenter().Text(storePhone).FontSize(8);
		var localTime = TimeZoneInfo.ConvertTimeFromUtc(
			DateTime.SpecifyKind(_order.CreatedAt, DateTimeKind.Utc),
			TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
		col.Item().PaddingTop(4).AlignCenter().Text($"Date: {localTime:M/d/yyyy h:mm tt}").FontSize(8);
		col.Item().AlignCenter().Text($"Order #{_order.OrderNumber}").FontSize(8);
	}

	private void ComposeDivider(ColumnDescriptor col)
	{
		col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Black);
	}

	private void ComposeItems(ColumnDescriptor col)
	{
		foreach (var item in _order.Items)
		{
			var menuItemName = item.MenuItem?.Name ?? "Item";
			var variantName = item.Variant?.Name;
			var isCustomizable = item.MenuItem?.IsCustomizable ?? false;

			// Bowl name from notes (displayed as quoted prefix)
			if (isCustomizable && !string.IsNullOrWhiteSpace(item.Notes))
			{
				col.Item().Text($"\"{item.Notes}\" — {menuItemName}").Bold().FontSize(9);
			}
			else if (!string.IsNullOrWhiteSpace(variantName) && variantName != "Regular")
			{
				col.Item().Text($"{menuItemName} ({variantName})").Bold().FontSize(9);
			}
			else
			{
				col.Item().Text(menuItemName).Bold().FontSize(9);
			}

			// Ingredients / add-ons
			if (item.Ingredients.Count > 0)
			{
				foreach (var ingredient in item.Ingredients)
				{
					var ingredientName = ingredient.Ingredient?.Name ?? "Ingredient";
					var price = ingredient.PriceCharged;
					var prefix = isCustomizable ? "  " : "  + ";

					col.Item().Row(row =>
					{
						row.RelativeItem().Text($"{prefix}{ingredientName}").FontSize(8);
						row.ConstantItem(50).AlignRight().Text($"${price:F2}").FontSize(8);
					});
				}
			}

			// Item total line
			col.Item().Row(row =>
			{
				row.RelativeItem().AlignRight().Text("Item:").FontSize(8);
				row.ConstantItem(50).AlignRight().Text($"${item.TotalPrice:F2}").FontSize(8);
			});

			// Item notes (non-bowl)
			if (!isCustomizable && !string.IsNullOrWhiteSpace(item.Notes))
			{
				col.Item().Text($"  Note: {item.Notes}").FontSize(7).Italic();
			}

			col.Item().PaddingBottom(4);
		}
	}

	private void ComposeTotals(ColumnDescriptor col)
	{
		var taxPercent = _order.TaxRate * 100;

		ComposeTotalLine(col, "Subtotal:", $"${_order.Subtotal:F2}");
		ComposeTotalLine(col, $"Tax ({taxPercent:F2}%):", $"${_order.Tax:F2}");
		col.Item().PaddingTop(2);
		ComposeTotalLine(col, "Total:", $"${_order.Total:F2}", bold: true);

		// Payment method
		col.Item().PaddingTop(4);
		var paymentText = _order.PaymentMethod switch
		{
			PaymentMethod.Card when !string.IsNullOrEmpty(_order.CardBrand) =>
				$"Payment: {_order.CardBrand} ***{_order.CardLast4}",
			PaymentMethod.Card => "Payment: Card",
			PaymentMethod.InStore => "Payment: Pay at Counter",
			_ => "Payment: Unknown"
		};
		col.Item().Text(paymentText).FontSize(8);
	}

	private void ComposeTotalLine(ColumnDescriptor col, string label, string value, bool bold = false)
	{
		col.Item().Row(row =>
		{
			row.RelativeItem().AlignRight().Text(text =>
			{
				var span = text.Span(label).FontSize(9);
				if (bold) span.Bold();
			});
			row.ConstantItem(60).AlignRight().Text(text =>
			{
				var span = text.Span(value).FontSize(9);
				if (bold) span.Bold();
			});
		});
	}

	private void ComposeFooter(ColumnDescriptor col)
	{
		var footer = GetSetting("ReceiptFooter", "Thank you for dining with us!");

		col.Item().PaddingTop(8);
		ComposeDivider(col);
		col.Item().AlignCenter().Text(footer).FontSize(8).Italic();
	}

	private string GetSetting(string key, string fallback)
		=> _storeSettings.TryGetValue(key, out var value) ? value : fallback;
}
