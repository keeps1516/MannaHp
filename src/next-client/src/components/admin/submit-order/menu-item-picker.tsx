"use client";

import { useMemo } from "react";
import type { MenuItemDto, CategoryDto } from "@/types/api";

interface MenuItemPickerProps {
  categories: CategoryDto[];
  menuItems: MenuItemDto[];
  activeCategory: string | null;
  onCategoryChange: (categoryId: string | null) => void;
  onSelectItem: (item: MenuItemDto) => void;
}

function getPriceLabel(item: MenuItemDto): string {
  if (item.isCustomizable) return "Build your own";

  const prices = item.variants
    .filter((v) => v.active)
    .map((v) => v.price)
    .sort((a, b) => a - b);

  if (prices.length === 0) return "";
  if (prices.length === 1 || prices[0] === prices[prices.length - 1]) {
    return `$${prices[0].toFixed(2)}`;
  }
  return `$${prices[0].toFixed(2)} \u2013 $${prices[prices.length - 1].toFixed(2)}`;
}

export function MenuItemPicker({
  categories,
  menuItems,
  activeCategory,
  onCategoryChange,
  onSelectItem,
}: MenuItemPickerProps) {
  const activeCategories = useMemo(
    () =>
      categories
        .filter((c) => c.active)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [categories]
  );

  const filtered = useMemo(
    () =>
      menuItems
        .filter((i) => i.active)
        .filter((i) => !activeCategory || i.categoryId === activeCategory)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItems, activeCategory]
  );

  return (
    <div className="space-y-4">
      {/* Category filter */}
      <div className="flex gap-2 overflow-x-auto pb-1 scrollbar-none">
        <button
          type="button"
          onClick={() => onCategoryChange(null)}
          className={`shrink-0 rounded-lg px-4 py-2 text-sm font-medium transition-colors cursor-pointer ${
            activeCategory === null
              ? "bg-[#00e5ff] text-[#0f1f35]"
              : "bg-white/5 text-[#7a9bb5] hover:bg-white/10 hover:text-white"
          }`}
        >
          All
        </button>
        {activeCategories.map((cat) => (
          <button
            key={cat.id}
            type="button"
            onClick={() => onCategoryChange(cat.id)}
            className={`shrink-0 rounded-lg px-4 py-2 text-sm font-medium transition-colors cursor-pointer ${
              activeCategory === cat.id
                ? "bg-[#00e5ff] text-[#0f1f35]"
                : "bg-white/5 text-[#7a9bb5] hover:bg-white/10 hover:text-white"
            }`}
          >
            {cat.name}
          </button>
        ))}
      </div>

      {/* Items grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-3">
        {filtered.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => onSelectItem(item)}
            className="text-left rounded-lg border border-white/10 bg-[#0d1f3c] p-4 cursor-pointer hover:border-[#00e5ff]/30 hover:bg-[#112240] transition-all"
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-semibold text-white truncate">
                  {item.name}
                </p>
                {item.description && (
                  <p className="text-sm text-[#7a9bb5] line-clamp-1 mt-0.5">
                    {item.description}
                  </p>
                )}
              </div>
              <span className="shrink-0 text-sm font-semibold text-[#00e5ff]">
                {getPriceLabel(item)}
              </span>
            </div>
          </button>
        ))}

        {filtered.length === 0 && (
          <p className="col-span-full text-center text-sm text-[#4a6a85] py-12">
            No items in this category
          </p>
        )}
      </div>
    </div>
  );
}
