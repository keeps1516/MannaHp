import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, act, waitFor } from "@testing-library/react";
import { AuthProvider, useAuth } from "@/store/auth-context";

// Mock admin-api
const mockLogin = vi.fn();
const mockMe = vi.fn();

vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    login: (...args: unknown[]) => mockLogin(...args),
    me: (...args: unknown[]) => mockMe(...args),
  },
}));

function AuthConsumer({ onRender }: { onRender: (auth: ReturnType<typeof useAuth>) => void }) {
  const auth = useAuth();
  onRender(auth);
  return (
    <div>
      <span data-testid="user">{auth.user?.email ?? "none"}</span>
      <span data-testid="loading">{auth.isLoading.toString()}</span>
    </div>
  );
}

describe("AuthContext", () => {
  let lastAuth: ReturnType<typeof useAuth>;
  const capture = (auth: ReturnType<typeof useAuth>) => { lastAuth = auth; };

  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    mockMe.mockRejectedValue(new Error("Not authenticated"));
  });

  it("starts with no user and finishes loading", async () => {
    render(
      <AuthProvider>
        <AuthConsumer onRender={capture} />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(lastAuth.isLoading).toBe(false);
    });
    expect(lastAuth.user).toBeNull();
  });

  it("login sets user and persists token to localStorage", async () => {
    mockLogin.mockResolvedValue({
      token: "test-token",
      email: "owner@manna.local",
      displayName: "Owner",
      role: "Owner",
      expiresAt: "2026-01-01",
    });

    render(
      <AuthProvider>
        <AuthConsumer onRender={capture} />
      </AuthProvider>
    );

    await waitFor(() => expect(lastAuth.isLoading).toBe(false));

    await act(async () => {
      await lastAuth.login("owner@manna.local", "MannaOwner123!");
    });

    expect(lastAuth.user).not.toBeNull();
    expect(lastAuth.user!.email).toBe("owner@manna.local");
    expect(localStorage.getItem("admin_token")).toBe("test-token");
  });

  it("logout clears user and token from localStorage", async () => {
    mockLogin.mockResolvedValue({
      token: "test-token",
      email: "owner@manna.local",
      displayName: "Owner",
      role: "Owner",
      expiresAt: "2026-01-01",
    });

    render(
      <AuthProvider>
        <AuthConsumer onRender={capture} />
      </AuthProvider>
    );

    await waitFor(() => expect(lastAuth.isLoading).toBe(false));

    await act(async () => {
      await lastAuth.login("owner@manna.local", "MannaOwner123!");
    });

    act(() => lastAuth.logout());

    expect(lastAuth.user).toBeNull();
    expect(lastAuth.token).toBeNull();
    expect(localStorage.getItem("admin_token")).toBeNull();
  });

  it("hydrates user from localStorage on mount", async () => {
    localStorage.setItem("admin_token", "stored-token");
    mockMe.mockResolvedValue({
      id: "user-1",
      email: "staff@manna.local",
      displayName: "Staff",
      role: "Staff",
    });

    render(
      <AuthProvider>
        <AuthConsumer onRender={capture} />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(lastAuth.isLoading).toBe(false);
    });

    expect(lastAuth.user).not.toBeNull();
    expect(lastAuth.user!.email).toBe("staff@manna.local");
    expect(mockMe).toHaveBeenCalledWith("stored-token");
  });

  it("removes invalid token from localStorage on hydration failure", async () => {
    localStorage.setItem("admin_token", "bad-token");
    mockMe.mockRejectedValue(new Error("Unauthorized"));

    render(
      <AuthProvider>
        <AuthConsumer onRender={capture} />
      </AuthProvider>
    );

    await waitFor(() => {
      expect(lastAuth.isLoading).toBe(false);
    });

    expect(lastAuth.user).toBeNull();
    expect(localStorage.getItem("admin_token")).toBeNull();
  });
});
