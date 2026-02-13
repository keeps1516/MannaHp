using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class VariantEndpoints
{
    public static void MapVariantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/menu-items/{menuItemId:guid}/variants").WithTags("Variants");

        group.MapGet("/", async (Guid menuItemId, MannaDbContext db) =>
        {
            var menuItem = await db.MenuItems.FindAsync(menuItemId);
            if (menuItem is null) return Results.NotFound();

            var variants = await db.MenuItemVariants
                .Where(v => v.MenuItemId == menuItemId)
                .OrderBy(v => v.Sortorder)
                .Select(v => new MenuItemVariantDto(v.Id, v.Name, v.Price, v.Sortorder, v.Active))
                .ToListAsync();

            return Results.Ok(variants);
        });

        group.MapPost("/", async (Guid menuItemId, CreateVariantRequest req, MannaDbContext db) =>
        {
            var menuItem = await db.MenuItems.FindAsync(menuItemId);
            if (menuItem is null) return Results.NotFound();

            var variant = new MenuItemVariant
            {
                MenuItemId = menuItemId,
                Name = req.Name,
                Price = req.Price,
                Sortorder = req.SortOrder,
                Active = true
            };
            db.MenuItemVariants.Add(variant);
            await db.SaveChangesAsync();
            return Results.Created($"/api/menu-items/{menuItemId}/variants/{variant.Id}",
                new MenuItemVariantDto(variant.Id, variant.Name, variant.Price, variant.Sortorder, variant.Active));
        }).AddEndpointFilter<ValidationFilter<CreateVariantRequest>>();

        group.MapPut("/{variantId:guid}", async (Guid menuItemId, Guid variantId,
            UpdateVariantRequest req, MannaDbContext db) =>
        {
            var variant = await db.MenuItemVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
            if (variant is null) return Results.NotFound();

            variant.Name = req.Name;
            variant.Price = req.Price;
            variant.Sortorder = req.SortOrder;
            variant.Active = req.Active;
            await db.SaveChangesAsync();
            return Results.Ok(new MenuItemVariantDto(variant.Id, variant.Name, variant.Price,
                variant.Sortorder, variant.Active));
        }).AddEndpointFilter<ValidationFilter<UpdateVariantRequest>>();

        group.MapDelete("/{variantId:guid}", async (Guid menuItemId, Guid variantId, MannaDbContext db) =>
        {
            var variant = await db.MenuItemVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.MenuItemId == menuItemId);
            if (variant is null) return Results.NotFound();

            variant.Active = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
