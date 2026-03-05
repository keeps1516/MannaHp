"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { FixedItemDetail } from "@/components/fixed-item-detail";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api";
import type { MenuItemDto } from "@/types/api";

export default function ItemPage() {
  const params = useParams();
  const itemId = params.id as string;

  const [menuItem, setMenuItem] = useState<MenuItemDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      try {
        const item = await api.getMenuItem(itemId);
        setMenuItem(item);
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load item");
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [itemId]);

  if (loading) {
    return (
      <div className="space-y-4 max-w-lg mx-auto">
        <Skeleton className="h-48 w-full rounded-xl" />
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-5 w-64" />
        <div className="space-y-2 mt-4">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-14 rounded-lg" />
          ))}
        </div>
      </div>
    );
  }

  if (error || !menuItem) {
    return (
      <div className="rounded-lg border border-[#ff4757] bg-[#ff4757]/10 p-4 text-[#ff4757] text-sm">
        {error ?? "Item not found."}
      </div>
    );
  }

  return <FixedItemDetail menuItem={menuItem} />;
}
