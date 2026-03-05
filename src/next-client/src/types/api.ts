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
  imageApproximate: boolean;
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
  orderNumber: number;
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
  quantityUsed: number;
  ingredientUnit: UnitOfMeasure;
  priceCharged: number;
}

// ── Ingredients ──

export interface IngredientDto {
  id: string;
  name: string;
  unit: UnitOfMeasure;
  costPerUnit: number;
  stockQuantity: number;
  lowStockThreshold: number;
  active: boolean;
}

export interface CreateIngredientRequest {
  name: string;
  unit: UnitOfMeasure;
  costPerUnit: number;
  stockQuantity: number;
  lowStockThreshold: number;
}

export interface UpdateIngredientRequest {
  name: string;
  unit: UnitOfMeasure;
  costPerUnit: number;
  stockQuantity: number;
  lowStockThreshold: number;
  active: boolean;
}

// ── Category Mutations ──

export interface CreateCategoryRequest {
  name: string;
  sortOrder: number;
}

export interface UpdateCategoryRequest {
  name: string;
  sortOrder: number;
  active: boolean;
}

// ── Menu Item Mutations ──

export interface CreateMenuItemRequest {
  categoryId: string;
  name: string;
  description: string | null;
  imageUrl: string | null;
  imageApproximate: boolean;
  isCustomizable: boolean;
  sortOrder: number;
}

export interface UpdateMenuItemRequest {
  name: string;
  description: string | null;
  imageUrl: string | null;
  imageApproximate: boolean;
  isCustomizable: boolean;
  categoryId: string;
  sortOrder: number;
  active: boolean;
}

// ── Variant Mutations ──

export interface CreateVariantRequest {
  name: string;
  price: number;
  sortOrder: number;
}

export interface UpdateVariantRequest {
  name: string;
  price: number;
  sortOrder: number;
  active: boolean;
}

// ── Available Ingredient Mutations ──

export interface CreateAvailableIngredientRequest {
  ingredientId: string;
  customerPrice: number;
  quantityUsed: number;
  isDefault: boolean;
  groupName: string;
  sortOrder: number;
}

export interface UpdateAvailableIngredientRequest {
  customerPrice: number;
  quantityUsed: number;
  isDefault: boolean;
  groupName: string;
  sortOrder: number;
  active: boolean;
}

// ── Recipe Ingredient Types ──

export interface RecipeIngredientDto {
  id: string;
  ingredientId: string;
  ingredientName: string;
  quantity: number;
}

export interface CreateRecipeIngredientRequest {
  ingredientId: string;
  quantity: number;
}

export interface UpdateRecipeIngredientRequest {
  quantity: number;
}

// Response for order creation — includes Stripe client secret for card payments
export interface CreateOrderResponse {
  order: OrderDto;
  clientSecret: string | null;
  stripePublishableKey: string | null;
}

// ── Order Mutations ──

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
}

// ── Auth Mutations ──

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string | null;
}

// ── Auth ──

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  displayName: string;
  role: string;
  expiresAt: string;
}

export interface UserDto {
  id: string;
  email: string;
  displayName: string | null;
  role: string;
}
