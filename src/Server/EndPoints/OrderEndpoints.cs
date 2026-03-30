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
        group.MapPost("/", async (CreateOrderRequest req, HttpContext httpContext, MannaDbContext db,
            IHubContext<OrderHub> hub, StripeService stripe) =>
        {
            // In-store orders require a valid store token
            if (req.PaymentMethod == PaymentMethod.InStore)
            {
                var tokenHeader = httpContext.Request.Headers["X-Store-Token"].FirstOrDefault();
                if (string.IsNullOrEmpty(tokenHeader))
                    return Results.Json(new { error = "A valid store token is required for in-store orders." }, statusCode: 401);

                var validToken = await db.StoreTokens.FirstOrDefaultAsync(t =>
                    t.Token == tokenHeader && !t.Revoked && t.ExpiresAt > DateTime.UtcNow);
                if (validToken is null)
                    return Results.Json(new { error = "Store token is invalid or expired." }, statusCode: 401);
            }
            var taxRateSetting = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "DefaultTaxRate");
            if (taxRateSetting is null)
                throw new InvalidOperationException("Missing required app setting: DefaultTaxRate");

            if (!decimal.TryParse(taxRateSetting.Value, out var taxRate))
                throw new InvalidOperationException($"Invalid DefaultTaxRate value: '{taxRateSetting.Value}'");

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
                .Include(o => o.Refunds)
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
                .Include(o => o.Refunds)
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
                .Include(o => o.Refunds)
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
                .Include(o => o.Refunds)
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

            var refunded = await db.Refunds
                .Where(r => r.CreatedAt >= todayUtc)
                .SumAsync(r => r.Amount + r.TaxAmount);

            return Results.Ok(new { total = total - refunded });
        }).RequireAuthorization("Staff");

        // PATCH mark in-store order as paid — Staff only
        group.MapPatch("/{id:guid}/mark-paid", async (Guid id, MannaDbContext db,
            IHubContext<OrderHub> hub) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();

            if (order.PaymentMethod != PaymentMethod.InStore)
                return Results.BadRequest(new { error = "Only in-store orders can be marked as paid" });
            if (order.PaymentStatus != PaymentStatus.Pending)
                return Results.BadRequest(new { error = "Order payment is not pending" });

            order.PaymentStatus = PaymentStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var update = new { order.Id, order.PaymentStatus };
            await hub.Clients.Group("kitchen").SendAsync("OrderPaymentUpdated", update);
            await hub.Clients.Group($"order-{id}").SendAsync("OrderPaymentUpdated", update);

            return Results.Ok(update);
        }).RequireAuthorization("Staff");

        // POST cancel order — Staff only
        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelOrderRequest req,
            MannaDbContext db, IHubContext<OrderHub> hub) =>
        {
            var order = await db.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Items).ThenInclude(oi => oi.Ingredients)
                .Include(o => o.Items).ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v!.RecipeIngredients)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();

            if (order.Status == OrderStatus.Cancelled)
                return Results.BadRequest(new { error = "Order is already cancelled" });

            var wasCompleted = order.Status == OrderStatus.Completed;
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            // Restock returnable items if order was completed (inventory was decremented)
            if (wasCompleted && req.RestockItems?.Count > 0)
            {
                await RestockItemsAsync(db, order, req.RestockItems
                    .Where(ri => ri.Restock)
                    .Select(ri => ri.OrderItemId)
                    .ToHashSet());
            }

            await db.SaveChangesAsync();

            var update = new { order.Id, Status = order.Status };
            await hub.Clients.Group("kitchen").SendAsync("OrderCancelled", update);
            await hub.Clients.Group($"order-{id}").SendAsync("OrderCancelled", update);

            return Results.Ok(update);
        }).AddEndpointFilter<ValidationFilter<CancelOrderRequest>>()
          .RequireAuthorization("Staff");

        // POST refund order items — Staff only
        group.MapPost("/{id:guid}/refund", async (Guid id, CreateRefundRequest req,
            MannaDbContext db, IHubContext<OrderHub> hub, HttpContext httpContext) =>
        {
            var order = await db.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Items).ThenInclude(oi => oi.Ingredients)
                .Include(o => o.Items).ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v!.RecipeIngredients)
                .Include(o => o.Refunds).ThenInclude(r => r.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();

            // Build set of already-refunded order item IDs
            var alreadyRefundedItemIds = order.Refunds
                .SelectMany(r => r.Items)
                .Select(ri => ri.OrderItemId)
                .ToHashSet();

            // Validate requested items exist on the order and aren't already refunded
            var requestedItemIds = req.Items.Select(i => i.OrderItemId).ToHashSet();
            var orderItemMap = order.Items.ToDictionary(oi => oi.Id);
            foreach (var itemReq in req.Items)
            {
                if (!orderItemMap.ContainsKey(itemReq.OrderItemId))
                    return Results.BadRequest(new { error = $"Order item {itemReq.OrderItemId} not found on this order" });
                if (alreadyRefundedItemIds.Contains(itemReq.OrderItemId))
                    return Results.BadRequest(new { error = $"Order item {itemReq.OrderItemId} has already been refunded" });
            }

            // Calculate refund amount from selected items
            decimal refundSubtotal = req.Items
                .Sum(i => orderItemMap[i.OrderItemId].TotalPrice);
            decimal refundTax = order.Subtotal > 0
                ? Math.Round(refundSubtotal / order.Subtotal * order.Tax, 2)
                : 0;

            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

            var refund = new Refund
            {
                OrderId = order.Id,
                Amount = refundSubtotal,
                TaxAmount = refundTax,
                Reason = req.Reason,
                CreatedBy = userId,
            };

            var restockItemIds = req.Items
                .Where(i => i.Restock)
                .Select(i => i.OrderItemId)
                .ToHashSet();

            foreach (var itemReq in req.Items)
            {
                var orderItem = orderItemMap[itemReq.OrderItemId];
                var isReturnable = orderItem.MenuItem?.RestockPolicy == RestockPolicy.Returnable;
                var shouldRestock = isReturnable && itemReq.Restock && order.Status == OrderStatus.Completed;

                refund.Items.Add(new RefundItem
                {
                    OrderItemId = itemReq.OrderItemId,
                    Amount = orderItem.TotalPrice,
                    Restocked = shouldRestock,
                });
            }

            db.Refunds.Add(refund);

            // Restock returnable items if order was completed
            if (order.Status == OrderStatus.Completed && restockItemIds.Count > 0)
            {
                await RestockItemsAsync(db, order, restockItemIds);
            }

            // Update payment status: Refunded if full order is now refunded
            var totalRefundedSoFar = order.Refunds.Sum(r => r.Amount + r.TaxAmount) + refundSubtotal + refundTax;
            if (totalRefundedSoFar >= order.Total)
                order.PaymentStatus = PaymentStatus.Refunded;

            order.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Re-fetch refund with navigation for DTO
            var savedRefund = await db.Refunds
                .Include(r => r.Items).ThenInclude(ri => ri.OrderItem).ThenInclude(oi => oi!.MenuItem)
                .FirstAsync(r => r.Id == refund.Id);

            var refundDto = MapRefundToDto(savedRefund);

            var broadcast = new { order.Id, RefundAmount = refundSubtotal + refundTax, order.PaymentStatus };
            await hub.Clients.Group("kitchen").SendAsync("OrderRefunded", broadcast);
            await hub.Clients.Group($"order-{id}").SendAsync("OrderRefunded", broadcast);

            return Results.Created($"/api/orders/{id}/refunds", refundDto);
        }).AddEndpointFilter<ValidationFilter<CreateRefundRequest>>()
          .RequireAuthorization("Staff");

        // GET refund history for an order — Staff only
        group.MapGet("/{id:guid}/refunds", async (Guid id, MannaDbContext db) =>
        {
            var orderExists = await db.Orders.AnyAsync(o => o.Id == id);
            if (!orderExists) return Results.NotFound();

            var refunds = await db.Refunds
                .Where(r => r.OrderId == id)
                .Include(r => r.Items).ThenInclude(ri => ri.OrderItem).ThenInclude(oi => oi!.MenuItem)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Results.Ok(refunds.Select(MapRefundToDto).ToList());
        }).RequireAuthorization("Staff");

        // PATCH status (kitchen staff) — Staff only
        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest req,
            MannaDbContext db, IHubContext<OrderHub> hub) =>
        {
            var order = await db.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Ingredients)
                .Include(o => o.Items).ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v!.RecipeIngredients)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return Results.NotFound();

            var previousStatus = order.Status;
            order.UpdatedAt = DateTime.UtcNow;

            // Decrement inventory only on transition to Completed (guard against double-decrement)
            bool hasLowStock = false;
            int lowStockCount = 0;
            if (req.Status == OrderStatus.Completed && previousStatus != OrderStatus.Completed)
            {
                await DecrementInventoryAsync(db, order);

                // Check if any active ingredients are now below their low stock threshold
                lowStockCount = await db.Ingredients
                    .CountAsync(i => i.Active && i.StockQuantity < i.LowStockThreshold);
                hasLowStock = lowStockCount > 0;
            }

            order.Status = req.Status;

            await db.SaveChangesAsync();

            var update = new { order.Id, order.Status };

            // Broadcast status change to kitchen + individual order watchers
            await hub.Clients.Group("kitchen").SendAsync("OrderStatusChanged", update);
            await hub.Clients.Group($"order-{id}").SendAsync("OrderStatusChanged", update);

            // Broadcast low stock alert if any ingredients dropped below threshold
            if (hasLowStock)
            {
                await hub.Clients.Group("kitchen").SendAsync("LowStockAlert",
                    new { lowStockCount });
            }

            return Results.Ok(update);
        }).AddEndpointFilter<ValidationFilter<UpdateOrderStatusRequest>>()
          .RequireAuthorization("Staff");
    }

    private static async Task DecrementInventoryAsync(MannaDbContext db, Order order)
    {
        // Aggregate total usage per ingredient across all order items
        var decrements = new Dictionary<Guid, decimal>();

        foreach (var item in order.Items)
        {
            // Customizable items (bowls): decrement from order_item_ingredients
            if (item.Ingredients.Count > 0)
            {
                foreach (var oii in item.Ingredients)
                {
                    var total = oii.QuantityUsed * item.Quantity;
                    if (decrements.ContainsKey(oii.IngredientId))
                        decrements[oii.IngredientId] += total;
                    else
                        decrements[oii.IngredientId] = total;
                }
            }
            // Fixed items (drinks/sides): decrement from recipe_ingredients via variant
            else if (item.Variant?.RecipeIngredients is { Count: > 0 } recipe)
            {
                foreach (var ri in recipe)
                {
                    var total = ri.Quantity * item.Quantity;
                    if (decrements.ContainsKey(ri.IngredientId))
                        decrements[ri.IngredientId] += total;
                    else
                        decrements[ri.IngredientId] = total;
                }
            }
        }

        if (decrements.Count == 0) return;

        var ingredientIds = decrements.Keys.ToList();
        var ingredients = await db.Ingredients
            .Where(i => ingredientIds.Contains(i.Id))
            .ToListAsync();

        foreach (var ingredient in ingredients)
        {
            if (decrements.TryGetValue(ingredient.Id, out var amount))
            {
                ingredient.StockQuantity -= amount;

                db.InventoryLogs.Add(new InventoryLog
                {
                    IngredientId = ingredient.Id,
                    ChangeType = InventoryChangeType.OrderDecrement,
                    QuantityChange = -amount,
                    NewStockQuantity = ingredient.StockQuantity,
                    Notes = $"Order #{order.OrderNumber}",
                });
            }
        }
    }

    private static async Task RestockItemsAsync(MannaDbContext db, Order order, HashSet<Guid> restockOrderItemIds)
    {
        var increments = new Dictionary<Guid, decimal>();

        foreach (var item in order.Items)
        {
            if (!restockOrderItemIds.Contains(item.Id)) continue;
            if (item.MenuItem?.RestockPolicy != RestockPolicy.Returnable) continue;

            // Customizable items: restock from order_item_ingredients
            if (item.Ingredients.Count > 0)
            {
                foreach (var oii in item.Ingredients)
                {
                    var total = oii.QuantityUsed * item.Quantity;
                    if (increments.ContainsKey(oii.IngredientId))
                        increments[oii.IngredientId] += total;
                    else
                        increments[oii.IngredientId] = total;
                }
            }
            // Fixed items: restock from recipe_ingredients via variant
            else if (item.Variant?.RecipeIngredients is { Count: > 0 } recipe)
            {
                foreach (var ri in recipe)
                {
                    var total = ri.Quantity * item.Quantity;
                    if (increments.ContainsKey(ri.IngredientId))
                        increments[ri.IngredientId] += total;
                    else
                        increments[ri.IngredientId] = total;
                }
            }
        }

        if (increments.Count == 0) return;

        var ingredientIds = increments.Keys.ToList();
        var ingredients = await db.Ingredients
            .Where(i => ingredientIds.Contains(i.Id))
            .ToListAsync();

        foreach (var ingredient in ingredients)
        {
            if (increments.TryGetValue(ingredient.Id, out var amount))
            {
                ingredient.StockQuantity += amount;

                db.InventoryLogs.Add(new InventoryLog
                {
                    IngredientId = ingredient.Id,
                    ChangeType = InventoryChangeType.OrderRestock,
                    QuantityChange = amount,
                    NewStockQuantity = ingredient.StockQuantity,
                    Notes = $"Restock from order #{order.OrderNumber}",
                });
            }
        }
    }

    internal static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Status,
        order.PaymentMethod,
        order.PaymentStatus,
        order.Subtotal,
        order.TaxRate,
        order.Tax,
        order.Total,
        order.Refunds.Sum(r => r.Amount + r.TaxAmount),
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
            oi.MenuItem?.RestockPolicy ?? RestockPolicy.NonReturnable,
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

    internal static RefundDto MapRefundToDto(Refund refund) => new(
        refund.Id,
        refund.OrderId,
        refund.Amount,
        refund.TaxAmount,
        refund.Reason,
        refund.StripeRefundId,
        refund.CreatedBy,
        refund.CreatedAt,
        refund.Items.Select(ri => new RefundItemDto(
            ri.Id,
            ri.OrderItemId,
            ri.OrderItem?.MenuItem?.Name ?? "",
            ri.Amount,
            ri.Restocked
        )).ToList()
    );
}
