import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { CartDrawer } from "@/components/cart-drawer";
import { CartProvider } from "@/store/cart-context";
import type { CartItem } from "@/types/cart";
import type { CreateOrderResponse } from "@/types/api";
import { PaymentMethod, PaymentStatus, OrderStatus } from "@/types/api";

// --- Mocks ---

const pushMock = vi.fn();
const backMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: pushMock,
    back: backMock,
    replace: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

const createOrderMock = vi.fn();
vi.mock("@/lib/api", () => ({
  api: {
    createOrder: (...args: unknown[]) => createOrderMock(...args),
  },
}));

vi.mock("sonner", () => ({
  toast: { error: vi.fn(), success: vi.fn() },
}));

// --- Helpers ---

function makeCartItem(overrides?: Partial<CartItem>): CartItem {
  return {
    id: "item-1",
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
    ...overrides,
  };
}

function makeOrderResponse(id = "order-123"): CreateOrderResponse {
  return {
    order: {
      id,
      status: OrderStatus.Received,
      paymentMethod: PaymentMethod.InStore,
      paymentStatus: PaymentStatus.Pending,
      subtotal: 5.0,
      taxRate: 0.0825,
      tax: 0.41,
      total: 5.41,
      notes: null,
      createdAt: new Date().toISOString(),
      items: [],
    },
    clientSecret: null,
    stripePublishableKey: null,
  };
}

function renderCartDrawer(items: CartItem[] = [makeCartItem()]) {
  const onOpenChange = vi.fn();

  // We render the CartProvider with pre-populated items by adding them after mount
  const result = render(
    <CartProvider>
      <CartDrawerWithItems items={items} onOpenChange={onOpenChange} />
    </CartProvider>
  );

  return { ...result, onOpenChange };
}

// Helper component that adds items to cart on mount
import { useEffect } from "react";
import { useCart } from "@/store/cart-context";

function CartDrawerWithItems({
  items,
  onOpenChange,
}: {
  items: CartItem[];
  onOpenChange: (open: boolean) => void;
}) {
  const cart = useCart();

  useEffect(() => {
    for (const item of items) {
      cart.addItem({
        menuItem: item.menuItem,
        variant: item.variant,
        selectedIngredients: item.selectedIngredients,
        quantity: item.quantity,
        notes: item.notes,
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return <CartDrawer open={true} onOpenChange={onOpenChange} />;
}

// --- Tests ---

describe("CartDrawer", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders cart items when open", () => {
    renderCartDrawer();
    expect(screen.getByText("Your Order")).toBeInTheDocument();
    expect(screen.getByText(/Latte/)).toBeInTheDocument();
  });

  it("shows empty state when no items", () => {
    renderCartDrawer([]);
    expect(screen.getByText("Your cart is empty")).toBeInTheDocument();
  });

  it("calls createOrder only once on rapid double-click of Pay In-Store", async () => {
    let resolveOrder: (value: CreateOrderResponse) => void;
    createOrderMock.mockImplementation(
      () =>
        new Promise<CreateOrderResponse>((resolve) => {
          resolveOrder = resolve;
        })
    );

    renderCartDrawer();

    const payInStoreBtn = screen.getByRole("button", { name: /Pay In-Store/i });

    // Click twice rapidly
    fireEvent.click(payInStoreBtn);
    fireEvent.click(payInStoreBtn);

    // Only one API call should have been made
    expect(createOrderMock).toHaveBeenCalledTimes(1);

    // Resolve the order to clean up
    resolveOrder!(makeOrderResponse());
    await waitFor(() => {
      expect(createOrderMock).toHaveBeenCalledTimes(1);
    });
  });

  it("navigates to /checkout when Pay with Card is clicked", () => {
    renderCartDrawer();

    const payWithCardBtn = screen.getByRole("button", {
      name: /Pay with Card/i,
    });
    fireEvent.click(payWithCardBtn);

    expect(pushMock).toHaveBeenCalledWith("/checkout");
  });

  it("re-enables submitting after a failed order", async () => {
    createOrderMock
      .mockRejectedValueOnce(new Error("Network error"))
      .mockResolvedValueOnce(makeOrderResponse());

    renderCartDrawer();

    const payInStoreBtn = screen.getByRole("button", { name: /Pay In-Store/i });

    // First click — fails
    fireEvent.click(payInStoreBtn);
    await waitFor(() => {
      expect(createOrderMock).toHaveBeenCalledTimes(1);
    });

    // Button should be re-enabled after failure
    await waitFor(() => {
      expect(payInStoreBtn).not.toBeDisabled();
    });

    // Second click — succeeds
    fireEvent.click(payInStoreBtn);
    await waitFor(() => {
      expect(createOrderMock).toHaveBeenCalledTimes(2);
    });
  });
});
