import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import OrderConfirmationPage from "@/app/(customer)/order/[id]/page";
import { OrderStatus, PaymentMethod, PaymentStatus } from "@/types/api";

const pushMock = vi.fn();
vi.mock("next/navigation", () => ({
  useParams: () => ({ id: "order-abc" }),
  useRouter: () => ({
    push: pushMock,
    back: vi.fn(),
    replace: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

const getOrderMock = vi.fn();
vi.mock("@/lib/api", () => ({
  api: {
    getOrder: (...args: unknown[]) => getOrderMock(...args),
  },
}));

describe("OrderConfirmationPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows friendly 'Order not found' message with menu link on 404", async () => {
    getOrderMock.mockRejectedValue(new Error("API error 404: "));

    render(<OrderConfirmationPage />);

    await waitFor(() => {
      expect(screen.getByText("Order not found")).toBeInTheDocument();
    });

    // Should NOT show raw API error
    expect(screen.queryByText(/API error 404/)).not.toBeInTheDocument();

    // Should have a link/button back to menu
    expect(screen.getByText(/back to menu/i)).toBeInTheDocument();
  });

  it("displays order number prominently at the top of the page", async () => {
    getOrderMock.mockResolvedValue({
      id: "order-abc",
      orderNumber: 1042,
      status: OrderStatus.Received,
      paymentMethod: PaymentMethod.InStore,
      paymentStatus: PaymentStatus.Pending,
      subtotal: 12.0,
      taxRate: 0.0825,
      tax: 0.99,
      total: 12.99,
      notes: null,
      createdAt: new Date().toISOString(),
      items: [],
    });

    render(<OrderConfirmationPage />);

    await waitFor(() => {
      expect(screen.getByText("#1042")).toBeInTheDocument();
    });

    // Order number should appear before "Order Placed!" text
    const orderNumber = screen.getByText("#1042");
    const orderPlaced = screen.getByText("Order Placed!");
    const parent = orderNumber.parentElement!;
    const children = Array.from(parent.children);
    const numIdx = children.indexOf(orderNumber);
    const placedIdx = children.indexOf(orderPlaced);
    expect(numIdx).toBeLessThan(placedIdx);
  });
});
