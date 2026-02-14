using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
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
    }
}
