import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { CategoryCard } from "@/components/category-card";
import type { CategoryDto } from "@/types/api";

const category: CategoryDto = {
  id: "cat-1",
  name: "Burrito Bowls",
  sortOrder: 1,
  active: true,
};

describe("CategoryCard", () => {
  it("renders category name", () => {
    render(<CategoryCard category={category} itemCount={5} />);
    expect(screen.getByText("Burrito Bowls")).toBeInTheDocument();
  });

  it("renders item count with correct pluralization", () => {
    render(<CategoryCard category={category} itemCount={5} />);
    expect(screen.getByText("5 items")).toBeInTheDocument();
  });

  it("renders singular 'item' for count of 1", () => {
    render(<CategoryCard category={category} itemCount={1} />);
    expect(screen.getByText("1 item")).toBeInTheDocument();
  });

  it("renders emoji from category meta", () => {
    render(<CategoryCard category={category} itemCount={3} />);
    expect(screen.getByRole("img")).toHaveTextContent("🌯");
  });

  it("links to category page", () => {
    render(<CategoryCard category={category} itemCount={3} />);
    const link = screen.getByRole("link");
    expect(link).toHaveAttribute("href", "/category/cat-1");
  });
});
