using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class AuthorizationTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid CatBowls = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid MiBowl = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");

    public AuthorizationTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Staff cannot manage categories ────────────────────────────────

    [Fact]
    public async Task Staff_CreateCategory_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var response = await staffClient.PostAsJsonAsync("/api/categories",
            new CreateCategoryRequest("Staff Cat", 99));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_UpdateCategory_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var response = await staffClient.PutAsJsonAsync($"/api/categories/{CatBowls}",
            new UpdateCategoryRequest("Hacked", 1, true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_DeleteCategory_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var response = await staffClient.DeleteAsync($"/api/categories/{CatBowls}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Staff cannot manage ingredients ───────────────────────────────

    [Fact]
    public async Task Staff_CreateIngredient_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var response = await staffClient.PostAsJsonAsync("/api/ingredients",
            new CreateIngredientRequest("Staff Ing", UnitOfMeasure.Oz, 1m, 10m, 5m));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_UpdateIngredient_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var ingId = Guid.Parse("b0000000-0001-0000-0000-000000000001");
        var response = await staffClient.PutAsJsonAsync($"/api/ingredients/{ingId}",
            new UpdateIngredientRequest("Hacked", UnitOfMeasure.Oz, 99m, 99m, 99m, true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_DeleteIngredient_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var ingId = Guid.Parse("b0000000-0001-0000-0000-000000000001");
        var response = await staffClient.DeleteAsync($"/api/ingredients/{ingId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Staff cannot manage menu items ────────────────────────────────

    [Fact]
    public async Task Staff_CreateMenuItem_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var response = await staffClient.PostAsJsonAsync("/api/menu-items",
            new CreateMenuItemRequest(CatBowls, "Staff Item", null, null, false, false, 99));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_CreateVariant_Returns403()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var response = await staffClient.PostAsJsonAsync(
            $"/api/menu-items/{MiBowl}/variants",
            new CreateVariantRequest("Staff Variant", 5.00m, 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Anonymous cannot view active orders or update status ──────────

    [Fact]
    public async Task Anonymous_GetActiveOrders_Returns401()
    {
        var response = await _client.GetAsync("/api/orders/active");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_UpdateOrderStatus_Returns401()
    {
        // Create an order first (this is allowed anonymously)
        var createResp = await _client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest(PaymentMethod.InStore, null,
                [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]));
        var order = (await createResp.Content.ReadFromJsonAsync<CreateOrderResponse>())!.Order;

        // Try to update status without auth
        var response = await _client.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
