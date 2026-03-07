import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, within, fireEvent } from "@testing-library/react";
import IngredientsPage from "@/app/admin/(dashboard)/ingredients/page";
import { UnitOfMeasure } from "@/types/api";
import type { IngredientDto } from "@/types/api";

// Mock auth
vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({ token: "fake-token" }),
}));

// Mock sonner
vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

// Mock next/link
vi.mock("next/link", () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const getIngredientsMock = vi.fn();
const deleteIngredientMock = vi.fn();

vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    getIngredients: (...args: unknown[]) => getIngredientsMock(...args),
    deleteIngredient: (...args: unknown[]) => deleteIngredientMock(...args),
    createIngredient: vi.fn(),
    updateIngredient: vi.fn(),
  },
}));

function makeIngredient(overrides: Partial<IngredientDto> = {}): IngredientDto {
  return {
    id: crypto.randomUUID(),
    name: "Jasmine Rice",
    unit: UnitOfMeasure.Oz,
    costPerUnit: 0.05,
    stockQuantity: 300,
    lowStockThreshold: 80,
    active: true,
    ...overrides,
  };
}

const sampleIngredients: IngredientDto[] = [
  makeIngredient({ id: "ing-1", name: "Jasmine Rice", unit: UnitOfMeasure.Oz, stockQuantity: 300, lowStockThreshold: 80 }),
  makeIngredient({ id: "ing-2", name: "Ground Beef", unit: UnitOfMeasure.Lb, stockQuantity: 5, lowStockThreshold: 10 }), // LOW
  makeIngredient({ id: "ing-3", name: "Espresso Beans", unit: UnitOfMeasure.Oz, stockQuantity: 200, lowStockThreshold: 50, active: false }), // Inactive
];

describe("IngredientsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientsMock.mockResolvedValue(sampleIngredients);
  });

  // Mobile card layout
  it("renders mobile card layout with data-testid", async () => {
    render(<IngredientsPage />);

    await waitFor(() => {
      const mobileList = screen.getByTestId("ingredients-mobile");
      expect(mobileList).toBeInTheDocument();
    });
  });

  it("each mobile card shows ingredient name and combined stock+unit", async () => {
    render(<IngredientsPage />);

    await waitFor(() => {
      const mobileList = screen.getByTestId("ingredients-mobile");
      // "300 oz" for Jasmine Rice
      expect(within(mobileList).getByText("Jasmine Rice")).toBeInTheDocument();
      expect(within(mobileList).getByText("300 oz")).toBeInTheDocument();
      // "5 lb" for Ground Beef
      expect(within(mobileList).getByText("Ground Beef")).toBeInTheDocument();
      expect(within(mobileList).getByText("5 lb")).toBeInTheDocument();
    });
  });

  it("low stock ingredients show LOW badge in mobile card view", async () => {
    render(<IngredientsPage />);

    await waitFor(() => {
      const mobileList = screen.getByTestId("ingredients-mobile");
      // Ground Beef is low stock (5 < 10)
      const beefCard = within(mobileList).getByTestId("mobile-card-ing-2");
      expect(within(beefCard).getByText("LOW")).toBeInTheDocument();
    });
  });

  it("inactive ingredients show muted style in mobile card view", async () => {
    render(<IngredientsPage />);

    await waitFor(() => {
      const mobileList = screen.getByTestId("ingredients-mobile");
      const inactiveCard = within(mobileList).getByTestId("mobile-card-ing-3");
      expect(inactiveCard.className).toContain("opacity");
    });
  });

  // Desktop table layout
  it("renders desktop table layout with data-testid", async () => {
    render(<IngredientsPage />);

    await waitFor(() => {
      const desktopTable = screen.getByTestId("ingredients-desktop");
      expect(desktopTable).toBeInTheDocument();
    });
  });

  // Tap-to-open detail sheet
  it("tapping a mobile card opens the detail sheet", async () => {
    render(<IngredientsPage />);

    await waitFor(() => {
      screen.getByTestId("ingredients-mobile");
    });

    const card = screen.getByTestId("mobile-card-ing-1");
    fireEvent.click(card);

    await waitFor(() => {
      const detailSheet = screen.getByTestId("ingredient-detail");
      expect(detailSheet).toBeInTheDocument();
      expect(within(detailSheet).getByText("Jasmine Rice")).toBeInTheDocument();
    });
  });

  it("detail sheet shows all ingredient fields", async () => {
    render(<IngredientsPage />);

    await waitFor(() => screen.getByTestId("ingredients-mobile"));

    fireEvent.click(screen.getByTestId("mobile-card-ing-1"));

    await waitFor(() => {
      const detail = screen.getByTestId("ingredient-detail");
      expect(within(detail).getByText("Jasmine Rice")).toBeInTheDocument();
      expect(within(detail).getByText(/300/)).toBeInTheDocument(); // stock
      expect(within(detail).getByText(/80/)).toBeInTheDocument(); // threshold
      expect(within(detail).getByText(/\$0\.05/)).toBeInTheDocument(); // cost
      expect(within(detail).getByText(/Ounces/)).toBeInTheDocument(); // unit
    });
  });

  it("detail sheet has edit and deactivate buttons", async () => {
    render(<IngredientsPage />);

    await waitFor(() => screen.getByTestId("ingredients-mobile"));

    fireEvent.click(screen.getByTestId("mobile-card-ing-1"));

    await waitFor(() => {
      const detail = screen.getByTestId("ingredient-detail");
      expect(within(detail).getByRole("button", { name: /edit/i })).toBeInTheDocument();
      expect(within(detail).getByRole("button", { name: /deactivate/i })).toBeInTheDocument();
    });
  });

  // Search filters both views
  it("search filters cards on mobile", async () => {
    render(<IngredientsPage />);

    await waitFor(() => screen.getByTestId("ingredients-mobile"));

    const searchInput = screen.getByPlaceholderText("Search ingredients...");
    fireEvent.change(searchInput, { target: { value: "rice" } });

    const mobileList = screen.getByTestId("ingredients-mobile");
    expect(within(mobileList).getByText("Jasmine Rice")).toBeInTheDocument();
    expect(within(mobileList).queryByText("Ground Beef")).not.toBeInTheDocument();
  });
});
