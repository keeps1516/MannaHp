using System.Text.Json;
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
            var keys = new[] { "DefaultTaxRate", "StoreTokenRequiredMessage" };
            var settings = await db.AppSettings
                .Where(s => keys.Contains(s.Key))
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var taxRate = settings.TryGetValue("DefaultTaxRate", out var taxStr)
                && decimal.TryParse(taxStr, out var parsed)
                ? parsed : 0.0825m;

            var storeTokenRequiredMessage = settings.GetValueOrDefault(
                "StoreTokenRequiredMessage",
                "Please scan the QR code at our counter to place an in-store order.");

            return Results.Ok(new { taxRate, storeTokenRequiredMessage });
        });

        group.MapGet("/tv-menu-config", async (MannaDbContext db) =>
        {
            var setting = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "TvMenuConfig");

            TvMenuConfig config;
            if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
            {
                config = new TvMenuConfig();
            }
            else
            {
                config = JsonSerializer.Deserialize<TvMenuConfig>(setting.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new TvMenuConfig();
            }

            // Resolve sample bowl prices from current ingredient data
            var resolvedBowls = new Dictionary<string, ResolvedSampleBowl>();
            if (config.SampleBowls is { Count: > 0 })
            {
                var allIngredientIdStrings = config.SampleBowls.Values
                    .SelectMany(b => b.IngredientIds)
                    .Distinct()
                    .ToList();

                var allIngredientGuids = allIngredientIdStrings
                    .Where(s => Guid.TryParse(s, out _))
                    .Select(Guid.Parse)
                    .ToList();

                // Fetch available ingredients with their prices and names
                var availableIngredients = await db.MenuItemAvailableIngredients
                    .Where(ai => allIngredientGuids.Contains(ai.Id) && ai.Active)
                    .Select(ai => new { Id = ai.Id.ToString(), ai.Ingredient.Name, ai.CustomerPrice })
                    .ToListAsync();

                var ingredientLookup = availableIngredients
                    .ToDictionary(ai => ai.Id, ai => ai);

                foreach (var (menuItemId, bowl) in config.SampleBowls)
                {
                    var ingredients = bowl.IngredientIds
                        .Where(id => ingredientLookup.ContainsKey(id))
                        .Select(id => ingredientLookup[id])
                        .ToList();

                    resolvedBowls[menuItemId] = new ResolvedSampleBowl
                    {
                        Label = bowl.Label,
                        IngredientIds = bowl.IngredientIds,
                        IngredientNames = ingredients.Select(i => i.Name ?? "").ToList(),
                        CalculatedPrice = ingredients.Sum(i => i.CustomerPrice)
                    };
                }
            }

            return Results.Ok(new
            {
                config.VisibleCategoryIds,
                config.HiddenItemIds,
                config.ShowAllIngredients,
                config.OrderOnlineUrl,
                SampleBowls = resolvedBowls
            });
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
                else
                {
                    db.AppSettings.Add(new AppSettings
                    {
                        Id = Guid.NewGuid(),
                        Key = update.Key,
                        Value = update.Value
                    });
                }
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("Owner");
    }

    public record SettingUpdate(string Key, string Value);

    // TV Menu Config types
    public class TvMenuConfig
    {
        public List<string> VisibleCategoryIds { get; set; } = new();
        public List<string> HiddenItemIds { get; set; } = new();
        public bool ShowAllIngredients { get; set; }
        public string OrderOnlineUrl { get; set; } = "";
        public Dictionary<string, SampleBowlConfig> SampleBowls { get; set; } = new();
    }

    public class SampleBowlConfig
    {
        public string Label { get; set; } = "";
        public List<string> IngredientIds { get; set; } = new();
    }

    public class ResolvedSampleBowl
    {
        public string Label { get; set; } = "";
        public List<string> IngredientIds { get; set; } = new();
        public List<string> IngredientNames { get; set; } = new();
        public decimal CalculatedPrice { get; set; }
    }
}
