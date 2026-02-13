using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class MenuItemEndpoints
{
    public static void MapMenuItemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/menu-items").WithTags("Menu Items");

        group.MapGet("/", async (MannaDbContext db) =>
            await db.MenuItems
                .OrderBy(m => m.SortOrder)
                .Include(m => m.Variants.OrderBy(v => v.Sortorder))
                .Include(m => m.AvailableIngredients.OrderBy(a => a.SortOrder))
                    .ThenInclude(a => a.Ingredient)
                .Select(m => new MenuItemDto(
                    m.Id, m.Categoryid, m.Name, m.Description,
                    m.IsCustomizable, m.Active, m.SortOrder,
                    m.Variants.OrderBy(v => v.Sortorder).Select(v =>
                        new MenuItemVariantDto(v.Id, v.Name, v.Price, v.Sortorder, v.Active)).ToList(),
                    m.IsCustomizable
                        ? m.AvailableIngredients.OrderBy(a => a.SortOrder).Select(a =>
                            new AvailableIngredientDto(a.Id, a.IngredientId, a.Ingredient!.Name!,
                                a.CustomerPrice, a.QuantityUsed, a.IsDefault, a.GroupName!,
                                a.SortOrder, a.Active)).ToList()
                        : null))
                .ToListAsync());

        group.MapGet("/{id:guid}", async (Guid id, MannaDbContext db) =>
        {
            var m = await db.MenuItems
                .Include(m => m.Variants.OrderBy(v => v.Sortorder))
                .Include(m => m.AvailableIngredients.OrderBy(a => a.SortOrder))
                    .ThenInclude(a => a.Ingredient)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (m is null) return Results.NotFound();

            return Results.Ok(new MenuItemDto(
                m.Id, m.Categoryid, m.Name, m.Description,
                m.IsCustomizable, m.Active, m.SortOrder,
                m.Variants.OrderBy(v => v.Sortorder).Select(v =>
                    new MenuItemVariantDto(v.Id, v.Name, v.Price, v.Sortorder, v.Active)).ToList(),
                m.IsCustomizable
                    ? m.AvailableIngredients.OrderBy(a => a.SortOrder).Select(a =>
                        new AvailableIngredientDto(a.Id, a.IngredientId, a.Ingredient!.Name!,
                            a.CustomerPrice, a.QuantityUsed, a.IsDefault, a.GroupName!,
                            a.SortOrder, a.Active)).ToList()
                    : null));
        });

        group.MapPost("/", async (CreateMenuItemRequest req, MannaDbContext db) =>
        {
            var category = await db.Categories.FindAsync(req.CategoryId);
            if (category is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["CategoryId"] = ["Category not found"]
                });

            var menuItem = new MenuItem
            {
                Categoryid = req.CategoryId,
                Name = req.Name,
                Description = req.Description,
                IsCustomizable = req.IsCustomizable,
                SortOrder = req.SortOrder,
                Active = true
            };
            db.MenuItems.Add(menuItem);
            await db.SaveChangesAsync();
            return Results.Created($"/api/menu-items/{menuItem.Id}",
                new MenuItemDto(menuItem.Id, menuItem.Categoryid, menuItem.Name,
                    menuItem.Description, menuItem.IsCustomizable, menuItem.Active,
                    menuItem.SortOrder, [], menuItem.IsCustomizable ? [] : null));
        }).AddEndpointFilter<ValidationFilter<CreateMenuItemRequest>>();

        group.MapPut("/{id:guid}", async (Guid id, UpdateMenuItemRequest req, MannaDbContext db) =>
        {
            var menuItem = await db.MenuItems.FindAsync(id);
            if (menuItem is null) return Results.NotFound();

            var category = await db.Categories.FindAsync(req.CategoryId);
            if (category is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["CategoryId"] = ["Category not found"]
                });

            menuItem.Name = req.Name;
            menuItem.Description = req.Description;
            menuItem.IsCustomizable = req.IsCustomizable;
            menuItem.Categoryid = req.CategoryId;
            menuItem.SortOrder = req.SortOrder;
            menuItem.Active = req.Active;
            await db.SaveChangesAsync();
            return Results.Ok(new MenuItemDto(menuItem.Id, menuItem.Categoryid, menuItem.Name,
                menuItem.Description, menuItem.IsCustomizable, menuItem.Active,
                menuItem.SortOrder, [], menuItem.IsCustomizable ? [] : null));
        }).AddEndpointFilter<ValidationFilter<UpdateMenuItemRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, MannaDbContext db) =>
        {
            var menuItem = await db.MenuItems.FindAsync(id);
            if (menuItem is null) return Results.NotFound();

            menuItem.Active = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
