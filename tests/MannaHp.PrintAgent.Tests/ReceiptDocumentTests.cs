using FluentAssertions;
using MannaHp.PrintAgent;
using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace MannaHp.PrintAgent.Tests;

public class ReceiptDocumentTests
{
    private static readonly Dictionary<string, string> DefaultSettings = new()
    {
        ["StoreName"] = "Manna + HP",
        ["StoreAddress"] = "317 S Main St",
        ["StoreCity"] = "Lindsay, OK 73052",
        ["StorePhone"] = "(405) 208-2271",
        ["ReceiptFooter"] = "Thank you for dining with us!",
    };

    public ReceiptDocumentTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static Order MakeFixedItemOrder()
    {
        var latte = new MenuItem { Id = Guid.NewGuid(), Name = "Latte", IsCustomizable = false };
        var variant = new MenuItemVariant { Id = Guid.NewGuid(), Name = "12oz", Price = 4.75m, MenuItem = latte };

        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = 1042,
            Status = OrderStatus.Received,
            PaymentMethod = PaymentMethod.Card,
            PaymentStatus = PaymentStatus.Paid,
            CardBrand = "Visa",
            CardLast4 = "4242",
            Subtotal = 4.75m,
            TaxRate = 0.0825m,
            Tax = 0.39m,
            Total = 5.14m,
            Printed = false,
            CreatedAt = new DateTime(2026, 2, 6, 16, 30, 0, DateTimeKind.Utc),
            UpdatedAt = DateTime.UtcNow,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = latte.Id,
                    MenuItem = latte,
                    VariantId = variant.Id,
                    Variant = variant,
                    Quantity = 1,
                    UnitPrice = 4.75m,
                    TotalPrice = 4.75m,
                    Notes = "extra hot, oat milk",
                    Ingredients = []
                }
            ]
        };
    }

    private static Order MakeBowlOrder()
    {
        var bowl = new MenuItem { Id = Guid.NewGuid(), Name = "Burrito Bowl", IsCustomizable = true };
        var rice = new Ingredient { Id = Guid.NewGuid(), Name = "Jasmine Rice" };
        var chicken = new Ingredient { Id = Guid.NewGuid(), Name = "Chicken" };

        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = 1043,
            Status = OrderStatus.Received,
            PaymentMethod = PaymentMethod.InStore,
            PaymentStatus = PaymentStatus.Pending,
            Subtotal = 6.00m,
            TaxRate = 0.0825m,
            Tax = 0.50m,
            Total = 6.50m,
            Printed = false,
            CreatedAt = new DateTime(2026, 2, 6, 17, 0, 0, DateTimeKind.Utc),
            UpdatedAt = DateTime.UtcNow,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = bowl.Id,
                    MenuItem = bowl,
                    VariantId = null,
                    Variant = null,
                    Quantity = 1,
                    UnitPrice = 6.00m,
                    TotalPrice = 6.00m,
                    Notes = "Dad's Bowl",
                    Ingredients =
                    [
                        new OrderItemIngredient
                        {
                            Id = Guid.NewGuid(), IngredientId = rice.Id, Ingredient = rice,
                            QuantityUsed = 10m, PriceCharged = 3.00m
                        },
                        new OrderItemIngredient
                        {
                            Id = Guid.NewGuid(), IngredientId = chicken.Id, Ingredient = chicken,
                            QuantityUsed = 6m, PriceCharged = 3.00m
                        },
                    ]
                }
            ]
        };
    }

    private static string ExtractPdfText(byte[] pdfBytes)
    {
        using var doc = PdfDocument.Open(pdfBytes);
        return string.Join(" ", doc.GetPages().Select(p => p.Text));
    }

    // ── Tests ──────────────────────────────────────────────────────────

    [Fact]
    public void GeneratesPdf_NonNullNonEmpty()
    {
        var order = MakeFixedItemOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);

        var bytes = doc.GeneratePdf();
        bytes.Should().NotBeNull();
        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public void Receipt_ContainsOrderNumber()
    {
        var order = MakeFixedItemOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);
        var text = ExtractPdfText(doc.GeneratePdf());

        text.Should().Contain("1042");
    }

    [Fact]
    public void Receipt_RendersFixedItem_WithVariantAndPrice()
    {
        var order = MakeFixedItemOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);
        var text = ExtractPdfText(doc.GeneratePdf());

        text.Should().Contain("Latte");
        text.Should().Contain("12oz");
        text.Should().Contain("$4.75");
    }

    [Fact]
    public void Receipt_RendersCustomizableItem_WithIngredientBreakdown()
    {
        var order = MakeBowlOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);
        var text = ExtractPdfText(doc.GeneratePdf());

        text.Should().Contain("Burrito Bowl");
        text.Should().Contain("Jasmine Rice");
        text.Should().Contain("Chicken");
        text.Should().Contain("$3.00");
    }

    [Fact]
    public void Receipt_RendersItemNotes()
    {
        var order = MakeFixedItemOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);
        var text = ExtractPdfText(doc.GeneratePdf());

        text.Should().Contain("extra hot, oat milk");
    }

    [Fact]
    public void Receipt_ShowsSubtotalTaxAndTotal()
    {
        var order = MakeFixedItemOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);
        var text = ExtractPdfText(doc.GeneratePdf());

        text.Should().Contain("$4.75");
        text.Should().Contain("$0.39");
        text.Should().Contain("$5.14");
        text.Should().Contain("8.25%");
    }

    [Fact]
    public void Receipt_ShowsCardPaymentInfo()
    {
        var order = MakeFixedItemOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);
        var text = ExtractPdfText(doc.GeneratePdf());

        text.Should().Contain("Visa");
        text.Should().Contain("4242");
    }

    [Fact]
    public void Receipt_ShowsPayAtCounter_ForInStoreOrders()
    {
        var order = MakeBowlOrder();
        var doc = new ReceiptDocument(order, DefaultSettings);
        var text = ExtractPdfText(doc.GeneratePdf());

        text.Should().Contain("Pay at Counter");
    }
}
