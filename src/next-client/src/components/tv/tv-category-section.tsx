"use client";

import { getCategoryMeta } from "@/lib/category-meta";
import { TvItemCard } from "./tv-item-card";
import type { CategoryDto, MenuItemDto, ResolvedSampleBowl } from "@/types/api";

interface TvCategorySectionProps {
  category: CategoryDto;
  items: MenuItemDto[];
  sampleBowls: Record<string, ResolvedSampleBowl>;
  showIngredients: boolean;
}

export function TvCategorySection({
  category,
  items,
  sampleBowls,
  showIngredients,
}: TvCategorySectionProps) {
  const meta = getCategoryMeta(category.name);

  if (items.length === 0) return null;

  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-bold text-[#00e5ff] tracking-wide uppercase">
        {meta.emoji} {category.name}
      </h2>
      <div className="flex gap-5 flex-wrap">
        {items.map((item) => (
          <TvItemCard
            key={item.id}
            item={item}
            sampleBowl={sampleBowls[item.id]}
            showIngredients={showIngredients}
          />
        ))}
      </div>
    </div>
  );
}
