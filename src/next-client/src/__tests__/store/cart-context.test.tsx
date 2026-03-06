import { describe, it, expect, beforeEach } from "vitest";
import { render, screen, act } from "@testing-library/react";
import { CartProvider, useCart } from "@/store/cart-context";
import type { MenuItemDto, MenuItemVariantDto, AvailableIngredientDto } from "@/types/api";
import type { CartItem } from "@/types/cart";

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
  return { id: "var-1", name: "12oz", price: 4.75, sortOrder: 1, active: true, ...overrides };
}

function makeIngredient(overrides: Partial<AvailableIngredientDto> = {}): AvailableIngredientDto {
  return {
    id: "ing-1", ingredientId: "raw-1", ingredientName: "Rice",
    customerPrice: 3.0, quantityUsed: 10, isDefault: false,
    groupName: "Bases", sortOrder: 1, active: true, ingredientUnit: 0,
    ...overrides,
  };
}

function makeCartItemPayload(overrides: Partial<Omit<CartItem, "id">> = {}): Omit<CartItem, "id"> {
  return {
    menuItem: makeMenuItem(),
    variant: makeVariant(),
    selectedIngredients: null,
    quantity: 1,
    notes: null,
    ...overrides,
  };
}

// Test helper component that exposes cart state
function CartConsumer({ onRender }: { onRender: (cart: ReturnType<typeof useCart>) => void }) {
  const cart = useCart();
  onRender(cart);
  return (
    <div>
      <span data-testid="count">{cart.itemCount}</span>
      <span data-testid="subtotal">{cart.subtotal}</span>
      <span data-testid="tax">{cart.tax}</span>
      <span data-testid="total">{cart.total}</span>
    </div>
  );
}

describe("CartContext", () => {
  let lastCart: ReturnType<typeof useCart>;
  const capture = (cart: ReturnType<typeof useCart>) => { lastCart = cart; };

  beforeEach(() => {
    localStorage.clear();
  });

  function renderCart() {
    render(
      <CartProvider>
        <CartConsumer onRender={capture} />
      </CartProvider>
    );
  }

  it("starts with empty cart", () => {
    renderCart();
    expect(lastCart.items).toHaveLength(0);
    expect(lastCart.itemCount).toBe(0);
  });

  it("addItem increases items array", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    expect(lastCart.items).toHaveLength(1);
  });

  it("removeItem removes item by ID", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    const id = lastCart.items[0].id;
    act(() => lastCart.removeItem(id));
    expect(lastCart.items).toHaveLength(0);
  });

  it("updateQuantity changes item quantity", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    const id = lastCart.items[0].id;
    act(() => lastCart.updateQuantity(id, 5));
    expect(lastCart.items[0].quantity).toBe(5);
  });

  it("updateQuantity to 0 removes item", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    const id = lastCart.items[0].id;
    act(() => lastCart.updateQuantity(id, 0));
    expect(lastCart.items).toHaveLength(0);
  });

  it("clear empties all items", () => {
    renderCart();
    act(() => {
      lastCart.addItem(makeCartItemPayload());
      lastCart.addItem(makeCartItemPayload({ variant: makeVariant({ id: "var-2", price: 5.25 }) }));
    });
    act(() => lastCart.clear());
    expect(lastCart.items).toHaveLength(0);
  });

  it("calculates subtotal for fixed items", () => {
    renderCart();
    act(() => {
      lastCart.addItem(makeCartItemPayload({ variant: makeVariant({ price: 4.75 }), quantity: 2 }));
    });
    expect(lastCart.subtotal).toBe(9.50);
  });

  it("calculates subtotal for customizable items", () => {
    renderCart();
    act(() => {
      lastCart.addItem(makeCartItemPayload({
        menuItem: makeMenuItem({ isCustomizable: true, name: "Bowl" }),
        variant: null,
        selectedIngredients: [
          makeIngredient({ customerPrice: 3.0 }),
          makeIngredient({ id: "ing-2", customerPrice: 2.0 }),
        ],
        quantity: 1,
      }));
    });
    expect(lastCart.subtotal).toBe(5.0);
  });

  it("calculates tax at 8.25% rounded to 2 decimals", () => {
    renderCart();
    act(() => {
      // subtotal = $10.00 → tax = $0.83 (rounded)
      lastCart.addItem(makeCartItemPayload({ variant: makeVariant({ price: 10.0 }) }));
    });
    expect(lastCart.tax).toBe(0.83); // 10 * 0.0825 = 0.825 → rounded = 0.83
  });

  it("total equals subtotal + tax", () => {
    renderCart();
    act(() => {
      lastCart.addItem(makeCartItemPayload({ variant: makeVariant({ price: 10.0 }) }));
    });
    expect(lastCart.total).toBe(lastCart.subtotal + lastCart.tax);
  });

  it("removeItem with nonexistent ID does not error", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    expect(() => {
      act(() => lastCart.removeItem("nonexistent-id"));
    }).not.toThrow();
    expect(lastCart.items).toHaveLength(1);
  });
});

