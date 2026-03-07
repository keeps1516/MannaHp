import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import RestockPage from "@/app/admin/(dashboard)/ingredients/restock/page";
import { UnitOfMeasure } from "@/types/api";
import type { IngredientDto } from "@/types/api";

vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({ token: "fake-token" }),
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

vi.mock("next/link", () => ({
  default: ({
    children,
    href,
  }: {
    children: React.ReactNode;
    href: string;
  }) => <a href={href}>{children}</a>,
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), back: vi.fn() }),
}));

const getIngredientsMock = vi.fn();
const bulkRestockMock = vi.fn();
const createIngredientMock = vi.fn();

vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    getIngredients: (...args: unknown[]) => getIngredientsMock(...args),
    bulkRestock: (...args: unknown[]) => bulkRestockMock(...args),
    createIngredient: (...args: unknown[]) => createIngredientMock(...args),
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
  makeIngredient({ id: "ing-1", name: "Jasmine Rice", stockQuantity: 300 }),
  makeIngredient({
    id: "ing-2",
    name: "Ground Beef",
    unit: UnitOfMeasure.Lb,
    stockQuantity: 50,
  }),
  makeIngredient({
    id: "ing-3",
    name: "Espresso Beans",
    stockQuantity: 200,
    active: false,
  }),
];

describe("RestockPage — Search & Autocomplete", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientsMock.mockResolvedValue(sampleIngredients);
    bulkRestockMock.mockResolvedValue([]);
    createIngredientMock.mockResolvedValue(
      makeIngredient({ id: "new-created", name: "New Item" })
    );
  });

  it("renders search input on page load", async () => {
    render(<RestockPage />);
    await waitFor(() => {
      expect(screen.getByTestId("ingredient-search")).toBeInTheDocument();
    });
  });

  it("shows matching ingredients in dropdown as user types", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");

    await waitFor(() => {
      expect(screen.getByText("Jasmine Rice")).toBeInTheDocument();
    });
  });

  it('shows "Add new ingredient" when no exact match found', async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Avocado");

    await waitFor(() => {
      expect(screen.getByTestId("add-new-ingredient")).toBeInTheDocument();
    });
  });

  it("opens add-to-delivery card when ingredient selected", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));

    expect(screen.getByTestId("add-to-delivery-card")).toBeInTheDocument();
  });
});

describe("RestockPage — Add to Delivery Card", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientsMock.mockResolvedValue(sampleIngredients);
    bulkRestockMock.mockResolvedValue([]);
  });

  it("shows ingredient name, current stock, and unit", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));

    expect(screen.getByText(/Current stock: 300/)).toBeInTheDocument();
  });

  it("shows auto-calculated cost per unit", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));

    await user.type(screen.getByTestId("delivery-quantity"), "100");
    await user.type(screen.getByTestId("delivery-cost"), "20");

    await waitFor(() => {
      expect(screen.getByTestId("cost-per-unit")).toHaveTextContent("$0.2000");
    });
  });

  it('"Add to Delivery" button disabled when quantity is 0', async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));

    expect(screen.getByTestId("add-to-delivery-btn")).toBeDisabled();
  });

  it("adds item to delivery list on confirm", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));

    await user.type(screen.getByTestId("delivery-quantity"), "50");
    await user.type(screen.getByTestId("delivery-cost"), "10");
    await user.click(screen.getByTestId("add-to-delivery-btn"));

    expect(screen.getByTestId("delivery-list")).toBeInTheDocument();
    expect(screen.getByTestId("delivery-item-0")).toBeInTheDocument();
  });
});

describe("RestockPage — New Ingredient Inline Form", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientsMock.mockResolvedValue(sampleIngredients);
    bulkRestockMock.mockResolvedValue([]);
    createIngredientMock.mockResolvedValue(
      makeIngredient({ id: "new-created", name: "Avocado" })
    );
  });

  it("shows name pre-filled from search text", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Avocado");
    await waitFor(() => screen.getByTestId("add-new-ingredient"));
    await user.click(screen.getByTestId("add-new-ingredient"));

    expect(screen.getByTestId("new-ingredient-name")).toHaveValue("Avocado");
  });

  it("shows unit dropdown and threshold input", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Avocado");
    await waitFor(() => screen.getByTestId("add-new-ingredient"));
    await user.click(screen.getByTestId("add-new-ingredient"));

    expect(screen.getByTestId("new-ingredient-unit")).toBeInTheDocument();
    expect(screen.getByTestId("new-ingredient-threshold")).toBeInTheDocument();
  });
});

