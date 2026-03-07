import type {
  CategoryDto,
  MenuItemDto,
  OrderDto,
  CreateOrderRequest,
  CreateOrderResponse,
  StoreTokenResponse,
  StoreTokenValidationResponse,
  GenerateStoreTokenRequest,
  TvMenuConfigResponse,
} from "@/types/api";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5082";

/**
 * Resolves a menu item image URL to a full URL.
 * Uploaded images (`/uploads/...`) are served by the API server and need the API base URL.
 * Other relative paths (e.g. `/menu/...`) are static assets in Next.js public/ dir
 * and must remain relative so the browser fetches them from the same origin.
 */
export function resolveImageUrl(imageUrl: string): string {
  if (imageUrl.startsWith("http")) return imageUrl;
  if (imageUrl.startsWith("/uploads/")) return `${API_BASE}${imageUrl}`;
  return imageUrl;
}

function getStoreToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem("storeToken");
}

export function captureStoreTokenFromUrl(): void {
  if (typeof window === "undefined") return;
  const params = new URLSearchParams(window.location.search);
  const token = params.get("token");
  if (token) {
    localStorage.setItem("storeToken", token);
    // Clean the URL without reloading
    const url = new URL(window.location.href);
    url.searchParams.delete("token");
    window.history.replaceState({}, "", url.toString());
  }
}

async function fetchApi<T>(path: string, options?: RequestInit): Promise<T> {
  const storeToken = getStoreToken();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options?.headers as Record<string, string>),
  };
  if (storeToken) {
    headers["X-Store-Token"] = storeToken;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });
  if (!res.ok) {
    const errorBody = await res.text();
    throw new Error(`API error ${res.status}: ${errorBody}`);
  }
  return res.json();
}

export const api = {
  getPublicSettings: () => fetchApi<{ taxRate: number }>("/api/settings/public"),
  getCategories: () => fetchApi<CategoryDto[]>("/api/categories"),
  getMenuItems: () => fetchApi<MenuItemDto[]>("/api/menu-items"),
  getMenuItem: (id: string) => fetchApi<MenuItemDto>(`/api/menu-items/${id}`),
  getOrder: (id: string) => fetchApi<OrderDto>(`/api/orders/${id}`),
  createOrder: (req: CreateOrderRequest) =>
    fetchApi<CreateOrderResponse>("/api/orders", {
      method: "POST",
      body: JSON.stringify(req),
    }),
  confirmPayment: (orderId: string) =>
    fetchApi<OrderDto>(`/api/orders/${orderId}/confirm-payment`, {
      method: "POST",
    }),

  // Store tokens
  generateStoreToken: (req: GenerateStoreTokenRequest) =>
    fetchApi<StoreTokenResponse>("/api/store-tokens", {
      method: "POST",
      body: JSON.stringify(req),
    }),
  getCurrentStoreToken: () =>
    fetchApi<StoreTokenResponse>("/api/store-tokens/current"),
  revokeStoreToken: (id: string) =>
    fetchApi<void>(`/api/store-tokens/${id}`, { method: "DELETE" }),
  validateStoreToken: (token: string) =>
    fetchApi<StoreTokenValidationResponse>(
      `/api/store-tokens/${token}/validate`
    ),

  // TV Menu Config
  getTvMenuConfig: () =>
    fetchApi<TvMenuConfigResponse>("/api/settings/tv-menu-config"),
};
