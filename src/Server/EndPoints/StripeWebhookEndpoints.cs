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

    private static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Status,
        order.PaymentMethod,
        order.PaymentStatus,
        order.Subtotal,
        order.TaxRate,
        order.Tax,
        order.Total,
        order.Notes,
        order.CreatedAt,
        order.Items.Select(oi => new OrderItemDto(
            oi.Id,
            oi.MenuItem?.Name ?? "",
            oi.Variant?.Name,
            oi.Quantity,
            oi.UnitPrice,
            oi.TotalPrice,
            oi.Notes,
            oi.Ingredients.Count > 0
                ? oi.Ingredients.Select(oii => new OrderItemIngredientDto(
                    oii.IngredientId,
                    oii.Ingredient?.Name ?? "",
                    oii.QuantityUsed,
                    oii.Ingredient?.Unit ?? UnitOfMeasure.Each,
                    oii.PriceCharged)).ToList()
                : null
        )).ToList()
    );
}
