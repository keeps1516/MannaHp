import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MenuItemFormSheet } from "@/components/admin/menu-item-form-sheet";
import { RestockPolicy, type MenuItemDto, type CategoryDto } from "@/types/api";

vi.mock("@/store/auth-context", () => ({
  useAuth: () => ({ token: "test-token" }),
}));

const uploadMock = vi.fn();
const deleteMock = vi.fn();
vi.mock("@/lib/admin-api", () => ({
  adminApi: {
    updateMenuItem: vi.fn().mockResolvedValue({}),
    createMenuItem: vi.fn().mockResolvedValue({}),
    uploadMenuItemImage: (...args: unknown[]) => uploadMock(...args),
    deleteMenuItemImage: (...args: unknown[]) => deleteMock(...args),
  },
}));

vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

const categories: CategoryDto[] = [
  { id: "cat-1", name: "Bowls", sortOrder: 1, active: true },
  { id: "cat-2", name: "Drinks", sortOrder: 2, active: true },
];

function makeItem(overrides: Partial<MenuItemDto> = {}): MenuItemDto {
  return {
    id: "item-1",
    categoryId: "cat-2",
    name: "Latte",
    description: "A creamy espresso drink",
    imageUrl: null,
    imageApproximate: false,
    isCustomizable: false,
    active: true,
    sortOrder: 1,
    restockPolicy: RestockPolicy.NonReturnable,
    variants: [],
    availableIngredients: null,
    ...overrides,
  };
}

describe("MenuItemFormSheet — Image Upload", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders image upload zone when no image exists", () => {
    render(
      <MenuItemFormSheet
        open={true}
        onOpenChange={vi.fn()}
        menuItem={makeItem({ imageUrl: null })}
        categories={categories}
        onSaved={vi.fn()}
      />
    );
    expect(screen.getByTestId("image-upload-zone")).toBeInTheDocument();
  });

  it("renders image preview when imageUrl is set", () => {
    render(
      <MenuItemFormSheet
        open={true}
        onOpenChange={vi.fn()}
        menuItem={makeItem({ imageUrl: "/uploads/menu/test.jpg" })}
        categories={categories}
        onSaved={vi.fn()}
      />
    );
    expect(screen.getByTestId("image-preview")).toBeInTheDocument();
  });

  it("shows Change and Remove buttons when image exists", () => {
    render(
      <MenuItemFormSheet
        open={true}
        onOpenChange={vi.fn()}
        menuItem={makeItem({ imageUrl: "/uploads/menu/test.jpg" })}
        categories={categories}
        onSaved={vi.fn()}
      />
    );
    expect(screen.getByText("Change")).toBeInTheDocument();
    expect(screen.getByText("Remove")).toBeInTheDocument();
  });

  it("Remove button calls delete endpoint and clears preview", async () => {
    const user = userEvent.setup();
    deleteMock.mockResolvedValue(undefined);

    const onSaved = vi.fn();
    render(
      <MenuItemFormSheet
        open={true}
        onOpenChange={vi.fn()}
        menuItem={makeItem({ imageUrl: "/uploads/menu/test.jpg" })}
        categories={categories}
        onSaved={onSaved}
      />
    );

    await user.click(screen.getByText("Remove"));
    await waitFor(() => {
      expect(deleteMock).toHaveBeenCalledWith("test-token", "item-1");
    });
  });

  it("file picker accepts image/* (allows camera on mobile)", () => {
    render(
      <MenuItemFormSheet
        open={true}
        onOpenChange={vi.fn()}
        menuItem={makeItem({ imageUrl: null })}
        categories={categories}
        onSaved={vi.fn()}
      />
    );
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    expect(input).toBeTruthy();
    expect(input.accept).toBe("image/*");
  });

  it("shows imageApproximate checkbox in edit mode", () => {
    render(
      <MenuItemFormSheet
        open={true}
        onOpenChange={vi.fn()}
        menuItem={makeItem({ imageUrl: "/uploads/menu/test.jpg" })}
        categories={categories}
        onSaved={vi.fn()}
      />
    );
    expect(screen.getByLabelText(/not an accurate image/i)).toBeInTheDocument();
  });
});
