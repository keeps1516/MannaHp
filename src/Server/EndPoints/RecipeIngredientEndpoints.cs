using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class RecipeIngredientEndpoints
{
    public static void MapRecipeIngredientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/menu-items/{menuItemId:guid}/variants/{variantId:guid}/recipe-ingredients")
            .WithTags("Recipe Ingredients");

        group.MapGet("/", async (Guid menuItemId, Guid variantId, MannaDbContext db) =>
        {
            var variant = await db.MenuItemVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
            if (variant is null) return Results.NotFound();

            var recipes = await db.RecipeIngredients
                .Where(r => r.VariantId == variantId)
                .Include(r => r.Ingredient)
                .OrderBy(r => r.Ingredient!.Name)
                .Select(r => new RecipeIngredientDto(r.Id, r.IngredientId,
                    r.Ingredient!.Name!, r.Quantity))
                .ToListAsync();

            return Results.Ok(recipes);
        });

        group.MapPost("/", async (Guid menuItemId, Guid variantId,
            CreateRecipeIngredientRequest req, MannaDbContext db) =>
        {
            var variant = await db.MenuItemVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
            if (variant is null) return Results.NotFound();

            var ingredient = await db.Ingredients.FindAsync(req.IngredientId);
            if (ingredient is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["IngredientId"] = ["Ingredient not found"]
                });

            var recipe = new RecipeIngredient
            {
                VariantId = variantId,
                IngredientId = req.IngredientId,
                Quantity = req.Quantity
            };
            db.RecipeIngredients.Add(recipe);
            await db.SaveChangesAsync();
            return Results.Created(
                $"/api/menu-items/{menuItemId}/variants/{variantId}/recipe-ingredients/{recipe.Id}",
                new RecipeIngredientDto(recipe.Id, recipe.IngredientId, ingredient.Name!, recipe.Quantity));
        }).AddEndpointFilter<ValidationFilter<CreateRecipeIngredientRequest>>()
          .RequireAuthorization("Owner");

        group.MapPut("/{recipeId:guid}", async (Guid menuItemId, Guid variantId, Guid recipeId,
            UpdateRecipeIngredientRequest req, MannaDbContext db) =>
        {
            var variant = await db.MenuItemVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
            if (variant is null) return Results.NotFound();

            var recipe = await db.RecipeIngredients
                .Include(r => r.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == recipeId && r.VariantId == variantId);
            if (recipe is null) return Results.NotFound();

            recipe.Quantity = req.Quantity;
            await db.SaveChangesAsync();
            return Results.Ok(new RecipeIngredientDto(recipe.Id, recipe.IngredientId,
                recipe.Ingredient!.Name!, recipe.Quantity));
        }).AddEndpointFilter<ValidationFilter<UpdateRecipeIngredientRequest>>()
          .RequireAuthorization("Owner");

        group.MapDelete("/{recipeId:guid}", async (Guid menuItemId, Guid variantId, Guid recipeId,
            MannaDbContext db) =>
        {
            var variant = await db.MenuItemVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
            if (variant is null) return Results.NotFound();

            var recipe = await db.RecipeIngredients
                .FirstOrDefaultAsync(r => r.Id == recipeId && r.VariantId == variantId);
            if (recipe is null) return Results.NotFound();

            db.RecipeIngredients.Remove(recipe);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Owner");
    }
}
