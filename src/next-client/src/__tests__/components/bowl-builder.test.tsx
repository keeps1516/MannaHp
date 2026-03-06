import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, act, waitFor } from "@testing-library/react";
import { BowlBuilder } from "@/components/bowl-builder";
import { CartProvider, useCart } from "@/store/cart-context";
import type { MenuItemDto, AvailableIngredientDto } from "@/types/api";
import { UnitOfMeasure } from "@/types/api";
import type { CartItem } from "@/types/cart";

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

function makeIngredient(
  overrides: Partial<AvailableIngredientDto> = {}
): AvailableIngredientDto {
  return {
    id: "ing-1",
    ingredientId: "raw-1",
    ingredientName: "Jasmine Rice",
    customerPrice: 3.0,
    quantityUsed: 10,
    isDefault: false,
    groupName: "Bases",
    sortOrder: 1,
    active: true,
    ingredientUnit: UnitOfMeasure.Oz,
    ...overrides,
  };
}

function makeBowlItem(
  ingredients: AvailableIngredientDto[]
): MenuItemDto {
  return {
    id: "mi-bowl",
    categoryId: "cat-1",
    name: "Burrito Bowl",
    description: "Build your own",
    imageUrl: null,
    imageApproximate: false,
    isCustomizable: true,
    active: true,
    sortOrder: 1,
    variants: [],
    availableIngredients: ingredients,
  };
}

function renderBowl(ingredients: AvailableIngredientDto[], onItemAdded = vi.fn()) {
  return render(
    <CartProvider>
      <BowlBuilder menuItem={makeBowlItem(ingredients)} onItemAdded={onItemAdded} />
    </CartProvider>
  );
}

const rice = makeIngredient();
const chicken = makeIngredient({
  id: "ing-2",
  ingredientId: "raw-2",
  ingredientName: "Chicken",
  customerPrice: 3.0,
  groupName: "Proteins",
  sortOrder: 2,
});
const lettuce = makeIngredient({
  id: "ing-3",
  ingredientId: "raw-3",
  ingredientName: "Lettuce",
  customerPrice: 0.5,
  groupName: "Fresh Toppings",
  sortOrder: 3,
});

