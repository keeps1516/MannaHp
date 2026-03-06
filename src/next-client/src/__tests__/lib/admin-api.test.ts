import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { adminApi } from "@/lib/admin-api";

describe("adminApi.login", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    localStorage.clear();
    // Reset window.location mock
    Object.defineProperty(window, "location", {
      value: { href: "/admin/login" },
      writable: true,
    });
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("throws a user-friendly error on 401 without redirecting", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      text: () => Promise.resolve(""),
    });

    await expect(adminApi.login({ email: "bad@test.com", password: "wrong" }))
      .rejects
      .toThrow("Invalid email or password");

    // Should NOT redirect — we're already on the login page
    expect(window.location.href).toBe("/admin/login");
  });

  it("returns token on successful login", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ token: "abc123" }),
    });

    const result = await adminApi.login({ email: "owner@manna.local", password: "Owner123!" });
    expect(result.token).toBe("abc123");
  });
});
