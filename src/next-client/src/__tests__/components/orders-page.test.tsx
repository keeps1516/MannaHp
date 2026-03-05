import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";

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

let connectResolve: () => void;
const connectOrderHubMock = vi.fn().mockImplementation(
  () => new Promise<void>((resolve) => { connectResolve = resolve; })
);
vi.mock("@/lib/order-hub", () => ({
  connectOrderHub: (...args: unknown[]) => connectOrderHubMock(...args),
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
    connectOrderHubMock.mockImplementation(
      () => new Promise<void>((resolve) => { connectResolve = resolve; })
    );
  });

  it("shows all three kanban columns even when empty (no hidden columns on mobile)", async () => {
    render(<OrdersPage />);

    const receivedHeading = await screen.findByText("Received");
    const preparingHeading = await screen.findByText("Preparing");
    const readyHeading = await screen.findByText("Ready");

    const receivedColumn = receivedHeading.closest(".space-y-3")!;
    const preparingColumn = preparingHeading.closest(".space-y-3")!;
    const readyColumn = readyHeading.closest(".space-y-3")!;

    expect(receivedColumn.className).not.toMatch(/\bhidden\b/);
    expect(preparingColumn.className).not.toMatch(/\bhidden\b/);
    expect(readyColumn.className).not.toMatch(/\bhidden\b/);

    // Clean up
    connectResolve();
  });

  it("shows 'No orders' empty state in each empty column", async () => {
    render(<OrdersPage />);

    const noOrderTexts = await screen.findAllByText("No orders");
    expect(noOrderTexts).toHaveLength(3);

    connectResolve();
  });

  it("shows 'Connecting...' before SignalR connects, not 'Polling'", async () => {
    render(<OrdersPage />);

    // Wait for loading spinner to disappear and content to render
    await screen.findByText("Received");

    // Before SignalR connects, should show "Connecting..." not "Polling"
    expect(screen.queryByText("Polling")).not.toBeInTheDocument();
    expect(screen.getByText("Connecting...")).toBeInTheDocument();

    // After connection resolves, should show "Live"
    connectResolve();
    await waitFor(() => {
      expect(screen.getByText("Live")).toBeInTheDocument();
    });
  });
});
