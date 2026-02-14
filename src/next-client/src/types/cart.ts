import type { MenuItemDto, MenuItemVariantDto, AvailableIngredientDto } from "./api";

export interface CartItem {
  id: string;
  menuItem: MenuItemDto;
  variant: MenuItemVariantDto | null;
  selectedIngredients: AvailableIngredientDto[] | null;
  quantity: number;
  notes: string | null;
}

export function getUnitPrice(item: CartItem): number {
  if (item.menuItem.isCustomizable) {
    return (
      item.selectedIngredients?.reduce((sum, i) => sum + i.customerPrice, 0) ??
      0
    );
  }
  return item.variant?.price ?? 0;
}

export function getLineTotal(item: CartItem): number {
  return getUnitPrice(item) * item.quantity;
}

export function getDisplayName(item: CartItem): string {
  if (item.menuItem.isCustomizable) {
    return item.menuItem.name;
  }
  return item.variant
    ? `${item.menuItem.name} (${item.variant.name})`
    : item.menuItem.name;
}
