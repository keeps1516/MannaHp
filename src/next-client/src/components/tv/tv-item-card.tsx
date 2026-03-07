"use client";

import { resolveImageUrl } from "@/lib/api";
import type { MenuItemDto, ResolvedSampleBowl } from "@/types/api";

interface TvItemCardProps {
  item: MenuItemDto;
  sampleBowl?: ResolvedSampleBowl;
  showIngredients?: boolean;
}

export function TvItemCard({ item, sampleBowl, showIngredients }: TvItemCardProps) {
  const activeVariants = item.variants.filter((v) => v.active);

  return (
    <div className="flex-shrink-0 w-[320px] rounded-2xl border border-[#1e3a5f] bg-[#163a50] overflow-hidden">
      {/* Image */}
      {item.imageUrl ? (
        <div className="relative w-full h-[220px] bg-[#0f1f35]">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={resolveImageUrl(item.imageUrl)}
            alt={item.name}
            className="w-full h-full object-cover"
          />
        </div>
      ) : (
        <div className="w-full h-[220px] bg-gradient-to-br from-[#1e3a5f] to-[#0f1f35] flex items-center justify-center">
          <span className="text-6xl font-bold text-[#00e5ff]/30">
            {item.name.charAt(0)}
          </span>
        </div>
      )}

      {/* Content */}
      <div className="p-5 space-y-3">
        <h3 className="text-xl font-bold text-white leading-tight">{item.name}</h3>

        {item.description && (
          <p className="text-sm text-[#7a9bb5] line-clamp-2">{item.description}</p>
        )}

        {/* Fixed item variants + prices */}
        {!item.isCustomizable && activeVariants.length > 0 && (
          <div className="space-y-1">
            {activeVariants.map((v) => (
              <div key={v.id} className="flex justify-between items-center">
                <span className="text-sm text-[#a0c4d8]">{v.name}</span>
                <span className="text-lg font-semibold text-[#00e5ff]">
                  ${v.price.toFixed(2)}
                </span>
              </div>
            ))}
          </div>
        )}

        {/* Customizable item — sample bowl pricing */}
        {item.isCustomizable && sampleBowl && (
          <div className="space-y-2">
            <div className="flex justify-between items-center">
              <span className="text-sm text-[#a0c4d8]">{sampleBowl.label}</span>
              <span className="text-lg font-semibold text-[#00e5ff]">
                ${sampleBowl.calculatedPrice.toFixed(2)}
              </span>
            </div>
            <p className="text-xs text-[#7a9bb5]">
              {sampleBowl.ingredientNames.join(", ")}
            </p>
            <p className="text-sm font-medium text-[#00e5ff]/70 italic">
              Build Your Own!
            </p>
          </div>
        )}

        {/* Customizable item without sample bowl */}
        {item.isCustomizable && !sampleBowl && (
          <p className="text-sm font-medium text-[#00e5ff]/70 italic">
            Build Your Own!
          </p>
        )}

        {/* Available ingredients list */}
        {showIngredients && item.isCustomizable && item.availableIngredients && (
          <div className="border-t border-[#1e3a5f] pt-2">
            <p className="text-xs text-[#7a9bb5] mb-1">Available:</p>
            <p className="text-xs text-[#4a6a85] leading-relaxed">
              {item.availableIngredients
                .filter((ai) => ai.active)
                .map((ai) => ai.ingredientName)
                .join(" \u00B7 ")}
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
