using MannaHp.PrintAgent.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Sentry;

namespace MannaHp.PrintAgent;

public class Worker(
	ILogger<Worker> logger,
	IServiceScopeFactory scopeFactory,
	IConfiguration config) : BackgroundService
{
	private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(
		config.GetValue("PrintAgent:PollIntervalSeconds", 5));

	private readonly string _outputDir = string.IsNullOrWhiteSpace(config.GetValue<string>("PrintAgent:OutputDirectory"))
		? Path.Combine(AppContext.BaseDirectory, "receipts")
		: config.GetValue<string>("PrintAgent:OutputDirectory")!;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		QuestPDF.Settings.License = LicenseType.Community;

		Directory.CreateDirectory(_outputDir);
		logger.LogInformation("PrintAgent started. Polling every {Interval}s. Output: {Dir}",
			_pollInterval.TotalSeconds, _outputDir);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await ProcessUnprintedOrders(stoppingToken);
			}
			catch (Exception ex)
			{
				SentrySdk.CaptureException(ex);
				logger.LogError(ex, "Error processing unprinted orders");
			}

			await Task.Delay(_pollInterval, stoppingToken);
		}
	}

	private async Task ProcessUnprintedOrders(CancellationToken ct)
	{
		using var scope = scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PrintAgentDbContext>();

		var orders = await db.Orders
			.Where(o => !o.Printed)
			.Include(o => o.Items)
				.ThenInclude(oi => oi.MenuItem)
			.Include(o => o.Items)
				.ThenInclude(oi => oi.Variant)
			.Include(o => o.Items)
				.ThenInclude(oi => oi.Ingredients)
					.ThenInclude(oii => oii.Ingredient)
			.OrderBy(o => o.CreatedAt)
			.ToListAsync(ct);

		if (orders.Count == 0) return;

		// Load store settings once per batch
		var settings = await db.AppSettings
			.ToDictionaryAsync(s => s.Key, s => s.Value, ct);

		foreach (var order in orders)
		{
			try
			{
				var doc = new ReceiptDocument(order, settings);
				var filePath = Path.Combine(_outputDir, $"receipt-{order.OrderNumber}.pdf");

				doc.GeneratePdf(filePath);
				logger.LogInformation("Generated receipt for Order #{OrderNumber} → {Path}",
					order.OrderNumber, filePath);

				// TODO: Send to printer via lpr/System.Drawing.Printing
				// For now, just generate the PDF file

				order.Printed = true;
				order.UpdatedAt = DateTime.UtcNow;
				await db.SaveChangesAsync(ct);

				logger.LogInformation("Order #{OrderNumber} marked as printed", order.OrderNumber);
			}
			catch (Exception ex)
			{
				SentrySdk.CaptureException(ex);
				logger.LogError(ex, "Failed to print receipt for Order #{OrderNumber}", order.OrderNumber);
			}
		}
	}
}
