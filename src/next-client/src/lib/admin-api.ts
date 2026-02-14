import type { AuthResponse, LoginRequest, UserDto } from "@/types/api";

const API_BASE =
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
    // Token expired or invalid — clear and redirect
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

export const adminApi = {
  login: (req: LoginRequest) =>
    adminFetch<AuthResponse>("/api/auth/login", null, {
      method: "POST",
      body: JSON.stringify(req),
    }),

  me: (token: string) => adminFetch<UserDto>("/api/auth/me", token),
};
