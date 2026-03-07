using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Data;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class RestockEndpointTests
{
    private readonly MannaApiFactory _factory;
    private readonly HttpClient _client;

    // Seed ingredient ID for Jasmine Rice
    private static readonly Guid JasmineRiceId = Guid.Parse("b0000000-0001-0000-0000-000000000001");

    // Seed IDs for ordering (Tortilla Chips)
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");

    public RestockEndpointTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Restock_AddsToCurrentStock_NotOverwrites()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Get current stock
        var before = await ownerClient.GetFromJsonAsync<IngredientDto>($"/api/ingredients/{JasmineRiceId}");
        var originalStock = before!.StockQuantity;

        // Restock
        var response = await ownerClient.PostAsJsonAsync(
            $"/api/ingredients/{JasmineRiceId}/restock",
            new { Quantity = 50m, Notes = "Test delivery" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await response.Content.ReadFromJsonAsync<IngredientDto>();
        after!.StockQuantity.Should().Be(originalStock + 50);
    }

    [Fact]
    public async Task Restock_CreatesInventoryLog_WithChangeTypeReceived()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        await ownerClient.PostAsJsonAsync(
            $"/api/ingredients/{JasmineRiceId}/restock",
            new { Quantity = 25m, Notes = "Log test" });

        var logs = await ownerClient.GetFromJsonAsync<List<InventoryLogDto>>(
            $"/api/ingredients/{JasmineRiceId}/history");

        logs.Should().Contain(l =>
            l.ChangeType == InventoryChangeType.Received &&
            l.QuantityChange == 25m &&
            l.Notes == "Log test");
    }

    [Fact]
    public async Task BulkRestock_UpdatesMultipleIngredients()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Get a second ingredient ID
        var ingredients = await ownerClient.GetFromJsonAsync<List<IngredientDto>>("/api/ingredients");
        var secondIng = ingredients!.First(i => i.Id != JasmineRiceId);

        var before1 = ingredients.First(i => i.Id == JasmineRiceId).StockQuantity;
        var before2 = secondIng.StockQuantity;

        var response = await ownerClient.PostAsJsonAsync("/api/ingredients/bulk-restock",
            new
            {
                Items = new[]
                {
                    new { IngredientId = JasmineRiceId, Quantity = 10m, Notes = "Bulk 1" },
                    new { IngredientId = secondIng.Id, Quantity = 20m, Notes = "Bulk 2" },
                }
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<List<IngredientDto>>();
        updated.Should().HaveCount(2);
    }

    [Fact]
    public async Task Restock_RejectsNegativeQuantity()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/ingredients/{JasmineRiceId}/restock",
            new { Quantity = -10m });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Restock_RequiresOwnerAuthorization()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var response = await staffClient.PostAsJsonAsync(
            $"/api/ingredients/{JasmineRiceId}/restock",
            new { Quantity = 10m });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompletingOrder_CreatesInventoryLogs_WithChangeTypeOrderDecrement()
    {
        // Create and complete an order
        var order = await CreateAndCompleteOrderAsync();

        // Check that inventory logs were created with OrderDecrement type
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();

        var logs = await db.InventoryLogs
            .Where(l => l.ChangeType == InventoryChangeType.OrderDecrement
                && l.Notes!.Contains(order.OrderNumber.ToString()))
            .ToListAsync();

        logs.Should().NotBeEmpty("completing an order should create inventory log entries");
        logs.Should().OnlyContain(l => l.QuantityChange < 0, "order decrements should be negative");
    }

    [Fact]
    public async Task InventoryLog_RecordsCorrectNewStockQuantitySnapshot()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        var response = await ownerClient.PostAsJsonAsync(
            $"/api/ingredients/{JasmineRiceId}/restock",
            new { Quantity = 77m, Notes = "Snapshot test" });
        var updated = await response.Content.ReadFromJsonAsync<IngredientDto>();

        var logs = await ownerClient.GetFromJsonAsync<List<InventoryLogDto>>(
            $"/api/ingredients/{JasmineRiceId}/history");

        var log = logs!.First(l => l.Notes == "Snapshot test");
        log.NewStockQuantity.Should().Be(updated!.StockQuantity);
    }

    private async Task<OrderDto> CreateAndCompleteOrderAsync()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]);
        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();

        var staffClient = await _factory.CreateStaffClientAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{result!.Order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));

        return result.Order;
    }
}
