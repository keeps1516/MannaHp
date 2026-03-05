using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MannaHp.Server.Tests.Fixtures;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using Microsoft.AspNetCore.SignalR.Client;

namespace MannaHp.Server.Tests.SignalR;

[Collection("Api")]
public class SignalRTests
{
    private readonly HttpClient _client;
    private readonly MannaApiFactory _factory;

    private static readonly Guid MiChips = Guid.Parse("c0000000-001f-0000-0000-000000000031");
    private static readonly Guid VChips = Guid.Parse("d0000000-003e-0000-0000-000000000062");

    public SignalRTests(MannaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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

    // ── Kitchen group receives OrderCreated ───────────────────────────

    [Fact]
    public async Task KitchenClient_ReceivesOrderCreated_WhenInStoreOrderPlaced()
    {
        await using var hub = CreateHubConnection();
        var received = new TaskCompletionSource<OrderDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<OrderDto>("OrderCreated", order => received.TrySetResult(order));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinKitchen");

        // Place an in-store order
        var order = await CreateInStoreOrderAsync();

        var notified = await Task.WhenAny(received.Task, Task.Delay(5000));
        notified.Should().Be(received.Task, "kitchen should receive OrderCreated within 5s");

        var dto = await received.Task;
        dto.Id.Should().Be(order.Id);
    }

    // ── Kitchen group receives OrderStatusChanged ─────────────────────

    [Fact]
    public async Task KitchenClient_ReceivesOrderStatusChanged_OnStatusUpdate()
    {
        await using var hub = CreateHubConnection();
        var received = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<object>("OrderStatusChanged", update => received.TrySetResult(update));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinKitchen");

        // Create and then update order status
        var order = await CreateInStoreOrderAsync();

        var staffClient = await _factory.CreateStaffClientAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        var notified = await Task.WhenAny(received.Task, Task.Delay(5000));
        notified.Should().Be(received.Task, "kitchen should receive OrderStatusChanged within 5s");
    }

    // ── Individual order group receives status update ──────────────────

    [Fact]
    public async Task CustomerInOrderGroup_ReceivesStatusUpdate()
    {
        var order = await CreateInStoreOrderAsync();

        await using var hub = CreateHubConnection();
        var received = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<object>("OrderStatusChanged", update => received.TrySetResult(update));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinOrder", order.Id.ToString());

        var staffClient = await _factory.CreateStaffClientAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        var notified = await Task.WhenAny(received.Task, Task.Delay(5000));
        notified.Should().Be(received.Task, "customer should receive OrderStatusChanged within 5s");
    }

    // ── Customer does NOT receive updates for other orders ─────────────

    [Fact]
    public async Task CustomerInOrderGroup_DoesNotReceiveOtherOrderUpdates()
    {
        var myOrder = await CreateInStoreOrderAsync();
        var otherOrder = await CreateInStoreOrderAsync();

        await using var hub = CreateHubConnection();
        var received = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.On<object>("OrderStatusChanged", update => received.TrySetResult(update));

        await hub.StartAsync();
        await hub.InvokeAsync("JoinOrder", myOrder.Id.ToString());

        // Update the OTHER order
        var staffClient = await _factory.CreateStaffClientAsync();
        await staffClient.PatchAsJsonAsync($"/api/orders/{otherOrder.Id}/status",
            new UpdateOrderStatusRequest(OrderStatus.Preparing));

        // Should NOT receive anything — wait briefly then verify
        var notified = await Task.WhenAny(received.Task, Task.Delay(1000));
        notified.Should().NotBe(received.Task, "customer should not receive other order updates");
    }

    // ── Client can join and leave kitchen group ───────────────────────

    [Fact]
    public async Task Client_CanJoinAndLeaveKitchenGroup()
    {
        await using var hub = CreateHubConnection();
        await hub.StartAsync();

        // Join and leave should not throw
        await hub.InvokeAsync("JoinKitchen");
        await hub.InvokeAsync("LeaveKitchen");

        hub.State.Should().Be(HubConnectionState.Connected);
    }

    // ── Multiple kitchen clients all receive broadcasts ───────────────

    [Fact]
    public async Task MultipleKitchenClients_AllReceiveBroadcasts()
    {
        await using var hub1 = CreateHubConnection();
        await using var hub2 = CreateHubConnection();

        var received1 = new TaskCompletionSource<OrderDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received2 = new TaskCompletionSource<OrderDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub1.On<OrderDto>("OrderCreated", order => received1.TrySetResult(order));
        hub2.On<OrderDto>("OrderCreated", order => received2.TrySetResult(order));

        await hub1.StartAsync();
        await hub2.StartAsync();
        await hub1.InvokeAsync("JoinKitchen");
        await hub2.InvokeAsync("JoinKitchen");

        await CreateInStoreOrderAsync();

        var r1 = await Task.WhenAny(received1.Task, Task.Delay(5000));
        var r2 = await Task.WhenAny(received2.Task, Task.Delay(5000));

        r1.Should().Be(received1.Task, "hub1 should receive OrderCreated");
        r2.Should().Be(received2.Task, "hub2 should receive OrderCreated");
    }
}
