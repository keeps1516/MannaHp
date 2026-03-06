import type {
  CategoryDto,
  MenuItemDto,
  OrderDto,
  CreateOrderRequest,
  CreateOrderResponse,
} from "@/types/api";

const API_BASE =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5082";

async function fetchApi<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { "Content-Type": "application/json", ...options?.headers },
    ...options,
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
};
