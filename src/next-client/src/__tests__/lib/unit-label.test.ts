import { describe, it, expect } from "vitest";
import { formatMeasurement } from "@/lib/unit-label";
import { UnitOfMeasure } from "@/types/api";

describe("formatMeasurement", () => {
  it('returns empty string for UnitOfMeasure.Each', () => {
    expect(formatMeasurement(1, UnitOfMeasure.Each)).toBe("");
    expect(formatMeasurement(5, UnitOfMeasure.Each)).toBe("");
  });

  it("formats ounces without pluralization", () => {
    expect(formatMeasurement(8, UnitOfMeasure.Oz)).toBe("8oz");
    expect(formatMeasurement(1, UnitOfMeasure.Oz)).toBe("1oz");
  });

  it("formats pounds without pluralization", () => {
    expect(formatMeasurement(2, UnitOfMeasure.Lb)).toBe("2lb");
    expect(formatMeasurement(0.5, UnitOfMeasure.Lb)).toBe("0.5lb");
  });

  it("formats fluid ounces without pluralization", () => {
    expect(formatMeasurement(12, UnitOfMeasure.FlOz)).toBe("12fl oz");
  });

  it("pluralizes shots when quantity is not 1", () => {
    expect(formatMeasurement(1, UnitOfMeasure.Shot)).toBe("1shot");
    expect(formatMeasurement(2, UnitOfMeasure.Shot)).toBe("2 shots");
  });

  it("pluralizes cups when quantity is not 1", () => {
    expect(formatMeasurement(1, UnitOfMeasure.Cups)).toBe("1cup");
    expect(formatMeasurement(3, UnitOfMeasure.Cups)).toBe("3 cups");
  });

  it("pluralizes tsp when quantity is not 1", () => {
    expect(formatMeasurement(1, UnitOfMeasure.Tsp)).toBe("1tsp");
    expect(formatMeasurement(2, UnitOfMeasure.Tsp)).toBe("2 tsps");
  });

  it("pluralizes tbsp when quantity is not 1", () => {
    expect(formatMeasurement(1, UnitOfMeasure.Tbsp)).toBe("1tbsp");
    expect(formatMeasurement(2, UnitOfMeasure.Tbsp)).toBe("2 tbsps");
  });

  it("strips trailing .0 for whole numbers", () => {
    expect(formatMeasurement(8.0, UnitOfMeasure.Oz)).toBe("8oz");
  });

  it("preserves decimal for non-whole numbers", () => {
    expect(formatMeasurement(0.5, UnitOfMeasure.Oz)).toBe("0.5oz");
  });
});
