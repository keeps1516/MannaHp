import { UnitOfMeasure } from "@/types/api";

export interface ConversionResult {
  value: number;
  fromUnit: UnitOfMeasure;
  toUnit: UnitOfMeasure;
}

// Conversion groups — units within the same group can convert to each other
// Each entry maps [from, to] -> multiplier (from * multiplier = to)
const conversions: [UnitOfMeasure, UnitOfMeasure, number][] = [
  // Weight: oz <-> lb
  [UnitOfMeasure.Oz, UnitOfMeasure.Lb, 1 / 16],
  [UnitOfMeasure.Lb, UnitOfMeasure.Oz, 16],

  // Volume: fl oz <-> cups
  [UnitOfMeasure.FlOz, UnitOfMeasure.Cups, 1 / 8],
  [UnitOfMeasure.Cups, UnitOfMeasure.FlOz, 8],

  // Volume: tsp <-> tbsp
  [UnitOfMeasure.Tsp, UnitOfMeasure.Tbsp, 1 / 3],
  [UnitOfMeasure.Tbsp, UnitOfMeasure.Tsp, 3],
];

export function convert(
  value: number,
  from: UnitOfMeasure,
  to: UnitOfMeasure
): ConversionResult | null {
  if (from === to) {
    return { value, fromUnit: from, toUnit: to };
  }

  const entry = conversions.find(([f, t]) => f === from && t === to);
  if (!entry) return null;

  return { value: value * entry[2], fromUnit: from, toUnit: to };
}

// Get units that a given unit can convert to
export function getConvertibleUnits(unit: UnitOfMeasure): UnitOfMeasure[] {
  return conversions.filter(([f]) => f === unit).map(([, t]) => t);
}
