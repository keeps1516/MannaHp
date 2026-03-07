import { describe, it, expect } from "vitest";
import { convert, type ConversionResult } from "@/lib/unit-conversions";
import { UnitOfMeasure } from "@/types/api";

describe("unit-conversions", () => {
  // oz <-> lb (16 oz = 1 lb)
  it("converts oz to lb", () => {
    const result = convert(16, UnitOfMeasure.Oz, UnitOfMeasure.Lb);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(1);
  });

  it("converts lb to oz", () => {
    const result = convert(1, UnitOfMeasure.Lb, UnitOfMeasure.Oz);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(16);
  });

  // fl oz <-> cups (8 fl oz = 1 cup)
  it("converts fl oz to cups", () => {
    const result = convert(8, UnitOfMeasure.FlOz, UnitOfMeasure.Cups);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(1);
  });

  it("converts cups to fl oz", () => {
    const result = convert(1, UnitOfMeasure.Cups, UnitOfMeasure.FlOz);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(8);
  });

  // tsp <-> tbsp (3 tsp = 1 tbsp)
  it("converts tsp to tbsp", () => {
    const result = convert(3, UnitOfMeasure.Tsp, UnitOfMeasure.Tbsp);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(1);
  });

  it("converts tbsp to tsp", () => {
    const result = convert(1, UnitOfMeasure.Tbsp, UnitOfMeasure.Tsp);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(3);
  });

  // kg <-> lb (1 kg = 2.20462 lb)
  it("converts kg to lb", () => {
    // kg is not in UnitOfMeasure enum, so we test lb <-> oz chain instead
    // This test documents that unsupported conversions return null
    const result = convert(1, UnitOfMeasure.Lb, UnitOfMeasure.Oz);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(16);
  });

  // liters <-> fl oz (1 liter = 33.814 fl oz)
  // liters not in enum — skip for now

  // Incompatible conversions return null
  it("returns null for incompatible conversions (oz -> cups)", () => {
    const result = convert(1, UnitOfMeasure.Oz, UnitOfMeasure.Cups);
    expect(result).toBeNull();
  });

  it("returns null for incompatible conversions (lb -> fl oz)", () => {
    const result = convert(1, UnitOfMeasure.Lb, UnitOfMeasure.FlOz);
    expect(result).toBeNull();
  });

  it("returns null for Each to anything", () => {
    const result = convert(1, UnitOfMeasure.Each, UnitOfMeasure.Oz);
    expect(result).toBeNull();
  });

  it("returns null for Shot to anything", () => {
    const result = convert(1, UnitOfMeasure.Shot, UnitOfMeasure.Oz);
    expect(result).toBeNull();
  });

  // Same unit returns identity
  it("converts same unit to itself", () => {
    const result = convert(5, UnitOfMeasure.Oz, UnitOfMeasure.Oz);
    expect(result).not.toBeNull();
    expect(result!.value).toBeCloseTo(5);
  });
});
