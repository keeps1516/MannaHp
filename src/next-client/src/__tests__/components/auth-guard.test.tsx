import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";

const replaceMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: vi.fn(),
    back: vi.fn(),
    replace: replaceMock,
    prefetch: vi.fn(),
  }),
}));

const mockMe = vi.fn();
vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    login: vi.fn(),
    me: (...args: unknown[]) => mockMe(...args),
  },
}));

import { AuthProvider } from "@/store/auth-context";
import { AdminAuthGuard } from "@/components/admin/auth-guard";

describe("AdminAuthGuard", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    mockMe.mockRejectedValue(new Error("Not authenticated"));
  });

  it("redirects to /admin/login when not authenticated", async () => {
    render(
      <AuthProvider>
        <AdminAuthGuard>
          <div>Protected Content</div>
        </AdminAuthGuard>
      </AuthProvider>
    );

    await waitFor(() => {
      expect(replaceMock).toHaveBeenCalledWith("/admin/login");
    });
    expect(screen.queryByText("Protected Content")).not.toBeInTheDocument();
  });

  it("renders children when authenticated", async () => {
    localStorage.setItem("admin_token", "valid-token");
    mockMe.mockResolvedValue({
      id: "user-1",
      email: "owner@manna.local",
      displayName: "Owner",
      role: "Owner",
    });

    render(
      <AuthProvider>
        <AdminAuthGuard>
          <div>Protected Content</div>
        </AdminAuthGuard>
      </AuthProvider>
    );

    await waitFor(() => {
      expect(screen.getByText("Protected Content")).toBeInTheDocument();
    });
    expect(replaceMock).not.toHaveBeenCalled();
  });
});
