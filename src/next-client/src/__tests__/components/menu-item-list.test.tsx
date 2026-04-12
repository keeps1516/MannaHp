import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { MenuItemList } from "@/components/admin/menu-item-list";
import { RestockPolicy, type MenuItemDto, type CategoryDto } from "@/types/api";

vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({ token: "test-token" }),
}));

const getMenuItemsMock = vi.fn();
vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    getMenuItems: (...args: unknown[]) => getMenuItemsMock(...args),
    getMenuItem: vi.fn(),
    deleteMenuItem: vi.fn(),
  },
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

const categories: CategoryDto[] = [
  { id: "cat-1", name: "Drinks", sortOrder: 1, active: true },
];

function makeItem(overrides: Partial<MenuItemDto> = {}): MenuItemDto {
  return {
    id: "item-1",
    categoryId: "cat-1",
    name: "Latte",
    description: null,
    imageUrl: null,
    imageApproximate: false,
    isCustomizable: false,
    active: true,
    sortOrder: 1,
    restockPolicy: RestockPolicy.NonReturnable,
    variants: [{ id: "v1", name: "12oz", price: 4.75, sortOrder: 1, active: true }],
    availableIngredients: null,
    ...overrides,
  };
}

describe("MenuItemList — Image Thumbnails", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders image thumbnail for items with imageUrl", async () => {
    getMenuItemsMock.mockResolvedValue([
      makeItem({ imageUrl: "/uploads/menu/latte.jpg" }),
    ]);

    render(<MenuItemList categories={categories} />);

    await waitFor(() => {
      expect(screen.getByTestId("item-thumbnail-item-1")).toBeInTheDocument();
    });
    const img = screen.getByTestId("item-thumbnail-item-1").querySelector("img");
    expect(img).toBeTruthy();
  });

  it("renders letter fallback for items without imageUrl", async () => {
    getMenuItemsMock.mockResolvedValue([
      makeItem({ imageUrl: null }),
    ]);

    render(<MenuItemList categories={categories} />);

    await waitFor(() => {
      expect(screen.getByTestId("item-thumbnail-item-1")).toBeInTheDocument();
    });
    expect(screen.getByTestId("item-thumbnail-item-1").textContent).toContain("L");
  });
});
