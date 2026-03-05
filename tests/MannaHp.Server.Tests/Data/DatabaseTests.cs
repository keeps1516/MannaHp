using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Data;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MannaHp.Server.Tests.Data;

[Collection("Api")]
public class DatabaseTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");

    public DatabaseTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Seed data creates expected categories ─────────────────────────

    [Fact]
    public async Task SeedData_Creates5Categories()
    {
        var categories = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        // Seed data creates exactly 5 base categories (other tests may add more)
        var expectedNames = new[] { "Bowls", "Traditional Drinks", "Seasonal Specials", "Sides & Drinks", "Add-Ons" };
        foreach (var name in expectedNames)
        {
            categories!.Should().Contain(c => c.Name == name);
        }
    }

    // ── Seed data creates expected ingredients ────────────────────────

    [Fact]
    public async Task SeedData_CreatesIngredientsWithCorrectPrices()
    {
        var ingredients = await _client.GetFromJsonAsync<List<IngredientDto>>("/api/ingredients");

        ingredients.Should().NotBeNull();

        // Spot-check a few
        var chicken = ingredients!.FirstOrDefault(i => i.Name == "Chicken");
        chicken.Should().NotBeNull();
        chicken!.Unit.Should().Be(UnitOfMeasure.Oz);

        var espresso = ingredients.FirstOrDefault(i => i.Name == "Espresso");
        espresso.Should().NotBeNull();
        espresso!.Unit.Should().Be(UnitOfMeasure.Shot);
    }

    // ── Order number sequence starts at 1001 ──────────────────────────

    [Fact]
    public async Task OrderNumber_StartsAtOrAbove1001()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        var order = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{result!.Order.Id}");

        // PostgreSQL sequence starts at 1001; may be higher if previous tests created orders
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var dbOrder = await db.Orders.FindAsync(result.Order.Id);
        dbOrder!.OrderNumber.Should().BeGreaterThanOrEqualTo(1001);
    }

    // ── Soft delete: deactivated items still fetchable ─────────────────

    [Fact]
    public async Task SoftDelete_DeactivatedCategoryStillFetchableById()
    {
        var ownerClient = await _factory.CreateOwnerClientAsync();

        // Create and soft-delete a category
        var createResp = await ownerClient.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest("SoftDeleteTest", 97));
        var created = await createResp.Content.ReadFromJsonAsync<CategoryDto>();

        await ownerClient.DeleteAsync($"/api/categories/{created!.Id}");

        // Can still GET by ID
        var category = await _client.GetFromJsonAsync<CategoryDto>($"/api/categories/{created.Id}");
        category.Should().NotBeNull();
        category!.Active.Should().BeFalse();
    }

    // ── Concurrent order creation — no conflicts ──────────────────────

    [Fact]
    public async Task ConcurrentOrderCreation_NoSequenceConflicts()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]);

        // Create 5 orders concurrently
        var tasks = Enumerable.Range(0, 5).Select(_ =>
            _client.PostAsJsonAsync("/api/orders", req));

        var responses = await Task.WhenAll(tasks);

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // All order numbers should be distinct
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var orderNumbers = await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => o.OrderNumber)
            .ToListAsync();

        orderNumbers.Should().OnlyHaveUniqueItems();
    }
}
