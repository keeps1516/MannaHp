"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft, Loader2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { unitShortLabel } from "@/lib/unit-options";
import { InventoryChangeType } from "@/types/api";
import type { InventoryLogDto, IngredientDto } from "@/types/api";

const changeTypeLabels: Record<InventoryChangeType, string> = {
  [InventoryChangeType.Received]: "Received",
  [InventoryChangeType.OrderDecrement]: "Order",
  [InventoryChangeType.Adjustment]: "Adjustment",
};

const changeTypeBadgeStyles: Record<InventoryChangeType, string> = {
  [InventoryChangeType.Received]: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
  [InventoryChangeType.OrderDecrement]: "bg-blue-500/10 text-blue-400 border-blue-500/20",
  [InventoryChangeType.Adjustment]: "bg-violet-500/10 text-violet-400 border-violet-500/20",
};

export default function IngredientHistoryPage() {
  const { token } = useAuth();
  const params = useParams();
  const ingredientId = params.id as string;

  const [ingredient, setIngredient] = useState<IngredientDto | null>(null);
  const [logs, setLogs] = useState<InventoryLogDto[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchData = useCallback(async () => {
    if (!token || !ingredientId) return;
    try {
      const [ing, history] = await Promise.all([
        adminApi.getIngredient(token, ingredientId),
        adminApi.getInventoryHistory(token, ingredientId),
      ]);
      setIngredient(ing);
      setLogs(history);
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, [token, ingredientId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-[#00e5ff]" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <Link
          href="/admin/ingredients"
          className="flex items-center gap-1 text-[#7a9bb5] hover:text-[#00e5ff] text-sm mb-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Ingredients
        </Link>
        <h1 className="text-2xl font-bold text-white">
          {ingredient?.name ?? "Ingredient"} — History
        </h1>
        {ingredient && (
          <p className="text-[#7a9bb5] mt-1">
            Current stock: {ingredient.stockQuantity} {unitShortLabel(ingredient.unit)}
          </p>
        )}
      </div>

      {/* Log entries */}
      {logs.length === 0 ? (
        <p className="text-center text-[#7a9bb5] py-12">No history yet.</p>
      ) : (
        <div className="space-y-2">
          {logs.map((log) => {
            const isPositive = log.quantityChange > 0;
            return (
              <div
                key={log.id}
                className="rounded-lg border border-white/10 bg-[#0d1f3c] p-4 flex items-center gap-4"
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <Badge
                      className={`${changeTypeBadgeStyles[log.changeType]} hover:bg-transparent text-xs`}
                    >
                      {changeTypeLabels[log.changeType]}
                    </Badge>
                    <span className="text-xs text-[#4a6a85]">
                      {new Date(log.createdAt).toLocaleString()}
                    </span>
                  </div>
                  {log.notes && (
                    <p className="text-sm text-[#7a9bb5] truncate">{log.notes}</p>
                  )}
                </div>
                <div className="text-right shrink-0">
                  <p
                    className={`font-bold text-lg ${
                      isPositive ? "text-green-400" : "text-red-400"
                    }`}
                  >
                    {isPositive ? "+" : ""}{log.quantityChange}
                  </p>
                  <p className="text-xs text-[#4a6a85]">
                    → {log.newStockQuantity}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
