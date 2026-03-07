import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
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
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), back: vi.fn() }),
}));

const getIngredientsMock = vi.fn();
const bulkRestockMock = vi.fn();

vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    getIngredients: (...args: unknown[]) => getIngredientsMock(...args),
    bulkRestock: (...args: unknown[]) => bulkRestockMock(...args),
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
  makeIngredient({ id: "ing-2", name: "Ground Beef", unit: UnitOfMeasure.Lb, stockQuantity: 50 }),
  makeIngredient({ id: "ing-3", name: "Espresso Beans", stockQuantity: 200, active: false }),
];

describe("RestockPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientsMock.mockResolvedValue(sampleIngredients);
    bulkRestockMock.mockResolvedValue([]);
  });

  it("renders all active ingredients with current stock levels", async () => {
    render(<RestockPage />);

    await waitFor(() => {
      expect(screen.getByText("Jasmine Rice")).toBeInTheDocument();
      expect(screen.getByText("Ground Beef")).toBeInTheDocument();
      // shows current stock
      expect(screen.getByText(/300/)).toBeInTheDocument();
      expect(screen.getByText(/50/)).toBeInTheDocument();
    });
  });

  it("has quantity input fields that accept numeric values", async () => {
    render(<RestockPage />);

    await waitFor(() => {
      screen.getByText("Jasmine Rice");
    });

    const inputs = screen.getAllByPlaceholderText("0");
    expect(inputs.length).toBeGreaterThanOrEqual(2);
    fireEvent.change(inputs[0], { target: { value: "100" } });
    expect(inputs[0]).toHaveValue(100);
  });

  it("Submit Delivery sends only modified ingredients", async () => {
    render(<RestockPage />);

    await waitFor(() => screen.getByText("Jasmine Rice"));

    // Only fill in the first ingredient
    const inputs = screen.getAllByPlaceholderText("0");
    fireEvent.change(inputs[0], { target: { value: "50" } });

    const submitBtn = screen.getByRole("button", { name: /submit delivery/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(bulkRestockMock).toHaveBeenCalledWith(
        "fake-token",
        expect.objectContaining({
          items: expect.arrayContaining([
            expect.objectContaining({ ingredientId: "ing-1", quantity: 50 }),
          ]),
        })
      );
    });

    // Should NOT include the second ingredient (no value entered)
    const callArgs = bulkRestockMock.mock.calls[0][1];
    expect(callArgs.items.length).toBe(1);
  });

  it("shows success toast with count of restocked ingredients", async () => {
    const { toast } = await import("sonner");
    bulkRestockMock.mockResolvedValue([sampleIngredients[0]]);

    render(<RestockPage />);

    await waitFor(() => screen.getByText("Jasmine Rice"));

    const inputs = screen.getAllByPlaceholderText("0");
    fireEvent.change(inputs[0], { target: { value: "50" } });

    fireEvent.click(screen.getByRole("button", { name: /submit delivery/i }));

    await waitFor(() => {
      expect(toast.success).toHaveBeenCalledWith(
        expect.stringContaining("1")
      );
    });
  });

  it("has a notes field for delivery notes", async () => {
    render(<RestockPage />);

    await waitFor(() => screen.getByText("Jasmine Rice"));

    const notesInput = screen.getByPlaceholderText(/delivery|notes/i);
    expect(notesInput).toBeInTheDocument();
  });

  it("includes notes in the request when provided", async () => {
    render(<RestockPage />);

    await waitFor(() => screen.getByText("Jasmine Rice"));

    const inputs = screen.getAllByPlaceholderText("0");
    fireEvent.change(inputs[0], { target: { value: "50" } });

    const notesInput = screen.getByPlaceholderText(/delivery|notes/i);
    fireEvent.change(notesInput, { target: { value: "Sysco delivery" } });

    fireEvent.click(screen.getByRole("button", { name: /submit delivery/i }));

    await waitFor(() => {
      const callArgs = bulkRestockMock.mock.calls[0][1];
      expect(callArgs.items[0].notes).toBe("Sysco delivery");
    });
  });

  it("resets form after successful submission", async () => {
    bulkRestockMock.mockResolvedValue([sampleIngredients[0]]);

    render(<RestockPage />);

    await waitFor(() => screen.getByText("Jasmine Rice"));

    const inputs = screen.getAllByPlaceholderText("0");
    fireEvent.change(inputs[0], { target: { value: "50" } });

    fireEvent.click(screen.getByRole("button", { name: /submit delivery/i }));

    await waitFor(() => {
      // Input should be reset to empty
      expect(inputs[0]).toHaveValue(null);
    });
  });
});
