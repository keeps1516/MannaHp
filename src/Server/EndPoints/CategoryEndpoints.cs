using MannaHp.Server.Data;
using MannaHp.Server.Filters;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories").WithTags("Categories");

        group.MapGet("/", async (MannaDbContext db) =>
            await db.Categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryDto(c.Id, c.Name, c.SortOrder, c.Active))
                .ToListAsync());

        group.MapGet("/{id:guid}", async (Guid id, MannaDbContext db) =>
            await db.Categories.FindAsync(id) is Category c
                ? Results.Ok(new CategoryDto(c.Id, c.Name, c.SortOrder, c.Active))
                : Results.NotFound());

        group.MapPost("/", async (CreateCategoryRequest req, MannaDbContext db) =>
        {
            var category = new Category
            {
                Name = req.Name,
                SortOrder = req.SortOrder,
                Active = true
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/categories/{category.Id}",
                new CategoryDto(category.Id, category.Name, category.SortOrder, category.Active));
        }).AddEndpointFilter<ValidationFilter<CreateCategoryRequest>>()
          .RequireAuthorization("Owner");

        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest req, MannaDbContext db) =>
        {
            var category = await db.Categories.FindAsync(id);
            if (category is null) return Results.NotFound();

            category.Name = req.Name;
            category.SortOrder = req.SortOrder;
            category.Active = req.Active;
            await db.SaveChangesAsync();
            return Results.Ok(new CategoryDto(category.Id, category.Name, category.SortOrder, category.Active));
        }).AddEndpointFilter<ValidationFilter<UpdateCategoryRequest>>()
          .RequireAuthorization("Owner");

        group.MapDelete("/{id:guid}", async (Guid id, MannaDbContext db) =>
        {
            var category = await db.Categories.FindAsync(id);
            if (category is null) return Results.NotFound();

            category.Active = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Owner");
    }
}
