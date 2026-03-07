using System.Security.Claims;
using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using MannaHp.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class IngredientEndpoints
{
    public static void MapIngredientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ingredients").WithTags("Ingredients");

        group.MapGet("/", async (MannaDbContext db) =>
            await db.Ingredients
                .OrderBy(i => i.Name)
                .Select(i => new IngredientDto(i.Id, i.Name!, i.Unit, i.CostPerUnit,
                    i.StockQuantity, i.LowStockThreshold, i.Active))
                .ToListAsync());

        group.MapGet("/{id:guid}", async (Guid id, MannaDbContext db) =>
            await db.Ingredients.FindAsync(id) is Ingredient i
                ? Results.Ok(new IngredientDto(i.Id, i.Name!, i.Unit, i.CostPerUnit,
                    i.StockQuantity, i.LowStockThreshold, i.Active))
                : Results.NotFound());

        group.MapPost("/", async (CreateIngredientRequest req, MannaDbContext db) =>
        {
            var ingredient = new Ingredient
            {
                Name = req.Name,
                Unit = req.Unit,
                CostPerUnit = req.CostPerUnit,
                StockQuantity = req.StockQuantity,
                LowStockThreshold = req.LowStockThreshold,
                Active = true
            };
            db.Ingredients.Add(ingredient);
            await db.SaveChangesAsync();
            return Results.Created($"/api/ingredients/{ingredient.Id}",
                new IngredientDto(ingredient.Id, ingredient.Name!, ingredient.Unit,
                    ingredient.CostPerUnit, ingredient.StockQuantity,
                    ingredient.LowStockThreshold, ingredient.Active));
        }).AddEndpointFilter<ValidationFilter<CreateIngredientRequest>>()
          .RequireAuthorization("Owner");

        group.MapPut("/{id:guid}", async (Guid id, UpdateIngredientRequest req, MannaDbContext db) =>
        {
            var ingredient = await db.Ingredients.FindAsync(id);
            if (ingredient is null) return Results.NotFound();

            ingredient.Name = req.Name;
            ingredient.Unit = req.Unit;
            ingredient.CostPerUnit = req.CostPerUnit;
            ingredient.StockQuantity = req.StockQuantity;
            ingredient.LowStockThreshold = req.LowStockThreshold;
            ingredient.Active = req.Active;
            await db.SaveChangesAsync();
            return Results.Ok(new IngredientDto(ingredient.Id, ingredient.Name!, ingredient.Unit,
                ingredient.CostPerUnit, ingredient.StockQuantity,
                ingredient.LowStockThreshold, ingredient.Active));
        }).AddEndpointFilter<ValidationFilter<UpdateIngredientRequest>>()
          .RequireAuthorization("Owner");

        group.MapDelete("/{id:guid}", async (Guid id, MannaDbContext db) =>
        {
            var ingredient = await db.Ingredients.FindAsync(id);
            if (ingredient is null) return Results.NotFound();

            ingredient.Active = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Owner");

        // ── Restock ──
        group.MapPost("/{id:guid}/restock", async (Guid id, RestockRequest req, MannaDbContext db, ClaimsPrincipal user) =>
        {
            if (req.Quantity <= 0)
                return Results.BadRequest("Quantity must be positive");

            var ingredient = await db.Ingredients.FindAsync(id);
            if (ingredient is null) return Results.NotFound();

            ingredient.StockQuantity += req.Quantity;

            var log = new InventoryLog
            {
                IngredientId = id,
                ChangeType = InventoryChangeType.Received,
                QuantityChange = req.Quantity,
                NewStockQuantity = ingredient.StockQuantity,
                Notes = req.Notes,
                CreatedBy = user.FindFirstValue(ClaimTypes.NameIdentifier),
            };
            db.InventoryLogs.Add(log);

            await db.SaveChangesAsync();
            return Results.Ok(new IngredientDto(ingredient.Id, ingredient.Name!, ingredient.Unit,
                ingredient.CostPerUnit, ingredient.StockQuantity,
                ingredient.LowStockThreshold, ingredient.Active));
        }).RequireAuthorization("Owner");

        // ── Bulk Restock ──
        group.MapPost("/bulk-restock", async (BulkRestockRequest req, MannaDbContext db, ClaimsPrincipal user) =>
        {
            if (req.Items.Count == 0)
                return Results.BadRequest("No items to restock");

            var ids = req.Items.Select(i => i.IngredientId).ToList();
            var ingredients = await db.Ingredients
                .Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var updated = new List<IngredientDto>();

            foreach (var item in req.Items)
            {
                if (item.Quantity <= 0) continue;
                if (!ingredients.TryGetValue(item.IngredientId, out var ingredient)) continue;

                // Update cost per unit via weighted average
                if (item.CostPaid > 0)
                {
                    var newCostPerUnit = item.CostPaid / item.Quantity;
                    if (ingredient.StockQuantity > 0)
                    {
                        ingredient.CostPerUnit =
                            ((ingredient.StockQuantity * ingredient.CostPerUnit) + (item.Quantity * newCostPerUnit))
                            / (ingredient.StockQuantity + item.Quantity);
                    }
                    else
                    {
                        ingredient.CostPerUnit = newCostPerUnit;
                    }
                }

                ingredient.StockQuantity += item.Quantity;

                db.InventoryLogs.Add(new InventoryLog
                {
                    IngredientId = item.IngredientId,
                    ChangeType = InventoryChangeType.Received,
                    QuantityChange = item.Quantity,
                    NewStockQuantity = ingredient.StockQuantity,
                    Notes = $"Delivery: {item.Quantity} received" + (item.CostPaid > 0 ? $" (${item.CostPaid:F2})" : ""),
                    CreatedBy = userId,
                });

                updated.Add(new IngredientDto(ingredient.Id, ingredient.Name!, ingredient.Unit,
                    ingredient.CostPerUnit, ingredient.StockQuantity,
                    ingredient.LowStockThreshold, ingredient.Active));
            }

            await db.SaveChangesAsync();
            return Results.Ok(updated);
        }).RequireAuthorization("Owner");

        // ── Inventory History ──
        group.MapGet("/{id:guid}/history", async (Guid id, MannaDbContext db) =>
        {
            var ingredient = await db.Ingredients.FindAsync(id);
            if (ingredient is null) return Results.NotFound();

            var logs = await db.InventoryLogs
                .Where(l => l.IngredientId == id)
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .Select(l => new InventoryLogDto(
                    l.Id, l.IngredientId, ingredient.Name!,
                    l.ChangeType, l.QuantityChange, l.NewStockQuantity,
                    l.Notes, l.CreatedBy, l.CreatedAt))
                .ToListAsync();

            return Results.Ok(logs);
        }).RequireAuthorization("Owner");
    }
}
