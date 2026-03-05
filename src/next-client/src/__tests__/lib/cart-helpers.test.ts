import { describe, it, expect } from "vitest";
import { getUnitPrice, getLineTotal, getDisplayName, type CartItem } from "@/types/cart";
import type { MenuItemDto, MenuItemVariantDto, AvailableIngredientDto } from "@/types/api";

function makeMenuItem(overrides: Partial<MenuItemDto> = {}): MenuItemDto {
  return {
    id: "item-1",
    categoryId: "cat-1",
    name: "Latte",
    description: null,
    imageUrl: null,
    imageApproximate: false,
    isCustomizable: false,
    active: true,
    sortOrder: 1,
    variants: [],
    availableIngredients: null,
    ...overrides,
  };
}

function makeVariant(overrides: Partial<MenuItemVariantDto> = {}): MenuItemVariantDto {
  return {
    id: "var-1",
    name: "12oz",
    price: 4.75,
    sortOrder: 1,
    active: true,
    ...overrides,
  };
}

function makeIngredient(overrides: Partial<AvailableIngredientDto> = {}): AvailableIngredientDto {
  return {
    id: "ing-1",
    ingredientId: "raw-1",
    ingredientName: "Rice",
    customerPrice: 3.0,
    quantityUsed: 10,
    isDefault: false,
    groupName: "Bases",
    sortOrder: 1,
    active: true,
    ingredientUnit: 0,
    ...overrides,
  };
}

describe("getUnitPrice", () => {
  it("returns variant price for fixed item", () => {
    const item: CartItem = {
      id: "1",
      menuItem: makeMenuItem(),
      variant: makeVariant({ price: 4.75 }),
      selectedIngredients: null,
      quantity: 1,
      notes: null,
    };
    expect(getUnitPrice(item)).toBe(4.75);
  });

  it("returns sum of ingredient prices for customizable item", () => {
    const item: CartItem = {
      id: "2",
      menuItem: makeMenuItem({ isCustomizable: true, name: "Bowl" }),
      variant: null,
      selectedIngredients: [
        makeIngredient({ customerPrice: 3.0 }),
        makeIngredient({ id: "ing-2", customerPrice: 2.0 }),
      ],
      quantity: 1,
      notes: null,
    };
    expect(getUnitPrice(item)).toBe(5.0);
  });

  it("returns 0 when variant is null and no ingredients", () => {
    const item: CartItem = {
      id: "3",
      menuItem: makeMenuItem(),
      variant: null,
      selectedIngredients: null,
      quantity: 1,
      notes: null,
    };
    expect(getUnitPrice(item)).toBe(0);
  });

  it("sums variant price and ingredient prices for fixed items with add-ons", () => {
    const item: CartItem = {
      id: "4",
      menuItem: makeMenuItem(),
      variant: makeVariant({ price: 4.75 }),
      selectedIngredients: [makeIngredient({ customerPrice: 1.0 })],
      quantity: 1,
      notes: null,
    };
    expect(getUnitPrice(item)).toBe(5.75);
  });
});

describe("getLineTotal", () => {
  it("multiplies unit price by quantity", () => {
    const item: CartItem = {
      id: "5",
      menuItem: makeMenuItem(),
      variant: makeVariant({ price: 4.75 }),
      selectedIngredients: null,
      quantity: 3,
      notes: null,
    };
    expect(getLineTotal(item)).toBe(14.25);
  });
});

describe("getDisplayName", () => {
  it("includes variant name in parentheses for fixed items", () => {
    const item: CartItem = {
      id: "6",
      menuItem: makeMenuItem({ name: "Latte" }),
      variant: makeVariant({ name: "16oz" }),
      selectedIngredients: null,
      quantity: 1,
      notes: null,
    };
    expect(getDisplayName(item)).toBe("Latte (16oz)");
  });

  it("returns just item name when no variant", () => {
    const item: CartItem = {
      id: "7",
      menuItem: makeMenuItem({ name: "Latte" }),
      variant: null,
      selectedIngredients: null,
      quantity: 1,
      notes: null,
    };
    expect(getDisplayName(item)).toBe("Latte");
  });

  it("returns just item name for customizable items", () => {
    const item: CartItem = {
      id: "8",
      menuItem: makeMenuItem({ name: "Burrito Bowl", isCustomizable: true }),
      variant: makeVariant({ name: "Regular" }),
      selectedIngredients: [],
      quantity: 1,
      notes: null,
    };
    expect(getDisplayName(item)).toBe("Burrito Bowl");
  });
});
