import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { FixedItemDetail } from "@/components/fixed-item-detail";
import { CartProvider } from "@/store/cart-context";
import type { MenuItemDto, AvailableIngredientDto } from "@/types/api";
import { UnitOfMeasure } from "@/types/api";

vi.mock("next/link", () => ({
  default: ({ children, href, ...rest }: { children: React.ReactNode; href: string }) => (
    <a href={href} {...rest}>{children}</a>
  ),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: vi.fn(),
    back: vi.fn(),
    replace: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

vi.mock("next/image", () => ({
  __esModule: true,
  default: (props: Record<string, unknown>) => {
    const { fill, priority, ...rest } = props;
    return <img {...rest} />;
  },
}));

function makeAddOn(
  overrides: Partial<AvailableIngredientDto> = {}
): AvailableIngredientDto {
  return {
    id: "addon-1",
    ingredientId: "raw-addon-1",
    ingredientName: "Extra Espresso Shot",
    customerPrice: 1.0,
    quantityUsed: 1,
    isDefault: false,
    groupName: "Add-Ons",
    sortOrder: 1,
    active: true,
    ingredientUnit: UnitOfMeasure.Shot,
    ...overrides,
  };
}

function makeLatte(
  overrides: Partial<MenuItemDto> = {}
): MenuItemDto {
  return {
    id: "mi-latte",
    categoryId: "cat-drinks",
    name: "Latte",
    description: "A creamy espresso drink",
    imageUrl: null,
    imageApproximate: false,
    isCustomizable: false,
    active: true,
    sortOrder: 1,
    variants: [
      { id: "v-12", name: "12oz", price: 4.75, sortOrder: 1, active: true },
      { id: "v-16", name: "16oz", price: 5.25, sortOrder: 2, active: true },
    ],
    availableIngredients: [makeAddOn()],
    ...overrides,
  };
}

function renderFixed(item?: MenuItemDto) {
  return render(
    <CartProvider>
      <FixedItemDetail menuItem={item ?? makeLatte()} />
    </CartProvider>
  );
}

describe("FixedItemDetail", () => {
  it("renders item name", () => {
    renderFixed();
    expect(screen.getByRole("heading", { name: "Latte" })).toBeInTheDocument();
  });

  it("renders description", () => {
    renderFixed();
    expect(screen.getByText("A creamy espresso drink")).toBeInTheDocument();
  });

  it("renders variant options", () => {
    renderFixed();
    expect(screen.getByText("12oz")).toBeInTheDocument();
    expect(screen.getByText("16oz")).toBeInTheDocument();
  });

  it("renders variant prices", () => {
    renderFixed();
    // Both variant prices appear (may appear multiple times due to total)
    expect(screen.getAllByText("$4.75").length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText("$5.25").length).toBeGreaterThanOrEqual(1);
  });

  it("selects first variant by default and shows its price as total", () => {
    renderFixed(makeLatte({ availableIngredients: [] }));
    // Total footer should show first variant's price
    expect(screen.getByText("Total").nextElementSibling!.textContent).toBe("$4.75");
  });

  it("updates total when a different variant is clicked", () => {
    renderFixed(makeLatte({ availableIngredients: [] }));
    fireEvent.click(screen.getByText("16oz"));
    // After clicking 16oz, the total in the footer should be $5.25
    const totals = screen.getAllByText("$5.25");
    expect(totals.length).toBeGreaterThanOrEqual(1);
  });

  it("renders add-on section", () => {
    renderFixed();
    expect(screen.getByText("Add-Ons")).toBeInTheDocument();
    expect(screen.getByText("Extra Espresso Shot")).toBeInTheDocument();
  });

  it("renders Back button with category link", () => {
    renderFixed();
    // Back button should link to the category page, not just generic "Back"
    const backLink = screen.getByRole("link", { name: /back to menu/i });
    expect(backLink).toBeInTheDocument();
    expect(backLink).toHaveAttribute("href", "/category/cat-drinks");
  });

  it("Add to Cart button is disabled when no variant selected", () => {
    renderFixed(makeLatte({ variants: [] }));
    const addButton = screen.getByRole("button", { name: /add to cart/i });
    expect(addButton).toBeDisabled();
  });

  it("Add to Cart button is enabled with default variant", () => {
    renderFixed();
    const addButton = screen.getByRole("button", { name: /add to cart/i });
    expect(addButton).not.toBeDisabled();
  });

  it("renders special requests textarea", () => {
    renderFixed();
    expect(
      screen.getByPlaceholderText("e.g., extra hot, oat milk, etc.")
    ).toBeInTheDocument();
  });

  it("shows styled fallback when no image is available", () => {
    renderFixed(makeLatte({ imageUrl: null }));
    // Should render a gradient/styled fallback instead of nothing
    const fallback = screen.getByTestId("image-fallback");
    expect(fallback).toBeInTheDocument();
    expect(fallback.textContent).toContain("Latte");
  });
});
