import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { OrderCard } from "@/components/admin/order-card";
import { OrderStatus, PaymentMethod, PaymentStatus } from "@/types/api";
import type { OrderDto } from "@/types/api";

function makeOrder(overrides: Partial<OrderDto> = {}): OrderDto {
  return {
    id: "abcdef12-0000-0000-0000-000000000001",
    orderNumber: 1042,
    status: OrderStatus.Received,
    paymentMethod: PaymentMethod.InStore,
    paymentStatus: PaymentStatus.Pending,
    subtotal: 9.5,
    taxRate: 0.0825,
    tax: 0.78,
    total: 10.28,
    notes: null,
    createdAt: new Date().toISOString(),
    items: [
      {
        id: "oi-1",
        menuItemName: "Burrito Bowl",
        variantName: null,
        quantity: 1,
        unitPrice: 9.5,
        totalPrice: 9.5,
        notes: null,
        ingredients: [
          {
            ingredientId: "ing-1",
            ingredientName: "Jasmine Rice",
            quantityUsed: 10,
            ingredientUnit: 0,
            priceCharged: 3.0,
          },
          {
            ingredientId: "ing-2",
            ingredientName: "Chicken",
            quantityUsed: 8,
            ingredientUnit: 0,
            priceCharged: 3.0,
          },
        ],
      },
    ],
    ...overrides,
  };
}

const noop = vi.fn().mockResolvedValue(undefined);

describe("OrderCard", () => {
  it("renders human-readable order number instead of GUID", () => {
    render(<OrderCard order={makeOrder()} onAdvance={noop} />);
    expect(screen.getByText("#1042")).toBeInTheDocument();
  });

  it("renders item count", () => {
    render(<OrderCard order={makeOrder()} onAdvance={noop} />);
    expect(screen.getByText("1 item")).toBeInTheDocument();
  });

  it("renders total price", () => {
    render(<OrderCard order={makeOrder()} onAdvance={noop} />);
    expect(screen.getByText("$10.28")).toBeInTheDocument();
  });

  it("renders payment method badge for InStore", () => {
    render(<OrderCard order={makeOrder()} onAdvance={noop} />);
    expect(screen.getByText("In-Store")).toBeInTheDocument();
  });

  it("renders payment method badge for Card", () => {
    render(
      <OrderCard
        order={makeOrder({ paymentMethod: PaymentMethod.Card })}
        onAdvance={noop}
      />
    );
    expect(screen.getByText("Card")).toBeInTheDocument();
  });

  it("renders item name", () => {
    render(<OrderCard order={makeOrder()} onAdvance={noop} />);
    expect(screen.getByText("Burrito Bowl")).toBeInTheDocument();
  });

  it("renders ingredient details", () => {
    render(<OrderCard order={makeOrder()} onAdvance={noop} />);
    expect(screen.getByText("Jasmine Rice")).toBeInTheDocument();
    expect(screen.getByText("Chicken")).toBeInTheDocument();
  });

  it("renders advance hint for Received status", () => {
    render(<OrderCard order={makeOrder()} onAdvance={noop} />);
    expect(screen.getByText("Tap → Start Preparing")).toBeInTheDocument();
  });

  it("renders advance hint for Preparing status", () => {
    render(
      <OrderCard
        order={makeOrder({ status: OrderStatus.Preparing })}
        onAdvance={noop}
      />
    );
    expect(screen.getByText("Tap → Mark Ready")).toBeInTheDocument();
  });

  it("renders item notes when present", () => {
    render(
      <OrderCard
        order={makeOrder({
          items: [
            {
              id: "oi-1",
              menuItemName: "Latte",
              variantName: "16oz",
              quantity: 1,
              unitPrice: 5.25,
              totalPrice: 5.25,
              notes: "extra hot, oat milk",
              ingredients: null,
            },
          ],
        })}
        onAdvance={noop}
      />
    );
    expect(screen.getByText("extra hot, oat milk")).toBeInTheDocument();
  });
});
