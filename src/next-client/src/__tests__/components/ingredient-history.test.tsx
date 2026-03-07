import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import IngredientHistoryPage from "@/app/admin/(dashboard)/ingredients/[id]/history/page";
import { InventoryChangeType } from "@/types/api";
import type { InventoryLogDto, IngredientDto } from "@/types/api";
import { UnitOfMeasure } from "@/types/api";

vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({ token: "fake-token" }),
}));

vi.mock("next/link", () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), back: vi.fn() }),
  useParams: () => ({ id: "ing-1" }),
}));

const getInventoryHistoryMock = vi.fn();
const getIngredientMock = vi.fn();

vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    getInventoryHistory: (...args: unknown[]) => getInventoryHistoryMock(...args),
    getIngredient: (...args: unknown[]) => getIngredientMock(...args),
  },
}));

const sampleIngredient: IngredientDto = {
  id: "ing-1",
  name: "Jasmine Rice",
  unit: UnitOfMeasure.Oz,
  costPerUnit: 0.05,
  stockQuantity: 300,
  lowStockThreshold: 80,
  active: true,
};

const sampleLogs: InventoryLogDto[] = [
  {
    id: "log-1",
    ingredientId: "ing-1",
    ingredientName: "Jasmine Rice",
    changeType: InventoryChangeType.Received,
    quantityChange: 100,
    newStockQuantity: 400,
    notes: "Sysco delivery",
    createdBy: "owner@manna.local",
    createdAt: "2026-03-06T10:00:00Z",
  },
  {
    id: "log-2",
    ingredientId: "ing-1",
    ingredientName: "Jasmine Rice",
    changeType: InventoryChangeType.OrderDecrement,
    quantityChange: -10,
    newStockQuantity: 390,
    notes: "Order #1042",
    createdBy: null,
    createdAt: "2026-03-06T11:00:00Z",
  },
];

describe("IngredientHistoryPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getIngredientMock.mockResolvedValue(sampleIngredient);
    getInventoryHistoryMock.mockResolvedValue(sampleLogs);
  });

  it("renders chronological list of inventory changes", async () => {
    render(<IngredientHistoryPage />);

    await waitFor(() => {
      expect(screen.getByText("Sysco delivery")).toBeInTheDocument();
      expect(screen.getByText("Order #1042")).toBeInTheDocument();
    });
  });

  it("shows change type badge (Received / Order)", async () => {
    render(<IngredientHistoryPage />);

    await waitFor(() => {
      expect(screen.getByText("Received")).toBeInTheDocument();
      expect(screen.getByText("Order")).toBeInTheDocument();
    });
  });

  it("positive changes show green +X, negative show red -X", async () => {
    render(<IngredientHistoryPage />);

    await waitFor(() => {
      const positive = screen.getByText("+100");
      expect(positive.className).toContain("green");
      const negative = screen.getByText("-10");
      expect(negative.className).toContain("red");
    });
  });

  it("each entry shows resulting stock level", async () => {
    render(<IngredientHistoryPage />);

    await waitFor(() => {
      expect(screen.getByText(/400/)).toBeInTheDocument();
      expect(screen.getByText(/390/)).toBeInTheDocument();
    });
  });

  it("shows 'No history' when log is empty", async () => {
    getInventoryHistoryMock.mockResolvedValue([]);

    render(<IngredientHistoryPage />);

    await waitFor(() => {
      expect(screen.getByText(/no history/i)).toBeInTheDocument();
    });
  });
});
