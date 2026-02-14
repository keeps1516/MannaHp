import { UnitOfMeasure } from "@/types/api";

const unitLabels: Record<UnitOfMeasure, string> = {
  [UnitOfMeasure.Oz]: "oz",
  [UnitOfMeasure.Lb]: "lb",
  [UnitOfMeasure.Cups]: "cup",
  [UnitOfMeasure.FlOz]: "fl oz",
  [UnitOfMeasure.Tsp]: "tsp",
  [UnitOfMeasure.Tbsp]: "tbsp",
  [UnitOfMeasure.Each]: "",
  [UnitOfMeasure.Shot]: "shot",
};

/**
 * Formats a measurement for display, e.g. "8oz", "2 shots", "1 cup".
 * Returns empty string for "Each" unit (e.g. a whole avocado — just show the name).
 */
export function formatMeasurement(
  quantity: number,
  unit: UnitOfMeasure
): string {
  if (unit === UnitOfMeasure.Each) return "";

  const label = unitLabels[unit] ?? "";
  if (!label) return "";

  // Pluralize for count-style units (shot, cup, tbsp, tsp)
  const needsPlural =
    quantity !== 1 &&
    (unit === UnitOfMeasure.Shot ||
      unit === UnitOfMeasure.Cups ||
      unit === UnitOfMeasure.Tsp ||
      unit === UnitOfMeasure.Tbsp);

  // Format: strip trailing zeros for clean display (8.0 → "8", 0.5 → "0.5")
  const qtyStr = Number.isInteger(quantity)
    ? quantity.toString()
    : quantity.toFixed(1).replace(/\.0$/, "");

  return `${qtyStr}${needsPlural ? ` ${label}s` : label}`;
}
