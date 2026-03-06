import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import AdminLoginPage from "@/app/admin/login/page";

const pushMock = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: pushMock,
    back: vi.fn(),
    replace: vi.fn(),
    prefetch: vi.fn(),
  }),
}));

const loginMock = vi.fn();
vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    login: (...args: unknown[]) => loginMock(...args),
  },
}));

describe("AdminLoginPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("displays error message when login returns 401", async () => {
    loginMock.mockRejectedValue(new Error("Invalid email or password"));

    render(<AdminLoginPage />);

    fireEvent.change(screen.getByLabelText(/email/i), {
      target: { value: "wrong@test.com" },
    });
    fireEvent.change(screen.getByLabelText(/password/i), {
      target: { value: "badpassword" },
    });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText("Invalid email or password")).toBeInTheDocument();
    });
  });

  it("navigates to /admin on successful login", async () => {
    loginMock.mockResolvedValue({ token: "test-token-123" });

    render(<AdminLoginPage />);

    fireEvent.change(screen.getByLabelText(/email/i), {
      target: { value: "owner@manna.local" },
    });
    fireEvent.change(screen.getByLabelText(/password/i), {
      target: { value: "Owner123!" },
    });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(pushMock).toHaveBeenCalledWith("/admin");
      expect(localStorage.getItem("admin_token")).toBe("test-token-123");
    });
  });
});
