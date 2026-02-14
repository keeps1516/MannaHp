"use client";

import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { getCategoryMeta } from "@/lib/category-meta";
import type { CategoryDto } from "@/types/api";

interface CategoryCardProps {
  category: CategoryDto;
  itemCount: number;
}

export function CategoryCard({ category, itemCount }: CategoryCardProps) {
  const meta = getCategoryMeta(category.name);

  return (
    <Link href={`/category/${category.id}`}>
      <div className="group relative rounded-xl border border-[#1e3a5f] bg-[#163a50] p-5 transition-all duration-200 hover:border-[#00e5ff]/50 hover:shadow-[0_0_20px_rgba(0,229,255,0.1)] cursor-pointer">
        <div className="flex items-center gap-4">
          <span className="text-4xl" role="img">
            {meta.emoji}
          </span>
          <div className="flex-1 min-w-0">
            <h3 className="text-lg font-bold text-white group-hover:text-[#00e5ff] transition-colors">
              {category.name}
            </h3>
            <p className="text-sm text-[#7a9bb5] mt-0.5">{meta.description}</p>
            <p className="text-xs text-[#4a6a85] mt-1">
              {itemCount} {itemCount === 1 ? "item" : "items"}
            </p>
          </div>
          <ChevronRight className="h-5 w-5 text-[#4a6a85] group-hover:text-[#00e5ff] transition-colors shrink-0" />
        </div>
      </div>
    </Link>
  );
}
