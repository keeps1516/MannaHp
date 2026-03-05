using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;

namespace MannaHp.Server.Tests.Endpoints;

[Collection("Api")]
public class OrderWorkflowTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    // Known seed GUIDs
    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");
    private static readonly Guid MiEspShot = Guid.Parse("c0000000-0022-0000-0000-000000000034");
    private static readonly Guid VEspShot = Guid.Parse("d0000000-0041-0000-0000-000000000065");

    public OrderWorkflowTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static CreateOrderRequest QuickOrder() =>
        new(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(MiChips, VChips, 1, null, null)]);

    private async Task<OrderDto> CreateOrderAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", QuickOrder());
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        return result!.Order;
    }

    // ── Full lifecycle: Received → Preparing → Ready → Completed ──────

    [Fact]
    public async Task FullLifecycle_ReceivedThroughCompleted()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        order.Status.Should().Be(OrderStatus.Received);

        // → Preparing
        var r1 = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));
        r1.StatusCode.Should().Be(HttpStatusCode.OK);

        // → Ready
        var r2 = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Ready));
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        // → Completed
        var r3 = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));
        r3.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify final state
        var final = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        final!.Status.Should().Be(OrderStatus.Completed);
    }

    // ── Cancel from Received ──────────────────────────────────────────

    [Fact]
    public async Task Cancel_FromReceived_Succeeds()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        var response = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Cancelled));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelled = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        cancelled!.Status.Should().Be(OrderStatus.Cancelled);
    }

    // ── Cancel from Preparing ─────────────────────────────────────────

    [Fact]
    public async Task Cancel_FromPreparing_Succeeds()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        var response = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Cancelled));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelled = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}");
        cancelled!.Status.Should().Be(OrderStatus.Cancelled);
    }

    // ── Skip status: Received → Ready ─────────────────────────────────

    [Fact]
    public async Task SkipStatus_ReceivedToReady_Succeeds()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        var response = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Ready));

        // The API allows skipping statuses (no sequential enforcement)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Status update on Completed order ─────────────────────────────
    // Note: The API currently allows status updates on any order — no
    // guard against re-opening Completed/Cancelled orders. These tests
    // document actual behavior.

    [Fact]
    public async Task PatchStatus_CompletedOrder_CurrentlyAllowed()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));

        // API does not block this — documenting current behavior
        var response = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Status update on Cancelled order ──────────────────────────────

    [Fact]
    public async Task PatchStatus_CancelledOrder_CurrentlyAllowed()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Cancelled));

        // API does not block this — documenting current behavior
        var response = await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/orders/active — filtering ────────────────────────────

    [Fact]
    public async Task GetActive_IncludesReceivedPreparingReady()
    {
        var staffClient = await _factory.CreateStaffClientAsync();

        // Create 3 orders in different active statuses
        var o1 = await CreateOrderAsync(); // Received
        var o2 = await CreateOrderAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{o2.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));
        var o3 = await CreateOrderAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{o3.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Ready));

        var active = await staffClient.GetFromJsonAsync<List<OrderDto>>("/api/orders/active");

        active.Should().NotBeNull();
        active!.Should().Contain(o => o.Id == o1.Id);
        active.Should().Contain(o => o.Id == o2.Id);
        active.Should().Contain(o => o.Id == o3.Id);
    }

    [Fact]
    public async Task GetActive_ExcludesCancelledOrders()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Cancelled));

        var active = await staffClient.GetFromJsonAsync<List<OrderDto>>("/api/orders/active");

        active!.Should().NotContain(o => o.Id == order.Id);
    }

    [Fact]
    public async Task GetActive_ExcludesCompletedOrders()
    {
        var staffClient = await _factory.CreateStaffClientAsync();
        var order = await CreateOrderAsync();

        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Completed));

        var active = await staffClient.GetFromJsonAsync<List<OrderDto>>("/api/orders/active");

        active!.Should().NotContain(o => o.Id == order.Id);
    }
}
