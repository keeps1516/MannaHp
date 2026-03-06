import { describe, it, expect, vi } from "vitest";
import { render, screen, act } from "@testing-library/react";
import { CartProvider, useCart } from "@/store/cart-context";
import type { MenuItemDto, MenuItemVariantDto } from "@/types/api";

// Mock next/image, next/link, and next/navigation (Header renders CartDrawer which uses useRouter)
vi.mock("next/image", () => ({
  default: (props: Record<string, unknown>) => <img {...props} />,
}));

vi.mock("next/link", () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: vi.fn(),
    back: vi.fn(),
    replace: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

vi.mock("sonner", () => ({
  toast: { error: vi.fn(), success: vi.fn() },
}));

vi.mock("@/lib/api", () => ({
  api: { createOrder: vi.fn() },
}));

// Must import Header after mocks
import { Header } from "@/components/header";

describe("Header", () => {
  it("renders without crashing", () => {
    render(
      <CartProvider>
        <Header />
      </CartProvider>
    );
    expect(screen.getByText("Manna + HP")).toBeInTheDocument();
  });

  it("does not show badge when cart is empty", () => {
    render(
      <CartProvider>
        <Header />
      </CartProvider>
    );
    expect(screen.queryByTestId("cart-badge")).not.toBeInTheDocument();
  });

  it("shows item count (not dollar amount) as cart badge", () => {
    let cartRef: ReturnType<typeof useCart>;
    function CartSetup() {
      cartRef = useCart();
      return null;
    }

    render(
      <CartProvider>
        <CartSetup />
        <Header />
      </CartProvider>
    );

    act(() => {
      cartRef!.addItem({
        menuItem: {
          id: "mi-1", categoryId: "cat-1", name: "Latte", description: null,
          imageUrl: null, imageApproximate: false, isCustomizable: false,
          active: true, sortOrder: 0, variants: [], availableIngredients: null,
        },
        variant: { id: "v-1", name: "12oz", price: 4.75, sortOrder: 1, active: true },
        selectedIngredients: null,
        quantity: 2,
        notes: null,
      });
    });

    // Should show item count "2", not dollar amount "$9.50"
    const badge = screen.getByTestId("cart-badge");
    expect(badge.textContent).toBe("2");
    expect(screen.queryByText("$9.50")).not.toBeInTheDocument();
  });
});