describe("CartContext editingItem", () => {
  let lastCart: ReturnType<typeof useCart>;
  const capture = (cart: ReturnType<typeof useCart>) => { lastCart = cart; };

  beforeEach(() => {
    localStorage.clear();
  });

  function renderCart() {
    render(
      <CartProvider>
        <CartConsumer onRender={capture} />
      </CartProvider>
    );
  }

  it("editingItem starts as null", () => {
    renderCart();
    expect(lastCart.editingItem).toBeNull();
  });

  it("setEditingItem stores the item being edited", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    const item = lastCart.items[0];
    act(() => lastCart.setEditingItem(item));
    expect(lastCart.editingItem).not.toBeNull();
    expect(lastCart.editingItem!.id).toBe(item.id);
  });

  it("clearEditingItem resets editingItem to null", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    const item = lastCart.items[0];
    act(() => lastCart.setEditingItem(item));
    act(() => lastCart.clearEditingItem());
    expect(lastCart.editingItem).toBeNull();
  });

  it("updateItem replaces an existing cart item by ID", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload({
      menuItem: makeMenuItem({ isCustomizable: true, name: "Bowl" }),
      variant: null,
      selectedIngredients: [makeIngredient({ customerPrice: 3.0 })],
      quantity: 1,
      notes: "Old Bowl",
    })));
    const id = lastCart.items[0].id;

    act(() => lastCart.updateItem(id, {
      menuItem: makeMenuItem({ isCustomizable: true, name: "Bowl" }),
      variant: null,
      selectedIngredients: [
        makeIngredient({ customerPrice: 3.0 }),
        makeIngredient({ id: "ing-2", customerPrice: 2.0 }),
      ],
      quantity: 2,
      notes: "Updated Bowl",
    }));

    expect(lastCart.items).toHaveLength(1);
    expect(lastCart.items[0].id).toBe(id);
    expect(lastCart.items[0].notes).toBe("Updated Bowl");
    expect(lastCart.items[0].quantity).toBe(2);
    expect(lastCart.items[0].selectedIngredients).toHaveLength(2);
  });
});

describe("CartContext dynamic tax rate", () => {
  let lastCart: ReturnType<typeof useCart>;
  const capture = (cart: ReturnType<typeof useCart>) => { lastCart = cart; };

  beforeEach(() => {
    localStorage.clear();
  });

  it("uses initialTaxRate when provided", () => {
    render(
      <CartProvider initialTaxRate={0.10}>
        <CartConsumer onRender={capture} />
      </CartProvider>
    );
    act(() => lastCart.addItem(makeCartItemPayload({ variant: makeVariant({ price: 10.0 }) })));
    expect(lastCart.taxRate).toBe(0.10);
    expect(lastCart.tax).toBe(1.00); // 10 * 0.10 = 1.00
  });

  it("defaults to 0.0825 when no initialTaxRate provided", () => {
    render(
      <CartProvider>
        <CartConsumer onRender={capture} />
      </CartProvider>
    );
    expect(lastCart.taxRate).toBe(0.0825);
  });
});

describe("CartContext localStorage persistence", () => {
  let lastCart: ReturnType<typeof useCart>;
  const capture = (cart: ReturnType<typeof useCart>) => { lastCart = cart; };

  beforeEach(() => {
    localStorage.clear();
  });

  function renderCart() {
    return render(
      <CartProvider>
        <CartConsumer onRender={capture} />
      </CartProvider>
    );
  }

  it("persists cart items to localStorage when an item is added", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    const stored = localStorage.getItem("manna-cart");
    expect(stored).not.toBeNull();
    const parsed = JSON.parse(stored!);
    expect(parsed.items).toHaveLength(1);
    expect(parsed.items[0].menuItem.name).toBe("Latte");
  });

  it("hydrates cart from localStorage on mount", () => {
    // Pre-populate localStorage with a cart item
    const seedItems = [{
      id: "seed-1",
      menuItem: makeMenuItem(),
      variant: makeVariant(),
      selectedIngredients: null,
      quantity: 2,
      notes: null,
    }];
    localStorage.setItem("manna-cart", JSON.stringify({ items: seedItems }));

    renderCart();
    expect(lastCart.items).toHaveLength(1);
    expect(lastCart.items[0].quantity).toBe(2);
    expect(lastCart.items[0].menuItem.name).toBe("Latte");
  });

  it("clears localStorage when cart is cleared", () => {
    renderCart();
    act(() => lastCart.addItem(makeCartItemPayload()));
    expect(localStorage.getItem("manna-cart")).not.toBeNull();

    act(() => lastCart.clear());
    const stored = localStorage.getItem("manna-cart");
    expect(stored).not.toBeNull();
    const parsed = JSON.parse(stored!);
    expect(parsed.items).toHaveLength(0);
  });
});
