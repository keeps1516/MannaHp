using System.Security.Claims;
using MannaHp.Server.Data;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class StoreTokenEndpoints
{
    public static void MapStoreTokenEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/store-tokens").WithTags("StoreTokens");

        // POST — generate a new token (auto-revokes existing active token)
        group.MapPost("/", async (GenerateStoreTokenRequest req, MannaDbContext db, ClaimsPrincipal user) =>
        {
            // Determine duration from request or fall back to AppSettings default
            var durationDays = req.DurationDays;
            if (durationDays is null or <= 0)
            {
                var setting = await db.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == "StoreTokenDurationDays");
                durationDays = setting is not null && int.TryParse(setting.Value, out var parsed)
                    ? parsed : 7;
            }

            // Revoke any existing active tokens
            var activeTokens = await db.StoreTokens
                .Where(t => !t.Revoked && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            foreach (var active in activeTokens)
                active.Revoked = true;

            var token = new StoreToken
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddDays(durationDays.Value),
                CreatedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            };

            db.StoreTokens.Add(token);
            await db.SaveChangesAsync();

            return Results.Created($"/api/store-tokens/{token.Id}", new StoreTokenResponse(
                token.Id, token.Token, token.ExpiresAt, token.CreatedAt, token.CreatedByUserId));
        }).RequireAuthorization("Staff");

        // GET — current active token
        group.MapGet("/current", async (MannaDbContext db) =>
        {
            var token = await db.StoreTokens
                .Where(t => !t.Revoked && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (token is null) return Results.NotFound();

            return Results.Ok(new StoreTokenResponse(
                token.Id, token.Token, token.ExpiresAt, token.CreatedAt, token.CreatedByUserId));
        }).RequireAuthorization("Staff");

        // DELETE — revoke a token
        group.MapDelete("/{id:guid}", async (Guid id, MannaDbContext db) =>
        {
            var token = await db.StoreTokens.FindAsync(id);
            if (token is null) return Results.NotFound();

            token.Revoked = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Staff");

        // GET — validate a token (anonymous — used by customer frontend)
        group.MapGet("/{token}/validate", async (string token, MannaDbContext db) =>
        {
            var storeToken = await db.StoreTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (storeToken is null || storeToken.Revoked || storeToken.ExpiresAt <= DateTime.UtcNow)
                return Results.Ok(new StoreTokenValidationResponse(false, null));

            return Results.Ok(new StoreTokenValidationResponse(true, storeToken.ExpiresAt));
        });
    }
}
