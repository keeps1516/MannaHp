using MannahHp.Shared.Enums;

namespace MannahHp.Shared.Helpers;

public static class UnitConversion
{
    private static readonly Dictionary<UnitOfMeasure, MeasurementType> UnitTypes = new()
    {
        [UnitOfMeasure.Oz]   = MeasurementType.Weight,
        [UnitOfMeasure.Lb]   = MeasurementType.Weight,
        [UnitOfMeasure.Cups] = MeasurementType.Volume,
        [UnitOfMeasure.FlOz] = MeasurementType.Volume,
        [UnitOfMeasure.Tsp]  = MeasurementType.Volume,
        [UnitOfMeasure.Tbsp] = MeasurementType.Volume,
        [UnitOfMeasure.Each] = MeasurementType.Count,
        [UnitOfMeasure.Shot] = MeasurementType.Count,
    };

    // Conversion factors to base unit within each type.
    // Weight base: Oz
    // Volume base: FlOz
    // Count base: Each
    private static readonly Dictionary<UnitOfMeasure, decimal> ToBaseFactor = new()
    {
        [UnitOfMeasure.Oz]   = 1m,
        [UnitOfMeasure.Lb]   = 16m,        // 1 lb = 16 oz
        [UnitOfMeasure.FlOz] = 1m,
        [UnitOfMeasure.Cups] = 8m,         // 1 cup = 8 fl oz
        [UnitOfMeasure.Tsp]  = 1m / 6m,    // 1 tsp = ~0.1667 fl oz
        [UnitOfMeasure.Tbsp] = 0.5m,       // 1 tbsp = 0.5 fl oz
        [UnitOfMeasure.Each] = 1m,
        [UnitOfMeasure.Shot] = 1m,          // 1 shot = 1 each (discrete)
    };

    private static readonly Dictionary<UnitOfMeasure, string> Abbreviations = new()
    {
        [UnitOfMeasure.Oz]   = "oz",
        [UnitOfMeasure.Lb]   = "lb",
        [UnitOfMeasure.Cups] = "cups",
        [UnitOfMeasure.FlOz] = "fl oz",
        [UnitOfMeasure.Tsp]  = "tsp",
        [UnitOfMeasure.Tbsp] = "tbsp",
        [UnitOfMeasure.Each] = "each",
        [UnitOfMeasure.Shot] = "shot",
    };

    public static MeasurementType GetMeasurementType(UnitOfMeasure unit)
        => UnitTypes[unit];

    public static string GetAbbreviation(UnitOfMeasure unit)
        => Abbreviations[unit];

    public static bool CanConvert(UnitOfMeasure from, UnitOfMeasure to)
        => UnitTypes[from] == UnitTypes[to];

    public static decimal Convert(decimal value, UnitOfMeasure from, UnitOfMeasure to)
    {
        if (from == to)
            return value;

        if (!CanConvert(from, to))
            throw new InvalidOperationException(
                $"Cannot convert between {from} ({UnitTypes[from]}) and {to} ({UnitTypes[to]}). " +
                $"Units must be in the same measurement type.");

        var baseValue = value * ToBaseFactor[from];
        return baseValue / ToBaseFactor[to];
    }
}
