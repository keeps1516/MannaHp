import type {
  AuthResponse,
  CategoryDto,
  CreateAvailableIngredientRequest,
  CreateCategoryRequest,
  CreateIngredientRequest,
  CreateMenuItemRequest,
  CreateVariantRequest,
  IngredientDto,
  LoginRequest,
  MenuItemDto,
  MenuItemVariantDto,
  AvailableIngredientDto,
  OrderDto,
  OrderStatus,
  RegisterRequest,
  UpdateAvailableIngredientRequest,
  UpdateCategoryRequest,
  UpdateIngredientRequest,
  UpdateMenuItemRequest,
  UpdateVariantRequest,
  UserDto,
} from "@/types/api";

export const API_BASE =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5082";

async function adminFetch<T>(
  path: string,
  token: string | null,
  options?: RequestInit
): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options?.headers as Record<string, string>),
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (res.status === 401) {
    if (typeof window !== "undefined") {
      localStorage.removeItem("admin_token");
      window.location.href = "/admin/login";
    }
    throw new Error("Unauthorized");
  }

  if (!res.ok) {
    const errorBody = await res.text();
    throw new Error(`API error ${res.status}: ${errorBody}`);
  }

  return res.json();
}

async function adminFetchNoBody(
  path: string,
  token: string | null,
  options?: RequestInit
): Promise<void> {
  const headers: Record<string, string> = {
    ...(options?.headers as Record<string, string>),
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (res.status === 401) {
    if (typeof window !== "undefined") {
      localStorage.removeItem("admin_token");
      window.location.href = "/admin/login";
    }
    throw new Error("Unauthorized");
  }

  if (!res.ok) {
    const errorBody = await res.text();
    throw new Error(`API error ${res.status}: ${errorBody}`);
  }
}

export const adminApi = {
  // ── Auth ──
  login: async (req: LoginRequest): Promise<AuthResponse> => {
    const res = await fetch(`${API_BASE}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
    });

    if (res.status === 401) {
      throw new Error("Invalid email or password");
    }

    if (!res.ok) {
      const errorBody = await res.text();
      throw new Error(`API error ${res.status}: ${errorBody}`);
    }

    return res.json();
  },

  me: (token: string) => adminFetch<UserDto>("/api/auth/me", token),

  register: (token: string, req: RegisterRequest) =>
    adminFetch<AuthResponse>("/api/auth/register", token, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  // ── Settings ──
  getSettings: (token: string) =>
    adminFetch<{ key: string; value: string }[]>("/api/settings", token),

  updateSettings: (token: string, settings: { key: string; value: string }[]) =>
    adminFetchNoBody("/api/settings", token, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(settings),
    }),

  // ── Orders ──
  getActiveOrders: (token: string) =>
    adminFetch<OrderDto[]>("/api/orders/active", token),

  getTodayRevenue: (token: string) =>
    adminFetch<{ total: number }>("/api/orders/today-revenue", token),

  updateOrderStatus: (token: string, id: string, status: OrderStatus) =>
    adminFetch<{ id: string; status: OrderStatus }>(
      `/api/orders/${id}/status`,
      token,
      { method: "PATCH", body: JSON.stringify({ status }) }
    ),

  // ── Categories ──
  getCategories: (token: string) =>
    adminFetch<CategoryDto[]>("/api/categories", token),

  createCategory: (token: string, req: CreateCategoryRequest) =>
    adminFetch<CategoryDto>("/api/categories", token, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  updateCategory: (token: string, id: string, req: UpdateCategoryRequest) =>
    adminFetch<CategoryDto>(`/api/categories/${id}`, token, {
      method: "PUT",
      body: JSON.stringify(req),
    }),

  deleteCategory: (token: string, id: string) =>
    adminFetchNoBody(`/api/categories/${id}`, token, { method: "DELETE" }),

  // ── Ingredients ──
  getIngredients: (token: string) =>
    adminFetch<IngredientDto[]>("/api/ingredients", token),

  createIngredient: (token: string, req: CreateIngredientRequest) =>
    adminFetch<IngredientDto>("/api/ingredients", token, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  updateIngredient: (token: string, id: string, req: UpdateIngredientRequest) =>
    adminFetch<IngredientDto>(`/api/ingredients/${id}`, token, {
      method: "PUT",
      body: JSON.stringify(req),
    }),

  deleteIngredient: (token: string, id: string) =>
    adminFetchNoBody(`/api/ingredients/${id}`, token, { method: "DELETE" }),

  // ── Menu Items ──
  getMenuItems: (token: string) =>
    adminFetch<MenuItemDto[]>("/api/menu-items", token),

  getMenuItem: (token: string, id: string) =>
    adminFetch<MenuItemDto>(`/api/menu-items/${id}`, token),

  createMenuItem: (token: string, req: CreateMenuItemRequest) =>
    adminFetch<MenuItemDto>("/api/menu-items", token, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  updateMenuItem: (token: string, id: string, req: UpdateMenuItemRequest) =>
    adminFetch<MenuItemDto>(`/api/menu-items/${id}`, token, {
      method: "PUT",
      body: JSON.stringify(req),
    }),

  deleteMenuItem: (token: string, id: string) =>
    adminFetchNoBody(`/api/menu-items/${id}`, token, { method: "DELETE" }),

  // ── Variants ──
  createVariant: (token: string, menuItemId: string, req: CreateVariantRequest) =>
    adminFetch<MenuItemVariantDto>(
      `/api/menu-items/${menuItemId}/variants`,
      token,
      { method: "POST", body: JSON.stringify(req) }
    ),

  updateVariant: (
    token: string,
    menuItemId: string,
    variantId: string,
    req: UpdateVariantRequest
  ) =>
    adminFetch<MenuItemVariantDto>(
      `/api/menu-items/${menuItemId}/variants/${variantId}`,
      token,
      { method: "PUT", body: JSON.stringify(req) }
    ),

  deleteVariant: (token: string, menuItemId: string, variantId: string) =>
    adminFetchNoBody(
      `/api/menu-items/${menuItemId}/variants/${variantId}`,
      token,
      { method: "DELETE" }
    ),

  // ── Available Ingredients ──
  createAvailableIngredient: (
    token: string,
    menuItemId: string,
    req: CreateAvailableIngredientRequest
  ) =>
    adminFetch<AvailableIngredientDto>(
      `/api/menu-items/${menuItemId}/available-ingredients`,
      token,
      { method: "POST", body: JSON.stringify(req) }
    ),

  updateAvailableIngredient: (
    token: string,
    menuItemId: string,
    availId: string,
    req: UpdateAvailableIngredientRequest
  ) =>
    adminFetch<AvailableIngredientDto>(
      `/api/menu-items/${menuItemId}/available-ingredients/${availId}`,
      token,
      { method: "PUT", body: JSON.stringify(req) }
    ),

  deleteAvailableIngredient: (
    token: string,
    menuItemId: string,
    availId: string
  ) =>
    adminFetchNoBody(
      `/api/menu-items/${menuItemId}/available-ingredients/${availId}`,
      token,
      { method: "DELETE" }
    ),
};
