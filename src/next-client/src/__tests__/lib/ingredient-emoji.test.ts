import { describe, it, expect } from "vitest";
import { getIngredientEmoji } from "@/lib/ingredient-emoji";

describe("getIngredientEmoji", () => {
  it("returns 🍚 for rice", () => {
    expect(getIngredientEmoji("Jasmine Rice")).toBe("🍚");
  });

  it("returns 🍗 for chicken", () => {
    expect(getIngredientEmoji("Chicken")).toBe("🍗");
  });

  it("returns 🥩 for ground beef", () => {
    expect(getIngredientEmoji("Ground Beef")).toBe("🥩");
  });

  it("returns 🫘 for beans", () => {
    expect(getIngredientEmoji("Beans")).toBe("🫘");
  });

  it("returns 🥬 for lettuce", () => {
    expect(getIngredientEmoji("Lettuce")).toBe("🥬");
  });

  it("returns ☕ for coffee", () => {
    expect(getIngredientEmoji("Coffee")).toBe("☕");
  });

  it("is case-insensitive", () => {
    expect(getIngredientEmoji("CHICKEN")).toBe("🍗");
  });

  it("uses partial match for compound names", () => {
    expect(getIngredientEmoji("Grilled Chicken Breast")).toBe("🍗");
  });

  it("returns fallback 🍽️ for unknown ingredient", () => {
    expect(getIngredientEmoji("xyzzy unknown ingredient")).toBe("🍽️");
  });
});
