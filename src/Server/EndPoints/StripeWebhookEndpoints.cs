using MannaHp.Server.Data;
using MannaHp.Server.Hubs;
using MannaHp.Server.Services;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace MannaHp.Server.Endpoints;

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/stripe/webhook", async (HttpRequest request, MannaDbContext db,
            IConfiguration config, IHubContext<OrderHub> hub, StripeService stripe) =>
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync();
            var webhookSecret = config["Stripe:WebhookSecret"];

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json,
                    request.Headers["Stripe-Signature"], webhookSecret);
            }
            catch (StripeException)
            {
                return Results.BadRequest(new { error = "Invalid webhook signature" });
            }

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                {
                    await HandlePaymentSucceeded(paymentIntent, db, stripe, hub);
                }
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                {
                    await HandlePaymentFailed(paymentIntent, db);
                }
            }
            else if (stripeEvent.Type == EventTypes.ChargeRefunded)
            {
                if (stripeEvent.Data.Object is Charge charge)
                {
                    await HandleChargeRefunded(charge, db, hub);
                }
            }

            return Results.Ok();
        }).DisableAntiforgery();
    }

    private static async Task HandlePaymentSucceeded(PaymentIntent paymentIntent,
        MannaDbContext db, StripeService stripe, IHubContext<OrderHub> hub)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
            .Include(o => o.Items).ThenInclude(oi => oi.Variant)
            .Include(o => o.Items).ThenInclude(oi => oi.Ingredients).ThenInclude(oii => oii.Ingredient)
            .Include(o => o.Refunds)
            .FirstOrDefaultAsync(o => o.StripePaymentId == paymentIntent.Id);

        if (order is null || order.PaymentStatus == PaymentStatus.Paid) return;

        order.PaymentStatus = PaymentStatus.Paid;

        // Extract card details from the charge
        if (!string.IsNullOrEmpty(paymentIntent.LatestChargeId))
        {
            var charge = await stripe.GetChargeAsync(paymentIntent.LatestChargeId);
            order.CardBrand = charge.PaymentMethodDetails?.Card?.Brand;
            order.CardLast4 = charge.PaymentMethodDetails?.Card?.Last4;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Broadcast to kitchen
        var dto = MapToDto(order);
        await hub.Clients.Group("kitchen").SendAsync("OrderCreated", dto);
    }

    private static async Task HandlePaymentFailed(PaymentIntent paymentIntent, MannaDbContext db)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.StripePaymentId == paymentIntent.Id);

        if (order is null || order.PaymentStatus == PaymentStatus.Paid) return;

        order.PaymentStatus = PaymentStatus.Failed;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task HandleChargeRefunded(Charge charge, MannaDbContext db, IHubContext<OrderHub> hub)
    {
        // Find the order by the PaymentIntent ID on the charge
        var paymentIntentId = charge.PaymentIntentId;
        if (string.IsNullOrEmpty(paymentIntentId)) return;

        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
            .Include(o => o.Refunds)
            .FirstOrDefaultAsync(o => o.StripePaymentId == paymentIntentId);
        if (order is null) return;

        // Get the latest refund from Stripe charge
        var stripeRefund = charge.Refunds?.Data?.FirstOrDefault();
        if (stripeRefund is null) return;

        // Idempotency: skip if we already have a refund with this Stripe refund ID
        if (order.Refunds.Any(r => r.StripeRefundId == stripeRefund.Id)) return;

        var refundAmount = stripeRefund.Amount / 100m; // Stripe uses cents
        var existingRefundTotal = order.Refunds.Sum(r => r.Amount + r.TaxAmount);
        var refundSubtotal = order.Subtotal > 0
            ? Math.Round(refundAmount * order.Subtotal / order.Total, 2)
            : refundAmount;
        var refundTax = refundAmount - refundSubtotal;

        var refund = new Refund
        {
            OrderId = order.Id,
            Amount = refundSubtotal,
            TaxAmount = refundTax,
            Reason = "Refunded via Stripe dashboard",
            StripeRefundId = stripeRefund.Id,
            CreatedBy = "stripe-webhook",
        };

        // Create refund items proportionally across all non-refunded order items
        var alreadyRefundedItemIds = order.Refunds
            .SelectMany(r => r.Items)
            .Select(ri => ri.OrderItemId)
            .ToHashSet();

        var eligibleItems = order.Items
            .Where(oi => !alreadyRefundedItemIds.Contains(oi.Id))
            .ToList();

        if (eligibleItems.Count > 0)
        {
            var eligibleTotal = eligibleItems.Sum(oi => oi.TotalPrice);
            foreach (var item in eligibleItems)
            {
                var itemShare = eligibleTotal > 0
                    ? Math.Round(item.TotalPrice / eligibleTotal * refundSubtotal, 2)
                    : 0;
                refund.Items.Add(new RefundItem
                {
                    OrderItemId = item.Id,
                    Amount = itemShare,
                    Restocked = false,
                });
            }
        }

        db.Refunds.Add(refund);

        // Update payment status
        var totalRefunded = existingRefundTotal + refundAmount;
        if (totalRefunded >= order.Total)
            order.PaymentStatus = PaymentStatus.Refunded;

        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var broadcast = new { order.Id, RefundAmount = refundAmount, order.PaymentStatus };
        await hub.Clients.Group("kitchen").SendAsync("OrderRefunded", broadcast);
        await hub.Clients.Group($"order-{order.Id}").SendAsync("OrderRefunded", broadcast);
    }

    // Reuse the shared MapToDto from OrderEndpoints
    private static OrderDto MapToDto(Order order) => OrderEndpoints.MapToDto(order);
}
