"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import { ArrowLeft, Loader2, PackagePlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { unitShortLabel } from "@/lib/unit-options";
import type { IngredientDto } from "@/types/api";

export default function RestockPage() {
  const { token } = useAuth();
  const [ingredients, setIngredients] = useState<IngredientDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [quantities, setQuantities] = useState<Record<string, string>>({});
  const [notes, setNotes] = useState("");

  const fetchIngredients = useCallback(async () => {
    if (!token) return;
    try {
      const data = await adminApi.getIngredients(token);
      setIngredients(data.filter((i) => i.active));
    } catch {
      toast.error("Failed to load ingredients");
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchIngredients();
  }, [fetchIngredients]);

  function updateQuantity(id: string, value: string) {
    setQuantities((prev) => ({ ...prev, [id]: value }));
  }

  async function handleSubmit() {
    if (!token) return;

    const items = ingredients
      .filter((ing) => {
        const val = quantities[ing.id];
        return val && Number(val) > 0;
      })
      .map((ing) => ({
        ingredientId: ing.id,
        quantity: Number(quantities[ing.id]),
        notes: notes.trim() || undefined,
      }));

    if (items.length === 0) {
      toast.error("Enter quantities for at least one ingredient");
      return;
    }

    setSubmitting(true);
    try {
      await adminApi.bulkRestock(token, { items });
      toast.success(`Restocked ${items.length} ingredient${items.length !== 1 ? "s" : ""}`);
      setQuantities({});
      setNotes("");
      fetchIngredients();
    } catch {
      toast.error("Failed to submit delivery");
    } finally {
      setSubmitting(false);
    }
  }

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
      <div className="flex items-center justify-between">
        <div>
          <Link
            href="/admin/ingredients"
            className="flex items-center gap-1 text-[#7a9bb5] hover:text-[#00e5ff] text-sm mb-2"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Ingredients
          </Link>
          <h1 className="text-2xl font-bold text-white">Check In Delivery</h1>
          <p className="text-[#7a9bb5] mt-1">
            Enter the quantities received for each ingredient
          </p>
        </div>
      </div>

      {/* Notes */}
      <div>
        <Input
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Delivery notes (e.g., Sysco delivery 3/10)"
          className="bg-[#0d1f3c] border-[#1e3a5f] text-white placeholder:text-[#4a6a85]"
        />
      </div>

      {/* Ingredient list */}
      <div className="space-y-2">
        {ingredients.map((ing) => {
          const isLowStock = ing.stockQuantity < ing.lowStockThreshold;
          return (
            <div
              key={ing.id}
              className="rounded-lg border border-white/10 bg-[#0d1f3c] p-4 flex items-center gap-4"
            >
              <div className="flex-1 min-w-0">
                <p className="font-medium text-white truncate">{ing.name}</p>
                <p className={`text-sm ${isLowStock ? "text-[#ff4757]" : "text-[#7a9bb5]"}`}>
                  Current: {ing.stockQuantity} {unitShortLabel(ing.unit)}
                  {isLowStock && " (LOW)"}
                </p>
              </div>
              <div className="w-24 shrink-0">
                <Input
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="0"
                  value={quantities[ing.id] ?? ""}
                  onChange={(e) => updateQuantity(ing.id, e.target.value)}
                  className="bg-[#0a1628] border-[#1e3a5f] text-white text-center"
                  inputMode="decimal"
                />
              </div>
              <span className="text-sm text-[#7a9bb5] w-10 shrink-0">
                {unitShortLabel(ing.unit)}
              </span>
            </div>
          );
        })}
      </div>

      {/* Submit */}
      <div className="sticky bottom-0 bg-[#0f1f35]/95 backdrop-blur-sm border-t border-[#1e3a5f] pt-4 pb-2 -mx-4 px-4">
        <Button
          onClick={handleSubmit}
          disabled={submitting}
          className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
        >
          {submitting ? (
            <Loader2 className="h-4 w-4 animate-spin mr-2" />
          ) : (
            <PackagePlus className="h-4 w-4 mr-2" />
          )}
          Submit Delivery
        </Button>
      </div>
    </div>
  );
}
