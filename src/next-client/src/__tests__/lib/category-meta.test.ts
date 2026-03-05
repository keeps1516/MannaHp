import { describe, it, expect } from "vitest";
import { getCategoryMeta } from "@/lib/category-meta";

describe("getCategoryMeta", () => {
  it("returns correct emoji for 'burrito bowls'", () => {
    const meta = getCategoryMeta("Burrito Bowls");
    expect(meta.emoji).toBe("🌯");
  });

  it("returns correct emoji for 'traditional drinks'", () => {
    const meta = getCategoryMeta("Traditional Drinks");
    expect(meta.emoji).toBe("☕");
  });

  it("returns correct emoji for 'seasonal specials'", () => {
    const meta = getCategoryMeta("Seasonal Specials");
    expect(meta.emoji).toBe("✨");
  });

  it("returns correct emoji for 'sides & drinks'", () => {
    const meta = getCategoryMeta("Sides & Drinks");
    expect(meta.emoji).toBe("🥤");
  });

  it("returns description for known categories", () => {
    const meta = getCategoryMeta("coffee");
    expect(meta.description).toBe("Freshly brewed coffee");
  });

  it("is case-insensitive", () => {
    const meta = getCategoryMeta("BURRITO BOWLS");
    expect(meta.emoji).toBe("🌯");
  });

  it("trims whitespace", () => {
    const meta = getCategoryMeta("  coffee  ");
    expect(meta.emoji).toBe("☕");
  });

  it("uses partial match for unrecognized variants", () => {
    const meta = getCategoryMeta("iced coffee");
    expect(meta.emoji).toBe("☕");
  });

  it("returns fallback for unknown category", () => {
    const meta = getCategoryMeta("totally unknown category xyz");
    expect(meta.emoji).toBe("🍽️");
    expect(meta.description).toBe("Explore our menu");
  });
});
