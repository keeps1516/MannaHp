import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, waitFor } from "@testing-library/react";
import { CartProvider } from "@/store/cart-context";
import type { CartItem } from "@/types/cart";
import type { CreateOrderResponse } from "@/types/api";
import { PaymentMethod, PaymentStatus, OrderStatus } from "@/types/api";
import { useState, useEffect } from "react";
import { useCart } from "@/store/cart-context";
import CheckoutPage from "@/app/(customer)/checkout/page";

// --- Mocks ---

const pushMock = vi.fn();
const replaceMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: pushMock,
    back: vi.fn(),
    replace: replaceMock,
    prefetch: vi.fn(),
  }),
}));

const createOrderMock = vi.fn();
vi.mock("@/lib/api", () => ({
  api: {
    createOrder: (...args: unknown[]) => createOrderMock(...args),
    confirmPayment: vi.fn(),
  },
}));

vi.mock("sonner", () => ({
  toast: { error: vi.fn(), success: vi.fn() },
}));

// Mock Stripe modules
vi.mock("@stripe/stripe-js", () => ({
  loadStripe: vi.fn(() => Promise.resolve(null)),
}));

vi.mock("@stripe/react-stripe-js", () => ({
  Elements: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  PaymentElement: () => <div data-testid="payment-element" />,
  useStripe: () => null,
  useElements: () => null,
}));

// --- Helpers ---

function makeCartItem(): Omit<CartItem, "id"> {
  return {
    menuItem: {
      id: "mi-1",
      categoryId: "cat-1",
      name: "Latte",
      description: null,
      imageUrl: null,
      imageApproximate: false,
      isCustomizable: false,
      active: true,
      sortOrder: 0,
      variants: [],
      availableIngredients: null,
    },
    variant: { id: "v-1", name: "Large", price: 5.0, sortOrder: 0, active: true },
    selectedIngredients: null,
    quantity: 1,
    notes: null,
  };
}

function makeOrderResponse(): CreateOrderResponse {
  return {
    order: {
      id: "order-123",
      orderNumber: 1001,
      status: OrderStatus.Received,
      paymentMethod: PaymentMethod.Card,
      paymentStatus: PaymentStatus.Pending,
      subtotal: 5.0,
      taxRate: 0.0825,
      tax: 0.41,
      total: 5.41,
      notes: null,
      createdAt: new Date().toISOString(),
      items: [],
    },
    clientSecret: "pi_test_secret_123",
    stripePublishableKey: "pk_test_123",
  };
}

// Populates cart first, then renders CheckoutPage only after cart has items
function CheckoutWithCart({ items }: { items: Omit<CartItem, "id">[] }) {
  const cart = useCart();
  const [ready, setReady] = useState(items.length === 0);

  useEffect(() => {
    if (items.length === 0) return;
    for (const item of items) {
      cart.addItem(item);
    }
    setReady(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (!ready) return null;
  return <CheckoutPage />;
}

// --- Tests ---

describe("CheckoutPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("calls createOrder exactly once (no duplicate from re-mount)", async () => {
    createOrderMock.mockResolvedValue(makeOrderResponse());

    render(
      <CartProvider>
        <CheckoutWithCart items={[makeCartItem()]} />
      </CartProvider>
    );

    await waitFor(() => {
      expect(createOrderMock).toHaveBeenCalled();
    });

    // Should only have been called once, not twice
    expect(createOrderMock).toHaveBeenCalledTimes(1);
  });

  it("sends PaymentMethod.Card in the createOrder request", async () => {
    createOrderMock.mockResolvedValue(makeOrderResponse());

    render(
      <CartProvider>
        <CheckoutWithCart items={[makeCartItem()]} />
      </CartProvider>
    );

    await waitFor(() => {
      expect(createOrderMock).toHaveBeenCalled();
    });

    const request = createOrderMock.mock.calls[0][0];
    expect(request.paymentMethod).toBe(PaymentMethod.Card);
  });

  it("shows clear error when Stripe is not configured", async () => {
    createOrderMock.mockResolvedValue({
      ...makeOrderResponse(),
      clientSecret: null,
      stripePublishableKey: null,
    });

    const { container } = render(
      <CartProvider>
        <CheckoutWithCart items={[makeCartItem()]} />
      </CartProvider>
    );

    await waitFor(() => {
      expect(container.textContent).toContain(
        "Card payments are not yet available"
      );
    });
  });

  it("redirects to home when cart is empty", async () => {
    render(
      <CartProvider>
        <CheckoutWithCart items={[]} />
      </CartProvider>
    );

    await waitFor(() => {
      expect(replaceMock).toHaveBeenCalledWith("/");
    });

    // Should NOT have called createOrder
    expect(createOrderMock).not.toHaveBeenCalled();
  });
});
