using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Server.Hubs;
using MannaHp.Server.Services;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // POST — place an order
        group.MapPost("/", async (CreateOrderRequest req, MannaDbContext db,
            IHubContext<OrderHub> hub, StripeService stripe) =>
        {
            var taxRate = 0.0825m; // TODO: pull from app_settings table

            var order = new Order
            {
                PaymentMethod = req.PaymentMethod,
                PaymentStatus = req.PaymentMethod == PaymentMethod.InStore
                    ? PaymentStatus.Pending : PaymentStatus.Pending,
                Status = OrderStatus.Received,
                Notes = req.Notes
            };

            foreach (var itemReq in req.Items)
            {
                var menuItem = await db.MenuItems.FindAsync(itemReq.MenuItemId);
                if (menuItem is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["MenuItemId"] = [$"Menu item {itemReq.MenuItemId} not found"]
                    });

                var orderItem = new OrderItem
                {
                    MenuItemId = itemReq.MenuItemId,
                    VariantId = itemReq.VariantId,
                    Quantity = itemReq.Quantity,
                    Notes = itemReq.Notes
                };

                decimal unitPrice = 0;

                // Step 1: If a variant is provided, look it up and use its price as the base
                if (itemReq.VariantId is not null)
                {
                    var variant = await db.MenuItemVariants
                        .FirstOrDefaultAsync(v => v.Id == itemReq.VariantId && v.MenuItemId == itemReq.MenuItemId);
                    if (variant is null)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["VariantId"] = [$"Variant {itemReq.VariantId} not found for this menu item"]
                        });

                    unitPrice += variant.Price;
                }

                // Step 2: If ingredients are selected, look them up and add their prices
                if (itemReq.SelectedIngredientIds?.Count > 0)
                {
                    var uniqueIds = itemReq.SelectedIngredientIds.Distinct().ToList();
                    var availableIngredients = await db.MenuItemAvailableIngredients
                        .Include(a => a.Ingredient)
                        .Where(a => a.MenuItemId == itemReq.MenuItemId
                            && uniqueIds.Contains(a.Id)
                            && a.Active)
                        .ToListAsync();

                    if (availableIngredients.Count != uniqueIds.Count)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["SelectedIngredientIds"] = ["One or more selected ingredients are invalid"]
                        });

                    var ingredientLookup = availableIngredients.ToDictionary(a => a.Id);

                    foreach (var selectedId in itemReq.SelectedIngredientIds)
                    {
                        if (!ingredientLookup.TryGetValue(selectedId, out var avail)) continue;
                        orderItem.Ingredients.Add(new OrderItemIngredient
                        {
                            IngredientId = avail.IngredientId,
                            QuantityUsed = avail.QuantityUsed,
                            PriceCharged = avail.CustomerPrice
                        });
                    }

                    unitPrice += itemReq.SelectedIngredientIds
                        .Where(id => ingredientLookup.ContainsKey(id))
                        .Sum(id => ingredientLookup[id].CustomerPrice);
                }

                // Validate: must have at least a variant or ingredients
                if (itemReq.VariantId is null && (itemReq.SelectedIngredientIds is null || itemReq.SelectedIngredientIds.Count == 0))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["Item"] = ["Either a variant or selected ingredients must be provided"]
                    });

                orderItem.UnitPrice = unitPrice;
                orderItem.TotalPrice = orderItem.UnitPrice * orderItem.Quantity;
                order.Items.Add(orderItem);
            }

            order.Subtotal = order.Items.Sum(i => i.TotalPrice);
            order.Tax = Math.Round(order.Subtotal * taxRate, 2);
            order.TaxRate = taxRate;
            order.Total = order.Subtotal + order.Tax;

            // For card payments, create a Stripe PaymentIntent
            string? clientSecret = null;
            if (req.PaymentMethod == PaymentMethod.Card)
            {
                if (!stripe.IsConfigured)
                    return Results.UnprocessableEntity(new { error = "Card payments are not yet available. Stripe is not configured." });

                var paymentIntent = await stripe.CreatePaymentIntentAsync(
                    order.Total, "Manna order");
                order.StripePaymentId = paymentIntent.Id;
                clientSecret = paymentIntent.ClientSecret;
            }

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Re-fetch with navigation properties for the DTO
            var saved = await db.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Items).ThenInclude(oi => oi.Variant)
                .Include(o => o.Items).ThenInclude(oi => oi.Ingredients).ThenInclude(oii => oii.Ingredient)
                .FirstAsync(o => o.Id == order.Id);

            var dto = MapToDto(saved);

            // For in-store orders, broadcast to kitchen immediately
            // For card orders, wait until payment is confirmed
            if (req.PaymentMethod == PaymentMethod.InStore)
            {
                await hub.Clients.Group("kitchen").SendAsync("OrderCreated", dto);
            }

            return Results.Created($"/api/orders/{order.Id}",
                new CreateOrderResponse(dto, clientSecret,
                    req.PaymentMethod == PaymentMethod.Card ? stripe.PublishableKey : null));
        }).AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();

        // POST — confirm payment (client calls after Stripe.confirmPayment succeeds)
        group.MapPost("/{id:guid}/confirm-payment", async (Guid id, MannaDbContext db,
            StripeService stripe, IHubContext<OrderHub> hub) =>
        {
            var order = await db.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Items).ThenInclude(oi => oi.Variant)
                .Include(o => o.Items).ThenInclude(oi => oi.Ingredients).ThenInclude(oii => oii.Ingredient)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null) return Results.NotFound();
            if (order.PaymentStatus == PaymentStatus.Paid)
                return Results.Ok(MapToDto(order));
            if (string.IsNullOrEmpty(order.StripePaymentId))
                return Results.BadRequest(new { error = "No payment intent associated with this order" });

            var paymentIntent = await stripe.GetPaymentIntentAsync(order.StripePaymentId);

            if (paymentIntent.Status == "succeeded")
            {
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

                // Now broadcast to kitchen
                var dto = MapToDto(order);
                await hub.Clients.Group("kitchen").SendAsync("OrderCreated", dto);
                return Results.Ok(dto);
            }

            if (paymentIntent.Status is "requires_payment_method" or "canceled")
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return Results.Ok(MapToDto(order));
        });

        // GET by id
        group.MapGet("/{id:guid}", async (Guid id, MannaDbContext db) =>
        {
            var order = await db.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Items).ThenInclude(oi => oi.Variant)
                .Include(o => o.Items).ThenInclude(oi => oi.Ingredients).ThenInclude(oii => oii.Ingredient)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null) return Results.NotFound();
            return Results.Ok(MapToDto(order));
        });

        // GET active orders (kitchen display) — Staff only
        group.MapGet("/active", async (MannaDbContext db) =>
        {
            var orders = await db.Orders
                .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.CreatedAt)
                .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Items).ThenInclude(oi => oi.Variant)
                .Include(o => o.Items).ThenInclude(oi => oi.Ingredients).ThenInclude(oii => oii.Ingredient)
                .ToListAsync();

            return Results.Ok(orders.Select(MapToDto).ToList());
        }).RequireAuthorization("Staff");

        // GET today's revenue — Staff only
        group.MapGet("/today-revenue", async (MannaDbContext db) =>
        {
            var todayUtc = DateTime.UtcNow.Date;
            var total = await db.Orders
                .Where(o => o.CreatedAt >= todayUtc
                    && o.Status == OrderStatus.Completed)
                .SumAsync(o => o.Total);

            return Results.Ok(new { total });
        }).RequireAuthorization("Staff");

        // PATCH status (kitchen staff) — Staff only
        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest req,
            MannaDbContext db, IHubContext<OrderHub> hub) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();

            order.Status = req.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var update = new { order.Id, order.Status };

            // Broadcast status change to kitchen + individual order watchers
            await hub.Clients.Group("kitchen").SendAsync("OrderStatusChanged", update);
            await hub.Clients.Group($"order-{id}").SendAsync("OrderStatusChanged", update);

            return Results.Ok(update);
        }).AddEndpointFilter<ValidationFilter<UpdateOrderStatusRequest>>()
          .RequireAuthorization("Staff");
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
