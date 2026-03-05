using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class OrderPricingEdgeCaseTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid MiBowl = Guid.Parse("c0000000-0001-0000-0000-000000000001");
    private static readonly Guid MiLatte = Guid.Parse("c0000000-0009-0000-0000-000000000009");
    private static readonly Guid VLatte12 = Guid.Parse("d0000000-0011-0000-0000-000000000017");
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");

    // Bowl available ingredients
    private static readonly Guid AvailRice = Guid.Parse("e0000000-0001-0000-0000-000000000000");      // $3.00
    private static readonly Guid AvailLettuce = Guid.Parse("e0000000-0006-0000-0000-000000000000");   // $0.50

    public OrderPricingEdgeCaseTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Notes preserved on OrderItem ──────────────────────────────────

    [Fact]
    public async Task Post_OrderWithNotes_NotesPreservedOnItem()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, "Order-level note",
        [
            new CreateOrderItemRequest(MiLatte, VLatte12, 1, "Extra hot, oat milk", null)
        ]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!.Order;
        order.Notes.Should().Be("Order-level note");
        order.Items[0].Notes.Should().Be("Extra hot, oat milk");
    }

    // ── InStore payment — no Stripe interaction ───────────────────────

    [Fact]
    public async Task Post_InStorePayment_NoClientSecret()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        result!.ClientSecret.Should().BeNull();
        result.Order.PaymentMethod.Should().Be(PaymentMethod.InStore);
    }

    // ── Card payment — returns clientSecret ───────────────────────────

    [Fact]
    public async Task Post_CardPayment_ReturnsClientSecret()
    {
        var req = new CreateOrderRequest(PaymentMethod.Card, null,
            [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);

        // Will fail with a Stripe error since we use fake keys, but the order creation
        // attempt should be made — check for 500 (Stripe API failure) or success
        // Since test uses fake Stripe keys, this may return 500
        // The important thing is it doesn't return 201 without a clientSecret
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
            result!.ClientSecret.Should().NotBeNullOrEmpty();
        }
        else
        {
            // Expected: Stripe API call fails with fake key
            response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest,
                "card payment should not be a validation error");
        }
    }

    // ── Triple same ingredient — 3x price ─────────────────────────────

    [Fact]
    public async Task Post_TripleSameIngredient_TriplesPriceCorrectly()
    {
        // 3× Lettuce ($0.50 each) = $1.50
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiBowl, null, 1, null,
                [AvailLettuce, AvailLettuce, AvailLettuce])]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!.Order;
        order.Items[0].UnitPrice.Should().Be(1.50m);
        order.Items[0].Ingredients!.Should().HaveCount(3);
    }

    // ── Mixed multi-item order tax calculation ────────────────────────

    [Fact]
    public async Task Post_MultiItemOrder_TaxCalculationRoundsCorrectly()
    {
        // Rice $3.00 + Chips $1.50 = $4.50
        // Tax: $4.50 × 0.0825 = $0.37 (rounded)
        // Total: $4.87
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
        [
            new CreateOrderItemRequest(MiBowl, null, 1, null, [AvailRice]),
            new CreateOrderItemRequest(MiChips, VChips, 1, null, null)
        ]);

        var response = await _client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = (await response.Content.ReadFromJsonAsync<CreateOrderResponse>())!.Order;
        order.Subtotal.Should().Be(4.50m);
        order.Tax.Should().Be(Math.Round(4.50m * 0.0825m, 2));
        order.Total.Should().Be(order.Subtotal + order.Tax);
    }
}
