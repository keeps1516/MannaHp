export enum OrderStatus {
  Received = 0,
  Preparing = 1,
  Ready = 2,
  Completed = 3,
  Cancelled = 4,
}

export enum PaymentMethod {
  Card = 0,
  InStore = 1,
}

export enum PaymentStatus {
  Pending = 0,
  Paid = 1,
  Failed = 2,
  Refunded = 3,
}

export interface CategoryDto {
  id: string;
  name: string;
  sortOrder: number;
  active: boolean;
}

export interface MenuItemDto {
  id: string;
  categoryId: string;
  name: string;
  description: string | null;
  imageUrl: string | null;
  isCustomizable: boolean;
  active: boolean;
  sortOrder: number;
  variants: MenuItemVariantDto[];
  availableIngredients: AvailableIngredientDto[] | null;
}

export interface MenuItemVariantDto {
  id: string;
  name: string;
  price: number;
  sortOrder: number;
  active: boolean;
}

export enum UnitOfMeasure {
  Oz = 0,
  Lb = 1,
  Cups = 2,
  FlOz = 3,
  Tsp = 4,
  Tbsp = 5,
  Each = 6,
  Shot = 7,
}

export interface AvailableIngredientDto {
  id: string;
  ingredientId: string;
  ingredientName: string;
  customerPrice: number;
  quantityUsed: number;
  isDefault: boolean;
  groupName: string;
  sortOrder: number;
  active: boolean;
  ingredientUnit: UnitOfMeasure;
}

export interface CreateOrderRequest {
  paymentMethod: PaymentMethod;
  notes: string | null;
  items: CreateOrderItemRequest[];
}

export interface CreateOrderItemRequest {
  menuItemId: string;
  variantId: string | null;
  quantity: number;
  notes: string | null;
  selectedIngredientIds: string[] | null;
}

export interface OrderDto {
  id: string;
  status: OrderStatus;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  subtotal: number;
  taxRate: number;
  tax: number;
  total: number;
  notes: string | null;
  createdAt: string;
  items: OrderItemDto[];
}

export interface OrderItemDto {
  id: string;
  menuItemName: string;
  variantName: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  notes: string | null;
  ingredients: OrderItemIngredientDto[] | null;
}

export interface OrderItemIngredientDto {
  ingredientId: string;
  ingredientName: string;
  priceCharged: number;
}