describe("RestockPage — Delivery List", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientsMock.mockResolvedValue(sampleIngredients);
    bulkRestockMock.mockResolvedValue([]);
  });

  it("displays added items with name, quantity, unit, cost", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    // Add Jasmine Rice
    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));
    await user.type(screen.getByTestId("delivery-quantity"), "50");
    await user.type(screen.getByTestId("delivery-cost"), "10");
    await user.click(screen.getByTestId("add-to-delivery-btn"));

    const item = screen.getByTestId("delivery-item-0");
    expect(item).toHaveTextContent("Jasmine Rice");
    expect(item).toHaveTextContent("50");
    expect(item).toHaveTextContent("$10.00");
  });

  it("remove button removes item from list", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));
    await user.type(screen.getByTestId("delivery-quantity"), "50");
    await user.click(screen.getByTestId("add-to-delivery-btn"));

    expect(screen.getByTestId("delivery-item-0")).toBeInTheDocument();

    await user.click(screen.getByLabelText("Remove Jasmine Rice"));

    expect(screen.queryByTestId("delivery-item-0")).not.toBeInTheDocument();
  });

  it('"Submit Delivery" button disabled when list is empty', async () => {
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    expect(screen.getByTestId("submit-delivery-btn")).toBeDisabled();
  });
});

describe("RestockPage — Submission", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientsMock.mockResolvedValue(sampleIngredients);
    bulkRestockMock.mockResolvedValue([]);
    createIngredientMock.mockResolvedValue(
      makeIngredient({ id: "new-created", name: "Avocado" })
    );
  });

  it("calls bulk restock API on submit", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));
    await user.type(screen.getByTestId("delivery-quantity"), "50");
    await user.type(screen.getByTestId("delivery-cost"), "10");
    await user.click(screen.getByTestId("add-to-delivery-btn"));

    await user.click(screen.getByTestId("submit-delivery-btn"));

    await waitFor(() => {
      expect(bulkRestockMock).toHaveBeenCalledWith("fake-token", {
        items: [
          { ingredientId: "ing-1", quantity: 50, costPaid: 10 },
        ],
      });
    });
  });

  it("creates new ingredients before restocking", async () => {
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Avocado");
    await waitFor(() => screen.getByTestId("add-new-ingredient"));
    await user.click(screen.getByTestId("add-new-ingredient"));
    await user.type(screen.getByTestId("delivery-quantity"), "20");
    await user.type(screen.getByTestId("delivery-cost"), "30");
    await user.click(screen.getByTestId("add-to-delivery-btn"));

    await user.click(screen.getByTestId("submit-delivery-btn"));

    await waitFor(() => {
      expect(createIngredientMock).toHaveBeenCalled();
      expect(bulkRestockMock).toHaveBeenCalledWith("fake-token", {
        items: [
          { ingredientId: "new-created", quantity: 20, costPaid: 30 },
        ],
      });
    });
  });

  it("shows success toast and resets page on success", async () => {
    const { toast } = await import("sonner");
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));
    await user.type(screen.getByTestId("delivery-quantity"), "50");
    await user.click(screen.getByTestId("add-to-delivery-btn"));

    await user.click(screen.getByTestId("submit-delivery-btn"));

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith(
        expect.stringContaining("1")
      );
    });

    // Delivery list should be cleared
    expect(screen.queryByTestId("delivery-list")).not.toBeInTheDocument();
  });

  it("shows error toast on failure", async () => {
    const { toast } = await import("sonner");
    bulkRestockMock.mockRejectedValue(new Error("Network error"));
    const user = userEvent.setup();
    render(<RestockPage />);
    await waitFor(() => screen.getByTestId("ingredient-search"));

    await user.type(screen.getByTestId("ingredient-search"), "Rice");
    await waitFor(() => screen.getByText("Jasmine Rice"));
    await user.click(screen.getByText("Jasmine Rice"));
    await user.type(screen.getByTestId("delivery-quantity"), "50");
    await user.click(screen.getByTestId("add-to-delivery-btn"));

    await user.click(screen.getByTestId("submit-delivery-btn"));

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Failed to submit delivery");
    });
  });
});
