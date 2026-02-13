using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // POST — place an order
        group.MapPost("/", async (CreateOrderRequest req, MannaDbContext db) =>
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

                if (menuItem.IsCustomizable && itemReq.SelectedIngredientIds?.Count > 0)
                {
                    // Look up each selected ingredient from available ingredients
                    var availableIngredients = await db.MenuItemAvailableIngredients
                        .Include(a => a.Ingredient)
                        .Where(a => a.MenuItemId == itemReq.MenuItemId
                            && itemReq.SelectedIngredientIds.Contains(a.Id)
                            && a.Active)
                        .ToListAsync();

                    if (availableIngredients.Count != itemReq.SelectedIngredientIds.Count)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["SelectedIngredientIds"] = ["One or more selected ingredients are invalid"]
                        });

                    foreach (var avail in availableIngredients)
                    {
                        orderItem.Ingredients.Add(new OrderItemIngredient
                        {
                            IngredientId = avail.IngredientId,
                            QuantityUsed = avail.QuantityUsed,
                            PriceCharged = avail.CustomerPrice
                        });
                    }

                    orderItem.UnitPrice = availableIngredients.Sum(a => a.CustomerPrice);
                }
                else if (!menuItem.IsCustomizable)
                {
                    // Fixed item — price comes from the variant
                    if (itemReq.VariantId is null)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["VariantId"] = ["Variant is required for fixed menu items"]
                        });

                    var variant = await db.MenuItemVariants
                        .FirstOrDefaultAsync(v => v.Id == itemReq.VariantId && v.MenuItemId == itemReq.MenuItemId);
                    if (variant is null)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["VariantId"] = [$"Variant {itemReq.VariantId} not found for this menu item"]
                        });

                    orderItem.UnitPrice = variant.Price;
                }

                orderItem.TotalPrice = orderItem.UnitPrice * orderItem.Quantity;
                order.Items.Add(orderItem);
            }

            order.Subtotal = order.Items.Sum(i => i.TotalPrice);
            order.Tax = Math.Round(order.Subtotal * taxRate, 2);
            order.TaxRate = taxRate;
            order.Total = order.Subtotal + order.Tax;

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return Results.Created($"/api/orders/{order.Id}", MapToDto(order));
        }).AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();

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

        // GET active orders (kitchen display)
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
        });

        // PATCH status (kitchen staff)
        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest req, MannaDbContext db) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();

            order.Status = req.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { order.Id, order.Status });
        }).AddEndpointFilter<ValidationFilter<UpdateOrderStatusRequest>>();
    }

    private static OrderDto MapToDto(Order order) => new(
        order.Id,
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
                    oii.PriceCharged)).ToList()
                : null
        )).ToList()
    );
}
