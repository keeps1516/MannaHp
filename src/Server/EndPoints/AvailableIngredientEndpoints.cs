using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class AvailableIngredientEndpoints
{
    public static void MapAvailableIngredientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/menu-items/{menuItemId:guid}/available-ingredients")
            .WithTags("Available Ingredients");

        group.MapGet("/", async (Guid menuItemId, MannaDbContext db) =>
        {
            var menuItem = await db.MenuItems.FindAsync(menuItemId);
            if (menuItem is null) return Results.NotFound();

            var ingredients = await db.MenuItemAvailableIngredients
                .Where(a => a.MenuItemId == menuItemId)
                .Include(a => a.Ingredient)
                .OrderBy(a => a.GroupName).ThenBy(a => a.SortOrder)
                .Select(a => new AvailableIngredientDto(a.Id, a.IngredientId, a.Ingredient!.Name!,
                    a.CustomerPrice, a.QuantityUsed, a.IsDefault, a.GroupName!,
                    a.SortOrder, a.Active))
                .ToListAsync();

            return Results.Ok(ingredients);
        });

        group.MapPost("/", async (Guid menuItemId, CreateAvailableIngredientRequest req, MannaDbContext db) =>
        {
            var menuItem = await db.MenuItems.FindAsync(menuItemId);
            if (menuItem is null) return Results.NotFound();

            var ingredient = await db.Ingredients.FindAsync(req.IngredientId);
            if (ingredient is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["IngredientId"] = ["Ingredient not found"]
                });

            var available = new MenuItemAvailableIngredient
            {
                MenuItemId = menuItemId,
                IngredientId = req.IngredientId,
                CustomerPrice = req.CustomerPrice,
                QuantityUsed = req.QuantityUsed,
                IsDefault = req.IsDefault,
                GroupName = req.GroupName,
                SortOrder = req.SortOrder,
                Active = true
            };
            db.MenuItemAvailableIngredients.Add(available);
            await db.SaveChangesAsync();
            return Results.Created(
                $"/api/menu-items/{menuItemId}/available-ingredients/{available.Id}",
                new AvailableIngredientDto(available.Id, available.IngredientId, ingredient.Name!,
                    available.CustomerPrice, available.QuantityUsed, available.IsDefault,
                    available.GroupName!, available.SortOrder, available.Active));
        }).AddEndpointFilter<ValidationFilter<CreateAvailableIngredientRequest>>();

        group.MapPut("/{availableId:guid}", async (Guid menuItemId, Guid availableId,
            UpdateAvailableIngredientRequest req, MannaDbContext db) =>
        {
            var available = await db.MenuItemAvailableIngredients
                .Include(a => a.Ingredient)
                .FirstOrDefaultAsync(a => a.Id == availableId && a.MenuItemId == menuItemId);
            if (available is null) return Results.NotFound();

            available.CustomerPrice = req.CustomerPrice;
            available.QuantityUsed = req.QuantityUsed;
            available.IsDefault = req.IsDefault;
            available.GroupName = req.GroupName;
            available.SortOrder = req.SortOrder;
            available.Active = req.Active;
            await db.SaveChangesAsync();
            return Results.Ok(new AvailableIngredientDto(available.Id, available.IngredientId,
                available.Ingredient!.Name!, available.CustomerPrice, available.QuantityUsed,
                available.IsDefault, available.GroupName!, available.SortOrder, available.Active));
        }).AddEndpointFilter<ValidationFilter<UpdateAvailableIngredientRequest>>();

        group.MapDelete("/{availableId:guid}", async (Guid menuItemId, Guid availableId, MannaDbContext db) =>
        {
            var available = await db.MenuItemAvailableIngredients
                .FirstOrDefaultAsync(a => a.Id == availableId && a.MenuItemId == menuItemId);
            if (available is null) return Results.NotFound();

            available.Active = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
