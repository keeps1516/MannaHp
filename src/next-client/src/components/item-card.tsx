"use client";

import Image from "next/image";
import Link from "next/link";
import { ChevronRight } from "lucide-react";
import type { MenuItemDto } from "@/types/api";

interface ItemCardProps {
  item: MenuItemDto;
}

function getPriceLabel(item: MenuItemDto): string {
  const activeVariants = item.variants.filter((v) => v.active);
  if (activeVariants.length === 0) return "";
  if (activeVariants.length === 1)
    return `$${activeVariants[0].price.toFixed(2)}`;
  const min = Math.min(...activeVariants.map((v) => v.price));
  const max = Math.max(...activeVariants.map((v) => v.price));
  if (min === max) return `$${min.toFixed(2)}`;
  return `$${min.toFixed(2)} \u2013 $${max.toFixed(2)}`;
}

function getVariantSummary(item: MenuItemDto): string | null {
  const activeVariants = item.variants.filter((v) => v.active);
  if (activeVariants.length <= 1) return null;
  return activeVariants.map((v) => v.name).join(" \u00B7 ");
}

export function ItemCard({ item }: ItemCardProps) {
  const priceLabel = getPriceLabel(item);
  const variantSummary = getVariantSummary(item);

  return (
    <Link href={`/item/${item.id}`}>
      <div className="group relative rounded-xl border border-[#1e3a5f] bg-[#163a50] p-4 transition-all duration-200 hover:border-[#00e5ff]/50 hover:shadow-[0_0_20px_rgba(0,229,255,0.1)] cursor-pointer">
        <div className="flex items-center gap-3">
          {/* Thumbnail */}
          {item.imageUrl && (
            <div className="relative h-14 w-14 shrink-0 rounded-lg overflow-hidden bg-[#0f1f35]">
              <Image
                src={item.imageUrl}
                alt={item.name}
                fill
                className="object-cover"
                sizes="56px"
              />
            </div>
          )}
          <div className="flex-1 min-w-0">
            <h3 className="font-semibold text-white group-hover:text-[#00e5ff] transition-colors">
              {item.name}
            </h3>
            {item.description && (
              <p className="text-sm text-[#7a9bb5] mt-0.5 line-clamp-2">
                {item.description}
              </p>
            )}
            {variantSummary && (
              <p className="text-xs text-[#4a6a85] mt-1">{variantSummary}</p>
            )}
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <span className="font-semibold text-[#00e5ff]">{priceLabel}</span>
            <ChevronRight className="h-4 w-4 text-[#4a6a85] group-hover:text-[#00e5ff] transition-colors" />
          </div>
        </div>
      </div>
    </Link>
  );
}
