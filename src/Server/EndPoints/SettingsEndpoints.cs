using MannaHp.Server.Data;
using MannaHp.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MannaHp.Server.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/", async (MannaDbContext db) =>
        {
            var settings = await db.AppSettings.ToListAsync();
            return Results.Ok(settings.Select(s => new { s.Key, s.Value }));
        }).RequireAuthorization("Owner");

        group.MapGet("/public", async (MannaDbContext db) =>
        {
            var taxRateSetting = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "DefaultTaxRate");
            var taxRate = taxRateSetting is not null
                && decimal.TryParse(taxRateSetting.Value, out var parsed)
                ? parsed : 0.0825m;
            return Results.Ok(new { taxRate });
        });

        group.MapPut("/", async (List<SettingUpdate> updates, MannaDbContext db) =>
        {
            var existing = await db.AppSettings.ToListAsync();
            var lookup = existing.ToDictionary(s => s.Key);

            foreach (var update in updates)
            {
                if (lookup.TryGetValue(update.Key, out var setting))
                {
                    setting.Value = update.Value;
                }
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Owner");
    }

    public record SettingUpdate(string Key, string Value);
}
