import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import AdminDashboardPage from "@/app/admin/(dashboard)/page";

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

// Mock next/link
vi.mock("next/link", () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

describe("AdminDashboardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
});