describe("BowlBuilder", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("renders ingredient group headings", () => {
    renderBowl([rice, chicken, lettuce]);
    expect(screen.getByText("Bases")).toBeInTheDocument();
    expect(screen.getByText("Proteins")).toBeInTheDocument();
    expect(screen.getByText("Fresh Toppings")).toBeInTheDocument();
  });

  it("renders ingredient names and prices", () => {
    renderBowl([rice, chicken]);
    expect(screen.getByText("Jasmine Rice")).toBeInTheDocument();
    expect(screen.getByText("Chicken")).toBeInTheDocument();
    expect(screen.getAllByText("$3.00")).toHaveLength(2);
  });

  it("starts with $0.00 running total", () => {
    renderBowl([rice]);
    expect(screen.getByText("$0.00")).toBeInTheDocument();
  });

  it("updates running total when ingredient is clicked", () => {
    renderBowl([rice, lettuce]);
    // Running total starts at $0.00
    expect(screen.getByText("Bowl Total").nextElementSibling!.textContent).toBe("$0.00");
    // Click lettuce ($0.50) to add it
    fireEvent.click(screen.getByText("Lettuce"));
    expect(screen.getByText("Bowl Total").nextElementSibling!.textContent).toBe("$0.50");
  });

  it("Add to Cart button is disabled when no selection", () => {
    renderBowl([rice]);
    const addButton = screen.getByRole("button", { name: /add to cart/i });
    expect(addButton).toBeDisabled();
  });

  it("Add to Cart button is enabled after selecting an ingredient", () => {
    renderBowl([rice]);
    fireEvent.click(screen.getByText("Jasmine Rice"));
    const addButton = screen.getByRole("button", { name: /add to cart/i });
    expect(addButton).not.toBeDisabled();
  });

  it("renders default ingredients as pre-selected", () => {
    const defaultRice = makeIngredient({ isDefault: true });
    renderBowl([defaultRice]);
    // Default ingredient starts at qty 1, so total should be $3.00
    const addButton = screen.getByRole("button", { name: /add to cart/i });
    expect(addButton).not.toBeDisabled();
  });

  it("renders 'Build Your Burrito Bowl' heading", () => {
    renderBowl([rice]);
    expect(screen.getByText("Build Your Burrito Bowl")).toBeInTheDocument();
  });

  it("renders Simple Bowl and One Of Everything quick-start buttons", () => {
    renderBowl([rice]);
    expect(screen.getByText("Simple Bowl")).toBeInTheDocument();
    expect(screen.getByText("One Of Everything")).toBeInTheDocument();
  });

  it("shows estimated total with tax in the sticky footer", () => {
    renderBowl([rice, lettuce]);
    // Select rice ($3.00)
    fireEvent.click(screen.getByText("Jasmine Rice"));
    // Should show estimated total with tax (8.25%): $3.00 + $0.25 = $3.25
    expect(screen.getByText(/Est\. with tax: \$3\.25/)).toBeInTheDocument();
  });

  it("filters out inactive ingredients", () => {
    const inactive = makeIngredient({
      id: "ing-inactive",
      ingredientName: "Hidden Ingredient",
      active: false,
    });
    renderBowl([rice, inactive]);
    expect(screen.getByText("Jasmine Rice")).toBeInTheDocument();
    expect(screen.queryByText("Hidden Ingredient")).not.toBeInTheDocument();
  });

  it("pre-populates quantities and bowl name when editing an existing cart item", () => {
    const bowlMenuItem = makeBowlItem([rice, chicken, lettuce]);
    let cartRef: ReturnType<typeof useCart>;

    // Phase 1: render a helper to set up the editing item in cart context
    function CartSetup() {
      cartRef = useCart();
      return null;
    }

    function DelayedBowlBuilder({ show }: { show: boolean }) {
      if (!show) return null;
      return <BowlBuilder menuItem={bowlMenuItem} onItemAdded={vi.fn()} />;
    }

    const { rerender } = render(
      <CartProvider>
        <CartSetup />
        <DelayedBowlBuilder show={false} />
      </CartProvider>
    );

    // Set up editing item
    act(() => {
      cartRef.addItem({
        menuItem: bowlMenuItem,
        variant: null,
        selectedIngredients: [rice, rice, chicken],
        quantity: 3,
        notes: "Dad's Bowl",
      });
    });
    const addedItem = cartRef!.items[0];
    act(() => {
      cartRef.setEditingItem(addedItem);
    });

    // Phase 2: now render the BowlBuilder, which should read editingItem on mount
    rerender(
      <CartProvider>
        <CartSetup />
        <DelayedBowlBuilder show={true} />
      </CartProvider>
    );

    // Bowl name should be pre-populated
    const nameInput = screen.getByPlaceholderText(/Dad's Bowl|Sarah's Veggie/i);
    expect(nameInput).toHaveValue("Dad's Bowl");

    // Running total should reflect 2x rice ($3.00) + 1x chicken ($3.00) = $9.00
    // multiplied by qty 3 = $27.00
    expect(screen.getByText("Bowl Total").nextElementSibling!.textContent).toBe("$27.00");

    // Button should say "Update Cart"
    expect(screen.getByRole("button", { name: /update cart/i })).toBeInTheDocument();
  });

  it("clicking ingredient card increments quantity each time (does not toggle)", () => {
    renderBowl([rice, lettuce]);
    // Click lettuce card area 3 times
    fireEvent.click(screen.getByText("Lettuce"));
    fireEvent.click(screen.getByText("Lettuce"));
    fireEvent.click(screen.getByText("Lettuce"));
    // Should be 3 x $0.50 = $1.50
    expect(screen.getByText("Bowl Total").nextElementSibling!.textContent).toBe("$1.50");
  });

  it("populates bowl builder when editingItem is set after component is already mounted", async () => {
    const bowlMenuItem = makeBowlItem([rice, chicken, lettuce]);
    let cartRef: ReturnType<typeof useCart>;

    function CartSetup() {
      cartRef = useCart();
      return null;
    }

    // Render BowlBuilder immediately (already mounted before editing)
    render(
      <CartProvider>
        <CartSetup />
        <BowlBuilder menuItem={bowlMenuItem} onItemAdded={vi.fn()} />
      </CartProvider>
    );

    // Bowl should start at $0.00
    expect(screen.getByText("Bowl Total").nextElementSibling!.textContent).toBe("$0.00");

    // Simulate: user adds a bowl to cart, then clicks Edit from cart drawer
    // Both actions happen within the same CartProvider instance
    act(() => {
      cartRef.addItem({
        menuItem: bowlMenuItem,
        variant: null,
        selectedIngredients: [rice, chicken],
        quantity: 2,
        notes: "My Bowl",
      });
    });
    const addedItem = cartRef!.items[0];
    act(() => {
      cartRef.setEditingItem(addedItem);
    });

    // The useEffect should fire and re-populate the BowlBuilder
    await waitFor(() => {
      const nameInput = screen.getByPlaceholderText(/Dad's Bowl|Sarah's Veggie/i);
      expect(nameInput).toHaveValue("My Bowl");
    });

    // Running total: rice ($3) + chicken ($3) = $6, qty 2 = $12.00
    expect(screen.getByText("Bowl Total").nextElementSibling!.textContent).toBe("$12.00");

    // Should show Update Cart button
    expect(screen.getByRole("button", { name: /update cart/i })).toBeInTheDocument();
  });

  it("has bottom padding on ingredient content to prevent sticky footer overlap", () => {
    renderBowl([rice, chicken, lettuce]);
    // The container div should have pb-32 (or similar) to push content above the sticky footer
    const container = screen.getByText("Build Your Burrito Bowl").closest(".space-y-8");
    expect(container).toBeTruthy();
    expect(container!.className).toMatch(/pb-\d+/);
  });
});
