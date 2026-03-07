import { describe, it, expect } from "vitest";
import { resolveImageUrl } from "@/lib/api";

describe("resolveImageUrl", () => {
  it("returns absolute URLs unchanged", () => {
    expect(resolveImageUrl("https://example.com/img.jpg")).toBe(
      "https://example.com/img.jpg"
    );
  });

  it("prepends API base for uploaded images (/uploads/...)", () => {
    const result = resolveImageUrl("/uploads/menu/abc-123.jpg");
    expect(result).toMatch(/^http.*\/uploads\/menu\/abc-123\.jpg$/);
    expect(result).not.toBe("/uploads/menu/abc-123.jpg");
  });

  it("keeps local public paths unchanged (e.g. /menu/drip-coffee.jpg)", () => {
    // Static seed images live in Next.js public/ dir and must NOT
    // be redirected to the API server
    expect(resolveImageUrl("/menu/drip-coffee.jpg")).toBe(
      "/menu/drip-coffee.jpg"
    );
  });

  it("keeps other local paths unchanged", () => {
    expect(resolveImageUrl("/images/logo.png")).toBe("/images/logo.png");
  });
});
