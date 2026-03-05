import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import HomePage from "@/app/(customer)/page";

vi.mock("next/link", () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

vi.mock("next/image", () => ({
  default: (props: Record<string, unknown>) => <img {...props} />,
}));

// API mock that stays pending (never resolves) to capture loading state
const getCategoriesMock = vi.fn();
const getMenuItemsMock = vi.fn();

vi.mock("@/lib/api", () => ({
  api: {
    getCategories: () => getCategoriesMock(),
    getMenuItems: () => getMenuItemsMock(),
  },
}));

describe("HomePage loading state", () => {
  it("shows skeleton placeholders instead of a bare spinner while loading", () => {
    // Make API calls hang forever to capture loading state
    getCategoriesMock.mockReturnValue(new Promise(() => {}));
    getMenuItemsMock.mockReturnValue(new Promise(() => {}));

    render(<HomePage />);

    // Should have skeleton placeholders (elements with animate-pulse class)
    const skeletons = document.querySelectorAll("[class*='animate-pulse']");
    expect(skeletons.length).toBeGreaterThan(0);
  });
});
