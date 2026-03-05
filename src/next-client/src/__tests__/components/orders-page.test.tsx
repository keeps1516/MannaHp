import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";

// --- Mocks ---

vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({ token: "test-token" }),
}));

const getActiveOrdersMock = vi.fn().mockResolvedValue([]);
vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    getActiveOrders: (...args: unknown[]) => getActiveOrdersMock(...args),
    updateOrderStatus: vi.fn(),
  },
}));

vi.mock("@/lib/order-hub", () => ({
  connectOrderHub: vi.fn().mockResolvedValue(undefined),
  disconnectOrderHub: vi.fn(),
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

import OrdersPage from "@/app/admin/(dashboard)/orders/page";

describe("OrdersPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getActiveOrdersMock.mockResolvedValue([]);
  });

  it("shows all three kanban columns even when empty (no hidden columns on mobile)", async () => {
    render(<OrdersPage />);

    // Wait for loading to finish
    const receivedHeading = await screen.findByText("Received");
    const preparingHeading = await screen.findByText("Preparing");
    const readyHeading = await screen.findByText("Ready");

    // All columns should be visible — none should have the "hidden" CSS class
    const receivedColumn = receivedHeading.closest(".space-y-3")!;
    const preparingColumn = preparingHeading.closest(".space-y-3")!;
    const readyColumn = readyHeading.closest(".space-y-3")!;

    expect(receivedColumn.className).not.toMatch(/\bhidden\b/);
    expect(preparingColumn.className).not.toMatch(/\bhidden\b/);
    expect(readyColumn.className).not.toMatch(/\bhidden\b/);
  });

  it("shows 'No orders' empty state in each empty column", async () => {
    render(<OrdersPage />);

    // Wait for loading to finish and check all three columns show empty state
    const noOrderTexts = await screen.findAllByText("No orders");
    expect(noOrderTexts).toHaveLength(3);
  });
});
