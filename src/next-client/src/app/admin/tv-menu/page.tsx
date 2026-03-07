"use client";

import { useEffect, useState, useCallback } from "react";
import { api } from "@/lib/api";
import { TvMenuBoard } from "@/components/tv/tv-menu-board";
import type {
  CategoryDto,
  MenuItemDto,
  TvMenuConfigResponse,
  StoreTokenResponse,
} from "@/types/api";

const PUBLIC_BASE_URL =
  process.env.NEXT_PUBLIC_BASE_URL ?? "http://localhost:3000";

const REFRESH_INTERVAL = 5 * 60 * 1000; // 5 minutes

export default function TvMenuPage() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [config, setConfig] = useState<TvMenuConfigResponse | null>(null);
  const [storeToken, setStoreToken] = useState<StoreTokenResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchData = useCallback(async () => {
    try {
      const [cats, items, tvConfig] = await Promise.all([
        api.getCategories(),
        api.getMenuItems(),
        api.getTvMenuConfig(),
      ]);
      setCategories(cats);
      setMenuItems(items);
      setConfig(tvConfig);

      // Fetch store token (may fail if none active — that's okay)
      try {
        const token = await api.getCurrentStoreToken();
        setStoreToken(token);
      } catch {
        setStoreToken(null);
      }
    } catch (err) {
      console.error("Failed to load TV menu data:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, REFRESH_INTERVAL);
    return () => clearInterval(interval);
  }, [fetchData]);

  // Hide cursor after 3 seconds of inactivity
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;
    const hide = () => {
      document.body.style.cursor = "none";
    };
    const show = () => {
      document.body.style.cursor = "";
      clearTimeout(timer);
      timer = setTimeout(hide, 3000);
    };
    document.addEventListener("mousemove", show);
    timer = setTimeout(hide, 3000);
    return () => {
      document.removeEventListener("mousemove", show);
      clearTimeout(timer);
      document.body.style.cursor = "";
    };
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen bg-[#0f1f35] flex items-center justify-center">
        <div className="text-center space-y-4">
          <h1 className="text-4xl font-bold text-white tracking-wider">
            MANNA <span className="text-[#00e5ff]">+</span> HP
          </h1>
          <p className="text-[#7a9bb5]">Loading menu...</p>
        </div>
      </div>
    );
  }

  if (!config) {
    return (
      <div className="min-h-screen bg-[#0f1f35] flex items-center justify-center">
        <p className="text-[#7a9bb5]">Unable to load menu configuration.</p>
      </div>
    );
  }

  const storeTokenUrl = storeToken
    ? `${PUBLIC_BASE_URL}?token=${storeToken.token}`
    : null;

  return (
    <TvMenuBoard
      categories={categories}
      menuItems={menuItems}
      config={config}
      storeTokenUrl={storeTokenUrl}
    />
  );
}
