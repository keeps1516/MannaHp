import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor, within, act } from "@testing-library/react";
import AdminDashboardPage from "@/app/admin/(dashboard)/page";
import { OrderStatus } from "@/types/api";

// Mock auth context
vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({
    user: { email: "owner@manna.local", displayName: "Owner" },
    token: "fake-token",
  }),
}));

// Mock admin API
const getActiveOrdersMock = vi.fn().mockResolvedValue([]);
const getIngredientsMock = vi.fn().mockResolvedValue([]);
const getTodayRevenueMock = vi.fn().mockResolvedValue({ total: 234.56 });

vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    getActiveOrders: (...args: unknown[]) => getActiveOrdersMock(...args),
    getIngredients: (...args: unknown[]) => getIngredientsMock(...args),
    getTodayRevenue: (...args: unknown[]) => getTodayRevenueMock(...args),
  },
}));

// Mock SignalR order hub — capture event handlers
const signalRHandlers: Record<string, (...args: unknown[]) => void> = {};
vi.mock("@/lib/order-hub", () => ({
  connectOrderHub: vi.fn(async () => {
    return {
      on: (event: string, handler: (...args: unknown[]) => void) => {
        signalRHandlers[event] = handler;
      },
      off: vi.fn(),
    };
  }),
  disconnectOrderHub: vi.fn(async () => {}),
}));

// Mock next/link
vi.mock("next/link", () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

function makeOrder(status: OrderStatus) {
  return { id: crypto.randomUUID(), status };
}

function makeIngredient(overrides: Record<string, unknown> = {}) {
  return {
    id: crypto.randomUUID(),
    name: "Test Ingredient",
    unit: 0,
    costPerUnit: 1.0,
    stockQuantity: 100,
    lowStockThreshold: 10,
    active: true,
    ...overrides,
  };
}

describe("AdminDashboardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Clear captured SignalR handlers
    for (const key of Object.keys(signalRHandlers)) {
      delete signalRHandlers[key];
    }
    getActiveOrdersMock.mockResolvedValue([]);
    getIngredientsMock.mockResolvedValue([]);
    getTodayRevenueMock.mockResolvedValue({ total: 234.56 });
  });

  it("displays today's revenue as a dollar amount instead of 'Coming Soon'", async () => {
    render(<AdminDashboardPage />);

    await waitFor(() => {
      expect(screen.queryByText("Coming Soon")).not.toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText("$234.56")).toBeInTheDocument();
    });
  });

  it("displays $0.00 when no revenue today", async () => {
    getTodayRevenueMock.mockResolvedValue({ total: 0 });

    render(<AdminDashboardPage />);

    await waitFor(() => {
      expect(screen.getByText("$0.00")).toBeInTheDocument();
    });
  });

  // F2: Merged Active Orders Card
  it("renders a single orders card that links to /admin/orders", async () => {
    getActiveOrdersMock.mockResolvedValue([
      makeOrder(OrderStatus.Received),
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      const ordersLink = screen.getByTestId("orders-card");
      expect(ordersLink).toBeInTheDocument();
      expect(ordersLink.closest("a")).toHaveAttribute("href", "/admin/orders");
    });
  });

  it("orders card displays active order count", async () => {
    getActiveOrdersMock.mockResolvedValue([
      makeOrder(OrderStatus.Received),
      makeOrder(OrderStatus.Preparing),
      makeOrder(OrderStatus.Ready),
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      const card = screen.getByTestId("orders-card");
      expect(within(card).getByText("3")).toBeInTheDocument();
    });
  });

  it("orders card shows status breakdown", async () => {
    getActiveOrdersMock.mockResolvedValue([
      makeOrder(OrderStatus.Received),
      makeOrder(OrderStatus.Received),
      makeOrder(OrderStatus.Preparing),
      makeOrder(OrderStatus.Ready),
      makeOrder(OrderStatus.Ready),
      makeOrder(OrderStatus.Ready),
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      const card = screen.getByTestId("orders-card");
      expect(within(card).getByText(/2 Received/)).toBeInTheDocument();
      expect(within(card).getByText(/1 Preparing/)).toBeInTheDocument();
      expect(within(card).getByText(/3 Ready/)).toBeInTheDocument();
    });
  });

  it("orders card is the first card in the stats grid", async () => {
    render(<AdminDashboardPage />);

    await waitFor(() => {
      const grid = screen.getByTestId("stats-grid");
      const firstChild = grid.children[0];
      expect(firstChild.querySelector('[data-testid="orders-card"]') ?? firstChild).toHaveAttribute(
        "data-testid",
        "orders-card"
      );
    });
  });

  it("does not render a separate 'View Active Orders' quick-link", async () => {
    render(<AdminDashboardPage />);

    await waitFor(() => {
      expect(screen.getByTestId("orders-card")).toBeInTheDocument();
    });

    expect(screen.queryByText("View Active Orders")).not.toBeInTheDocument();
  });

  // F3: Low Stock Items Card — conditional display
  it("does NOT render low stock card when no ingredients are below threshold", async () => {
    getIngredientsMock.mockResolvedValue([
      makeIngredient({ stockQuantity: 100, lowStockThreshold: 10 }),
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      expect(screen.getByText("$234.56")).toBeInTheDocument();
    });

    expect(screen.queryByText("Low Stock Items")).not.toBeInTheDocument();
  });

  it("renders low stock card when at least one ingredient is below threshold", async () => {
    getIngredientsMock.mockResolvedValue([
      makeIngredient({ stockQuantity: 5, lowStockThreshold: 10 }),
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      expect(screen.getByText("Low Stock Items")).toBeInTheDocument();
    });
  });

  it("low stock card shows correct count of low-stock ingredients", async () => {
    getIngredientsMock.mockResolvedValue([
      makeIngredient({ stockQuantity: 5, lowStockThreshold: 10 }),
      makeIngredient({ stockQuantity: 3, lowStockThreshold: 10 }),
      makeIngredient({ stockQuantity: 100, lowStockThreshold: 10 }), // above threshold
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      const card = screen.getByTestId("low-stock-card");
      expect(within(card).getByText("2")).toBeInTheDocument();
    });
  });

  it("low stock card links to /admin/ingredients", async () => {
    getIngredientsMock.mockResolvedValue([
      makeIngredient({ stockQuantity: 5, lowStockThreshold: 10 }),
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      const card = screen.getByTestId("low-stock-card");
      expect(card.closest("a")).toHaveAttribute("href", "/admin/ingredients");
    });
  });

  it("low stock card updates count when LowStockAlert event received", async () => {
    // Start with 1 low stock ingredient
    getIngredientsMock.mockResolvedValue([
      makeIngredient({ stockQuantity: 5, lowStockThreshold: 10 }),
    ]);

    render(<AdminDashboardPage />);

    await waitFor(() => {
      const card = screen.getByTestId("low-stock-card");
      expect(within(card).getByText("1")).toBeInTheDocument();
    });

    // Simulate a LowStockAlert from SignalR with lowStockCount = 3
    await act(async () => {
      signalRHandlers["LowStockAlert"]?.({ lowStockCount: 3 });
    });

    await waitFor(() => {
      const card = screen.getByTestId("low-stock-card");
      expect(within(card).getByText("3")).toBeInTheDocument();
    });
  });
});
