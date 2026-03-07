using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Data;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MannaHp.Server.Tests.SignalR;

[Collection("Api")]
public class LowStockAlertTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Seed IDs for Tortilla Chips (fixed item with recipe ingredients)
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");

    public LowStockAlertTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateStoreTokenClient();
    }

    private HubConnection CreateHubConnection()
    {
        var server = _factory.Server;
        return new HubConnectionBuilder()
            .WithUrl($"{server.BaseAddress}hubs/orders", o =>
            {
                o.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
    }

    private async Task<OrderDto> CreateInStoreOrderAsync()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]);
        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        return result!.Order;
    }

    /// <summary>
    /// Set an ingredient's stock near its threshold so completing an order triggers the alert.
    /// </summary>
    private async Task SetIngredientStockNearThreshold(Guid ingredientId, decimal stock)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var ingredient = await db.Ingredients.FindAsync(ingredientId);
        if (ingredient is not null)
        {
            ingredient.StockQuantity = stock;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get the ingredient IDs used in the recipe for Tortilla Chips.
    /// </summary>
    private async Task<List<Guid>> GetChipsRecipeIngredientIds()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MannaDbContext>();
        var variant = await db.MenuItemVariants
            .Include(v => v.RecipeIngredients)
            .FirstAsync(v => v.Id == VChips);
        return variant.RecipeIngredients.Select(r => r.IngredientId).ToList();
    }

    [Fact]
    public async Task CompletingOrder_ThatDropsIngredientBelowThreshold_TriggersLowStockAlert()
    {
        // Get recipe ingredients and set stock near threshold
        var recipeIngredientIds = await GetChipsRecipeIngredientIds();
        foreach (var ingId in recipeIngredientIds)
        {
            // Set stock just above threshold so decrement drops it below
            await SetIngredientStockNearThreshold(ingId, 5m);
        }

        await using var hub = CreateHubConnection();
        var received = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<object>("LowStockAlert", alert => received.TrySetResult(alert));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinKitchen");

        // Create and complete order
        var order = await CreateInStoreOrderAsync();
        var staffClient = await _factory.CreateStaffClientAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));

        var notified = await Task.WhenAny(received.Task, Task.Delay(5000));
        notified.Should().Be(received.Task, "kitchen should receive LowStockAlert within 5s");
    }

    [Fact]
    public async Task CompletingOrder_ThatDoesNotDropBelowThreshold_DoesNotTriggerLowStockAlert()
    {
        // Set stock very high so decrement won't drop below threshold
        var recipeIngredientIds = await GetChipsRecipeIngredientIds();
        foreach (var ingId in recipeIngredientIds)
        {
            await SetIngredientStockNearThreshold(ingId, 10000m);
        }

        await using var hub = CreateHubConnection();
        var received = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<object>("LowStockAlert", alert => received.TrySetResult(alert));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinKitchen");

        var order = await CreateInStoreOrderAsync();
        var staffClient = await _factory.CreateStaffClientAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));

        // Should NOT receive LowStockAlert
        var notified = await Task.WhenAny(received.Task, Task.Delay(2000));
        notified.Should().NotBe(received.Task, "should not receive LowStockAlert when stock stays above threshold");
    }

    [Fact]
    public async Task LowStockAlert_IncludesLowStockCount()
    {
        var recipeIngredientIds = await GetChipsRecipeIngredientIds();
        foreach (var ingId in recipeIngredientIds)
        {
            await SetIngredientStockNearThreshold(ingId, 5m);
        }

        await using var hub = CreateHubConnection();
        var received = new TaskCompletionSource<dynamic>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<dynamic>("LowStockAlert", alert => received.TrySetResult(alert));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinKitchen");

        var order = await CreateInStoreOrderAsync();
        var staffClient = await _factory.CreateStaffClientAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));

        var notified = await Task.WhenAny(received.Task, Task.Delay(5000));
        notified.Should().Be(received.Task, "should receive LowStockAlert");

        // The alert should have a lowStockCount property
        var alert = await received.Task;
        var json = System.Text.Json.JsonSerializer.Serialize(alert);
        json.Should().Contain("lowStockCount", "alert should include lowStockCount");
    }
}
