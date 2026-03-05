using FluentAssertions;
using MannaHp.PrintAgent;
using MannaHp.PrintAgent.Data;
using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace MannaHp.PrintAgent.Tests;

public class WorkerTests : IDisposable
{
    private readonly string _outputDir;

    public WorkerTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _outputDir = Path.Combine(Path.GetTempPath(), $"manna-worker-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, recursive: true);
    }

    private static PrintAgentDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PrintAgentDbContext>()
            .UseInMemoryDatabase($"worker-test-{Guid.NewGuid():N}")
            .Options;
        return new PrintAgentDbContext(options);
    }

    private static Order MakeOrder(int orderNumber, bool printed = false)
    {
        var menuItem = new MenuItem { Id = Guid.NewGuid(), Name = "Latte", IsCustomizable = false };
        var variant = new MenuItemVariant { Id = Guid.NewGuid(), Name = "12oz", Price = 4.75m, MenuItem = menuItem, MenuItemId = menuItem.Id };

        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            Status = OrderStatus.Received,
            PaymentMethod = PaymentMethod.InStore,
            PaymentStatus = PaymentStatus.Pending,
            Subtotal = 4.75m,
            TaxRate = 0.0825m,
            Tax = 0.39m,
            Total = 5.14m,
            Printed = printed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items =
            [
                new OrderItem
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = menuItem.Id,
                    MenuItem = menuItem,
                    VariantId = variant.Id,
                    Variant = variant,
                    Quantity = 1,
                    UnitPrice = 4.75m,
                    TotalPrice = 4.75m,
                    Ingredients = []
                }
            ]
        };
    }

    [Fact]
    public void InMemoryDb_CanAddAndQueryOrders()
    {
        using var db = CreateInMemoryDb();
        var order = MakeOrder(1001);
        db.Orders.Add(order);
        db.SaveChanges();

        var unprinted = db.Orders.Where(o => !o.Printed).ToList();
        unprinted.Should().HaveCount(1);
    }

    [Fact]
    public void InMemoryDb_SkipsPrintedOrders()
    {
        using var db = CreateInMemoryDb();
        db.Orders.Add(MakeOrder(1001, printed: true));
        db.Orders.Add(MakeOrder(1002, printed: false));
        db.SaveChanges();

        var unprinted = db.Orders.Where(o => !o.Printed).ToList();
        unprinted.Should().HaveCount(1);
        unprinted[0].OrderNumber.Should().Be(1002);
    }

    [Fact]
    public void InMemoryDb_EmptyResultSet_ReturnsEmpty()
    {
        using var db = CreateInMemoryDb();
        var unprinted = db.Orders.Where(o => !o.Printed).ToList();
        unprinted.Should().BeEmpty();
    }

    [Fact]
    public void InMemoryDb_MarkAsPrinted_UpdatesCorrectly()
    {
        using var db = CreateInMemoryDb();
        var order = MakeOrder(1001);
        db.Orders.Add(order);
        db.SaveChanges();

        order.Printed = true;
        order.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();

        var unprinted = db.Orders.Where(o => !o.Printed).ToList();
        unprinted.Should().BeEmpty();
    }

    [Fact]
    public void InMemoryDb_MultipleUnprintedOrders_AllReturned()
    {
        using var db = CreateInMemoryDb();
        db.Orders.Add(MakeOrder(1001));
        db.Orders.Add(MakeOrder(1002));
        db.Orders.Add(MakeOrder(1003));
        db.SaveChanges();

        var unprinted = db.Orders.Where(o => !o.Printed).ToList();
        unprinted.Should().HaveCount(3);
    }

    [Fact]
    public void ReceiptDocument_DoesNotThrow_WhenGeneratingPdf()
    {
        var order = MakeOrder(1001);
        var settings = new Dictionary<string, string>
        {
            ["StoreName"] = "Test Store",
            ["StoreAddress"] = "123 Test St",
            ["StoreCity"] = "TestCity, OK 73000",
            ["StorePhone"] = "(555) 555-5555",
        };

        var act = () =>
        {
            var doc = new ReceiptDocument(order, settings);
            doc.GeneratePdf();
        };

        act.Should().NotThrow();
    }
}
