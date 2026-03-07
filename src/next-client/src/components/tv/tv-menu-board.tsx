"use client";

import { TvCategorySection } from "./tv-category-section";
import { TvQrFooter } from "./tv-qr-footer";
import type {
  CategoryDto,
  MenuItemDto,
  TvMenuConfigResponse,
} from "@/types/api";

interface TvMenuBoardProps {
  categories: CategoryDto[];
  menuItems: MenuItemDto[];
  config: TvMenuConfigResponse;
  storeTokenUrl: string | null;
}

export function TvMenuBoard({
  categories,
  menuItems,
  config,
  storeTokenUrl,
}: TvMenuBoardProps) {
  // Filter categories based on config
  const visibleCategories = config.visibleCategoryIds.length > 0
    ? categories.filter((c) => config.visibleCategoryIds.includes(c.id) && c.active)
    : categories.filter((c) => c.active);

  // Sort by sortOrder
  const sortedCategories = [...visibleCategories].sort(
    (a, b) => a.sortOrder - b.sortOrder
  );

  // Filter items: active, not hidden, not out of stock (has at least one active variant for fixed items)
  const visibleItems = menuItems.filter(
    (item) =>
      item.active && !config.hiddenItemIds.includes(item.id)
  );

  // Group items by category
  const itemsByCategory = new Map<string, MenuItemDto[]>();
  for (const item of visibleItems) {
    const list = itemsByCategory.get(item.categoryId) ?? [];
    list.push(item);
    itemsByCategory.set(item.categoryId, list);
  }

  // Sort items within each category
  for (const [, items] of itemsByCategory) {
    items.sort((a, b) => a.sortOrder - b.sortOrder);
  }

  return (
    <div className="min-h-screen bg-[#0f1f35] text-white">
      {/* Header */}
      <div className="text-center py-8 border-b border-[#1e3a5f]">
        <h1 className="text-5xl font-bold tracking-wider">
          MANNA <span className="text-[#00e5ff]">+</span> HP
        </h1>
      </div>

      {/* Menu Content */}
      <div className="px-10 py-8 space-y-10 pr-[220px]">
        {sortedCategories.map((category) => {
          const items = itemsByCategory.get(category.id) ?? [];
          return (
            <TvCategorySection
              key={category.id}
              category={category}
              items={items}
              sampleBowls={config.sampleBowls}
              showIngredients={config.showAllIngredients}
            />
          );
        })}
      </div>

      {/* QR Codes */}
      <TvQrFooter
        storeTokenUrl={storeTokenUrl}
        orderOnlineUrl={config.orderOnlineUrl}
      />
    </div>
  );
}
