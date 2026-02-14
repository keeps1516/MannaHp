using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class OrderEndpointsTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // ── Known seed GUIDs (from SeedData.cs) ──────────────────────────
    // Menu items
    private static readonly Guid MiBowl = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    private static readonly Guid MiLatte = Guid.Parse("c0000000-0009-0000-0000-000000000009");
    private static readonly Guid MiEspShot = Guid.Parse("c0000000-0022-0000-0000-000000000034");
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");

    // Variants
    private static readonly Guid VLatte12 = Guid.Parse("d0000000-0011-0000-0000-000000000017");
    private static readonly Guid VLatte16 = Guid.Parse("d0000000-0012-0000-0000-000000000018");
    private static readonly Guid VEspShot = Guid.Parse("d0000000-0041-0000-0000-000000000065");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");

    // Available ingredients for bowl (e0 prefix, seq 1-10)
    private static readonly Guid AvailRice = Guid.Parse("e0000000-0001-0000-0000-000000000000");      // $3.00
    private static readonly Guid AvailBeans = Guid.Parse("e0000000-0002-0000-0000-000000000000");     // $2.00
    private static readonly Guid AvailChicken = Guid.Parse("e0000000-0004-0000-0000-000000000000");   // $3.00
    private static readonly Guid AvailLettuce = Guid.Parse("e0000000-0006-0000-0000-000000000000");   // $0.50
    private static readonly Guid AvailSalsa = Guid.Parse("e0000000-0009-0000-0000-000000000000");     // $0.50
    private static readonly Guid AvailCheese = Guid.Parse("e0000000-0010-0000-0000-000000000000");    // $0.50

    private const decimal TaxRate = 0.0825m;

    public OrderEndpointsTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static CreateOrderRequest FixedOrder(Guid menuItemId, Guid variantId, int qty = 1) =>
        new(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(menuItemId, variantId, qty, null, null)]);

    private static CreateOrderRequest BowlOrder(List<Guid> ingredientIds, int qty = 1) =>
        new(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiBowl, null, qty, null, ingredientIds)]);

    // ── POST — Fixed item pricing ───────────────────────────────────

    [Fact]
    public async Task Post_FixedItem_PriceFromVariant()
    {
        // Latte 12oz = $4.75
        var response = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiLatte, VLatte12));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items.Should().HaveCount(1);
        order.Items[0].UnitPrice.Should().Be(4.75m);
        order.Items[0].TotalPrice.Should().Be(4.75m);
    }

    [Fact]
    public async Task Post_FixedItem_16oz_CorrectPrice()
    {
        // Latte 16oz = $5.25
        var response = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiLatte, VLatte16));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items[0].UnitPrice.Should().Be(5.25m);
    }

    [Fact]
    public async Task Post_FixedItem_Qty2_DoublesLineTotal()
    {
        // Latte 12oz × 2 = $9.50
        var response = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiLatte, VLatte12, qty: 2));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items[0].UnitPrice.Should().Be(4.75m);
        order.Items[0].TotalPrice.Should().Be(9.50m);
    }

    [Fact]
    public async Task Post_FixedItem_WithoutVariant_Returns400()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiLatte, null, 1, null, null)]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST — Customizable item pricing ────────────────────────────

    [Fact]
    public async Task Post_CustomizableItem_PriceIsSumOfIngredients()
    {
        // Rice $3 + Chicken $3 + Lettuce $0.50 = $6.50
        var response = await _client.PostAsJsonAsync("/api/orders",
            BowlOrder([AvailRice, AvailChicken, AvailLettuce]));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items[0].UnitPrice.Should().Be(6.50m);
        order.Items[0].TotalPrice.Should().Be(6.50m);
    }

    [Fact]
    public async Task Post_CustomizableItem_AllToppings()
    {
        // Rice $3 + Beans $2 + Chicken $3 + Lettuce $0.50 + Salsa $0.50 + Cheese $0.50 = $9.50
        var response = await _client.PostAsJsonAsync("/api/orders",
            BowlOrder([AvailRice, AvailBeans, AvailChicken, AvailLettuce, AvailSalsa, AvailCheese]));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items[0].UnitPrice.Should().Be(9.50m);
    }

    [Fact]
    public async Task Post_CustomizableItem_Qty2_DoublesBowlPrice()
    {
        // (Rice $3 + Chicken $3) × 2 = $12.00
        var response = await _client.PostAsJsonAsync("/api/orders",
            BowlOrder([AvailRice, AvailChicken], qty: 2));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items[0].UnitPrice.Should().Be(6.00m);
        order.Items[0].TotalPrice.Should().Be(12.00m);
    }

    [Fact]
    public async Task Post_CustomizableItem_InvalidIngredient_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/orders",
            BowlOrder([AvailRice, Guid.NewGuid()]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_CustomizableItem_ReturnsIngredientDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/orders",
            BowlOrder([AvailRice, AvailChicken]));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items[0].Ingredients.Should().NotBeNull();
        order.Items[0].Ingredients!.Should().HaveCount(2);
        order.Items[0].Ingredients!.Should().Contain(i => i.IngredientName == "Jasmine Rice" && i.PriceCharged == 3.00m);
        order.Items[0].Ingredients!.Should().Contain(i => i.IngredientName == "Chicken" && i.PriceCharged == 3.00m);
    }

    // ── POST — Mixed order with tax ─────────────────────────────────

    [Fact]
    public async Task Post_MixedOrder_CorrectSubtotalTaxTotal()
    {
        // Bowl: Rice $3 + Chicken $3 = $6.00
        // Latte 12oz: $4.75
        // Espresso Shot: $1.00
        // Subtotal: $11.75
        // Tax: $11.75 × 0.0825 = $0.97 (rounded)
        // Total: $12.72
        var req = new CreateOrderRequest(PaymentMethod.InStore, "Mixed order test",
        [
            new CreateOrderItemRequest(MiBowl, null, 1, null, [AvailRice, AvailChicken]),
            new CreateOrderItemRequest(MiLatte, VLatte12, 1, null, null),
            new CreateOrderItemRequest(MiEspShot, VEspShot, 1, null, null)
        ]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Items.Should().HaveCount(3);
        order.Subtotal.Should().Be(11.75m);
        order.TaxRate.Should().Be(TaxRate);
        order.Tax.Should().Be(Math.Round(11.75m * TaxRate, 2));  // 0.97
        order.Total.Should().Be(11.75m + Math.Round(11.75m * TaxRate, 2));  // 12.72
        order.Notes.Should().Be("Mixed order test");
    }

    [Fact]
    public async Task Post_Order_StatusIsReceived()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiChips, VChips));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Status.Should().Be(OrderStatus.Received);
        order.PaymentMethod.Should().Be(PaymentMethod.InStore);
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    // ── POST — Validation failures ──────────────────────────────────

    [Fact]
    public async Task Post_EmptyItems_Returns400()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null, []);
        var response = await _client.PostAsJsonAsync("/api/orders", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NonexistentMenuItemId_Returns400()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(Guid.NewGuid(), VLatte12, 1, null, null)]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WrongVariantForMenuItem_Returns400()
    {
        // VLatte12 belongs to MiLatte, not MiChips
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiChips, VLatte12, 1, null, null)]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/orders/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsFullOrder()
    {
        // Create an order first
        var createResponse = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiLatte, VLatte12));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Fetch it
        var order = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{created!.Id}");

        order.Should().NotBeNull();
        order!.Id.Should().Be(created.Id);
        order.Items.Should().HaveCount(1);
        order.Items[0].MenuItemName.Should().Be("Latte");
        order.Items[0].VariantName.Should().Be("12oz");
    }

    [Fact]
    public async Task GetById_NonexistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/orders/active ──────────────────────────────────────

    [Fact]
    public async Task GetActive_ExcludesCompletedAndCancelled()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        // Create an order, then mark it completed
        var createResponse = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiChips, VChips));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        await staffClient.PatchAsJsonAsync($"/api/orders/{created!.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));

        // Create another active order
        var activeResponse = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiEspShot, VEspShot));
        var active = await activeResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Fetch active orders (requires Staff auth)
        var orders = await staffClient.GetFromJsonAsync<List<OrderDto>>("/api/orders/active");

        orders.Should().NotBeNull();
        orders!.Should().NotContain(o => o.Id == created.Id);
        orders.Should().Contain(o => o.Id == active!.Id);
    }

    // ── PATCH /api/orders/{id}/status ───────────────────────────────

    [Fact]
    public async Task PatchStatus_UpdatesOrderStatus()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        // Create an order
        var createResponse = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiLatte, VLatte16));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Update status to Preparing (requires Staff auth)
        var patchResponse = await staffClient.PatchAsJsonAsync($"/api/orders/{created!.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify via GET (anonymous)
        var order = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{created.Id}");
        order!.Status.Should().Be(OrderStatus.Preparing);
    }

    [Fact]
    public async Task PatchStatus_NonexistentId_Returns404()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var response = await staffClient.PatchAsJsonAsync($"/api/orders/{Guid.NewGuid()}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchStatus_InvalidStatus_Returns400()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/orders", FixedOrder(MiLatte, VLatte12));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var response = await staffClient.PatchAsJsonAsync($"/api/orders/{created!.Id}/status",
            new UpdateOrderStatusRequest((OrderStatus)999));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
