import { UnitOfMeasure } from "@/types/api";

export const unitOptions = [
  { value: UnitOfMeasure.Oz, label: "Ounces (oz)" },
  { value: UnitOfMeasure.Lb, label: "Pounds (lb)" },
  { value: UnitOfMeasure.Cups, label: "Cups" },
  { value: UnitOfMeasure.FlOz, label: "Fluid Ounces (fl oz)" },
  { value: UnitOfMeasure.Tsp, label: "Teaspoons (tsp)" },
  { value: UnitOfMeasure.Tbsp, label: "Tablespoons (tbsp)" },
  { value: UnitOfMeasure.Each, label: "Each" },
  { value: UnitOfMeasure.Shot, label: "Shots" },
];

/** Get the display label for a UnitOfMeasure enum value */
export function unitLabel(unit: UnitOfMeasure): string {
  return unitOptions.find((o) => o.value === unit)?.label ?? "Unknown";
}
