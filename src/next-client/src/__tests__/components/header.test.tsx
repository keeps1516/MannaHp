import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { CartProvider } from "@/store/cart-context";

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

  it("does not show subtotal when cart is empty", () => {
    render(
      <CartProvider>
        <Header />
      </CartProvider>
    );
    expect(screen.queryByText(/\$/)).not.toBeInTheDocument();
  });
});
