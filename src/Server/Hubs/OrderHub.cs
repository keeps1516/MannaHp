using Microsoft.AspNetCore.SignalR;

namespace MannaHp.Server.Hubs;

public class OrderHub : Hub
{
    /// <summary>
    /// Staff/admin clients join the kitchen group to receive all order updates.
    /// </summary>
    public async Task JoinKitchen()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
    }

    public async Task LeaveKitchen()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "kitchen");
    }

    /// <summary>
    /// Individual customers can track their specific order.
    /// </summary>
    public async Task JoinOrder(string orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }

    public async Task LeaveOrder(string orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
