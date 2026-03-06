import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { ItemCard } from "@/components/item-card";
import type { MenuItemDto } from "@/types/api";

function makeItem(overrides: Partial<MenuItemDto> = {}): MenuItemDto {
  return {
    id: "item-1",
    categoryId: "cat-1",
    name: "Latte",
    description: "A creamy espresso drink",
    imageUrl: null,
    imageApproximate: false,
    isCustomizable: false,
    active: true,
    sortOrder: 1,
    variants: [
      { id: "v1", name: "12oz", price: 4.75, sortOrder: 1, active: true },
      { id: "v2", name: "16oz", price: 5.25, sortOrder: 2, active: true },
    ],
    availableIngredients: null,
    ...overrides,
  };
}

describe("ItemCard", () => {
  it("renders item name", () => {
    render(<ItemCard item={makeItem()} />);
    expect(screen.getByText("Latte")).toBeInTheDocument();
  });

  it("renders description", () => {
    render(<ItemCard item={makeItem()} />);
    expect(screen.getByText("A creamy espresso drink")).toBeInTheDocument();
  });

  it("renders price range for multiple variants", () => {
    render(<ItemCard item={makeItem()} />);
    expect(screen.getByText("$4.75 – $5.25")).toBeInTheDocument();
  });

  it("renders single price for one variant", () => {
    render(
      <ItemCard
        item={makeItem({
          variants: [{ id: "v1", name: "Regular", price: 3.0, sortOrder: 1, active: true }],
        })}
      />
    );
    expect(screen.getByText("$3.00")).toBeInTheDocument();
  });

  it("renders variant summary for multiple variants", () => {
    render(<ItemCard item={makeItem()} />);
    expect(screen.getByText("12oz · 16oz")).toBeInTheDocument();
  });

  it("does not render variant summary for single variant", () => {
    render(
      <ItemCard
        item={makeItem({
          variants: [{ id: "v1", name: "Regular", price: 3.0, sortOrder: 1, active: true }],
        })}
      />
    );
    expect(screen.queryByText("Regular")).not.toBeInTheDocument();
  });

  it("links to item page", () => {
    render(<ItemCard item={makeItem()} />);
    const link = screen.getByRole("link");
    expect(link).toHaveAttribute("href", "/item/item-1");
  });

  it("shows a fallback thumbnail when imageUrl is null", () => {
    render(<ItemCard item={makeItem({ imageUrl: null })} />);
    // Should render a fallback thumbnail with the first letter of the item name
    const fallback = screen.getByTestId("item-thumbnail-fallback");
    expect(fallback).toBeInTheDocument();
    expect(fallback.textContent).toContain("L");
  });
});
