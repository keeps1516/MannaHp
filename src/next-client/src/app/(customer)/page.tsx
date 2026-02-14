"use client";

import { useEffect, useState, useMemo } from "react";
import { Loader2 } from "lucide-react";
import { CategoryCard } from "@/components/category-card";
import { api } from "@/lib/api";
import type { CategoryDto, MenuItemDto } from "@/types/api";

export default function HomePage() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      try {
        const [cats, items] = await Promise.all([
          api.getCategories(),
          api.getMenuItems(),
        ]);
        setCategories(
          cats.filter((c) => c.active).sort((a, b) => a.sortOrder - b.sortOrder)
        );
        setMenuItems(items.filter((i) => i.active));
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load menu");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  // Only show categories that have active menu items
  const categoriesWithItems = useMemo(() => {
    return categories.filter((cat) =>
      menuItems.some((item) => item.categoryId === cat.id)
    );
  }, [categories, menuItems]);

  // Count items per category
  const itemCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const cat of categories) {
      counts[cat.id] = menuItems.filter(
        (item) => item.categoryId === cat.id
      ).length;
    }
    return counts;
  }, [categories, menuItems]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-[#00e5ff]" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-[#ff4757] bg-[#ff4757]/10 p-4 text-[#ff4757] text-sm">
        {error}
      </div>
    );
  }

  if (categoriesWithItems.length === 0) {
    return (
      <p className="text-center text-[#7a9bb5] mt-12">
        No menu items available.
      </p>
    );
  }

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-bold tracking-tight text-white">
          Our Menu
        </h2>
        <p className="text-[#7a9bb5]">Fuel Your HP. Find Your Manna.</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {categoriesWithItems.map((cat) => (
          <CategoryCard
            key={cat.id}
            category={cat}
            itemCount={itemCounts[cat.id] ?? 0}
          />
        ))}
      </div>
    </div>
  );
}
