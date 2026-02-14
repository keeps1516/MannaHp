"use client";

import { useEffect, useState, useMemo } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, Loader2 } from "lucide-react";
import { BowlBuilder } from "@/components/bowl-builder";
import { ItemCard } from "@/components/item-card";
import { api } from "@/lib/api";
import type { CategoryDto, MenuItemDto } from "@/types/api";

export default function CategoryPage() {
  const params = useParams();
  const router = useRouter();
  const categoryId = params.id as string;

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
        setCategories(cats);
        setMenuItems(items.filter((i) => i.active));
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load menu");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, []);

  const category = useMemo(
    () => categories.find((c) => c.id === categoryId) ?? null,
    [categories, categoryId]
  );

  const categoryItems = useMemo(
    () =>
      menuItems
        .filter((item) => item.categoryId === categoryId)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItems, categoryId]
  );

  const isCustomizable = useMemo(
    () => categoryItems.some((item) => item.isCustomizable),
    [categoryItems]
  );

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

  if (!category) {
    return (
      <div className="text-center mt-12 space-y-4">
        <p className="text-[#7a9bb5]">Category not found.</p>
        <button
          onClick={() => router.push("/")}
          className="text-[#00e5ff] hover:underline text-sm"
        >
          Back to menu
        </button>
      </div>
    );
  }

  // For customizable categories (bowls), show the bowl builder directly
  if (isCustomizable) {
    const bowlItem = categoryItems.find((item) => item.isCustomizable);
    if (!bowlItem) {
      return (
        <p className="text-center text-[#7a9bb5] mt-12">
          No items available in this category.
        </p>
      );
    }

    return (
      <div className="space-y-4">
        <button
          onClick={() => router.push("/")}
          className="flex items-center gap-1.5 text-[#7a9bb5] hover:text-[#00e5ff] transition-colors text-sm"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to menu
        </button>
        <BowlBuilder menuItem={bowlItem} onItemAdded={() => {}} />
      </div>
    );
  }

  // For fixed categories (drinks, etc.), show item cards
  return (
    <div className="space-y-6">
      <button
        onClick={() => router.push("/")}
        className="flex items-center gap-1.5 text-[#7a9bb5] hover:text-[#00e5ff] transition-colors text-sm"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to menu
      </button>

      <div>
        <h2 className="text-2xl font-bold tracking-tight text-white">
          {category.name}
        </h2>
      </div>

      {categoryItems.length === 0 ? (
        <p className="text-[#7a9bb5]">No items available in this category.</p>
      ) : (
        <div className="grid grid-cols-1 gap-3">
          {categoryItems.map((item) => (
            <ItemCard key={item.id} item={item} />
          ))}
        </div>
      )}
    </div>
  );
}
