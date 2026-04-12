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
                    m.Id, m.Categoryid, m.Name, m.Description, m.ImageUrl,
                    m.ImageApproximate, m.IsCustomizable, m.Active, m.SortOrder,
                    m.RestockPolicy,
                    m.Variants.OrderBy(v => v.Sortorder).Select(v =>
                        new MenuItemVariantDto(v.Id, v.Name, v.Price, v.Sortorder, v.Active)).ToList(),
                    m.AvailableIngredients.Any()
                        ? m.AvailableIngredients.OrderBy(a => a.SortOrder).Select(a =>
                            new AvailableIngredientDto(a.Id, a.IngredientId, a.Ingredient!.Name!,
                                a.CustomerPrice, a.QuantityUsed, a.IsDefault, a.GroupName!,
                                a.SortOrder, a.Active, (int)a.Ingredient!.Unit)).ToList()
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
                m.Id, m.Categoryid, m.Name, m.Description, m.ImageUrl,
                m.ImageApproximate, m.IsCustomizable, m.Active, m.SortOrder,
                m.RestockPolicy,
                m.Variants.OrderBy(v => v.Sortorder).Select(v =>
                    new MenuItemVariantDto(v.Id, v.Name, v.Price, v.Sortorder, v.Active)).ToList(),
                m.AvailableIngredients.Any()
                    ? m.AvailableIngredients.OrderBy(a => a.SortOrder).Select(a =>
                        new AvailableIngredientDto(a.Id, a.IngredientId, a.Ingredient!.Name!,
                            a.CustomerPrice, a.QuantityUsed, a.IsDefault, a.GroupName!,
                            a.SortOrder, a.Active, (int)a.Ingredient!.Unit)).ToList()
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
                ImageUrl = req.ImageUrl,
                ImageApproximate = req.ImageApproximate,
                IsCustomizable = req.IsCustomizable,
                SortOrder = req.SortOrder,
                RestockPolicy = req.RestockPolicy,
                Active = true
            };
            db.MenuItems.Add(menuItem);
            await db.SaveChangesAsync();
            return Results.Created($"/api/menu-items/{menuItem.Id}",
                new MenuItemDto(menuItem.Id, menuItem.Categoryid, menuItem.Name,
                    menuItem.Description, menuItem.ImageUrl, menuItem.ImageApproximate,
                    menuItem.IsCustomizable, menuItem.Active, menuItem.SortOrder,
                    menuItem.RestockPolicy, [],
                    menuItem.IsCustomizable ? [] : null));
        }).AddEndpointFilter<ValidationFilter<CreateMenuItemRequest>>()
          .RequireAuthorization("Owner");

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
            menuItem.ImageUrl = req.ImageUrl;
            menuItem.ImageApproximate = req.ImageApproximate;
            menuItem.IsCustomizable = req.IsCustomizable;
            menuItem.Categoryid = req.CategoryId;
            menuItem.SortOrder = req.SortOrder;
            menuItem.Active = req.Active;
            menuItem.RestockPolicy = req.RestockPolicy;
            await db.SaveChangesAsync();
            return Results.Ok(new MenuItemDto(menuItem.Id, menuItem.Categoryid, menuItem.Name,
                menuItem.Description, menuItem.ImageUrl, menuItem.ImageApproximate,
                menuItem.IsCustomizable, menuItem.Active, menuItem.SortOrder,
                menuItem.RestockPolicy, [],
                menuItem.IsCustomizable ? [] : null));
        }).AddEndpointFilter<ValidationFilter<UpdateMenuItemRequest>>()
          .RequireAuthorization("Owner");

        group.MapDelete("/{id:guid}", async (Guid id, MannaDbContext db) =>
        {
            var menuItem = await db.MenuItems.FindAsync(id);
            if (menuItem is null) return Results.NotFound();

            menuItem.Active = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Owner");

        // ── Image Upload ──

        group.MapPost("/{id:guid}/image", async (Guid id, HttpRequest request, MannaDbContext db, IWebHostEnvironment env) =>
        {
            var menuItem = await db.MenuItems.FindAsync(id);
            if (menuItem is null) return Results.NotFound();

            if (!request.HasFormContentType)
                return Results.BadRequest("Expected multipart/form-data");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file provided");

            // Validate file size (max 5 MB)
            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest("File size exceeds 5 MB limit");

            // Validate content type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                return Results.BadRequest("Only JPEG, PNG, and WebP images are allowed");

            // Create uploads directory
            var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "menu");
            Directory.CreateDirectory(uploadsDir);

            // Delete previous image if exists
            if (!string.IsNullOrEmpty(menuItem.ImageUrl))
            {
                var oldPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
                    menuItem.ImageUrl.TrimStart('/'));
                if (File.Exists(oldPath))
                    File.Delete(oldPath);
            }

            // Generate unique filename
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = file.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
            var fileName = $"{id.ToString("N")[..8]}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            menuItem.ImageUrl = $"/uploads/menu/{fileName}";
            await db.SaveChangesAsync();

            return Results.Ok(new MenuItemDto(menuItem.Id, menuItem.Categoryid, menuItem.Name,
                menuItem.Description, menuItem.ImageUrl, menuItem.ImageApproximate,
                menuItem.IsCustomizable, menuItem.Active, menuItem.SortOrder,
                menuItem.RestockPolicy, [],
                menuItem.IsCustomizable ? [] : null));
        }).RequireAuthorization("Owner")
          .DisableAntiforgery();

        group.MapDelete("/{id:guid}/image", async (Guid id, MannaDbContext db, IWebHostEnvironment env) =>
        {
            var menuItem = await db.MenuItems.FindAsync(id);
            if (menuItem is null) return Results.NotFound();

            // Delete the file from disk
            if (!string.IsNullOrEmpty(menuItem.ImageUrl))
            {
                var filePath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
                    menuItem.ImageUrl.TrimStart('/'));
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            menuItem.ImageUrl = null;
            menuItem.ImageApproximate = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Owner");
    }
}
