"use client";

import { useState, useMemo } from "react";
import { Minus, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { QuantitySelector } from "@/components/quantity-selector";
import { useCart } from "@/store/cart-context";
import { getIngredientEmoji } from "@/lib/ingredient-emoji";
import type { MenuItemDto, AvailableIngredientDto } from "@/types/api";
import { toast } from "sonner";

interface BowlBuilderProps {
  menuItem: MenuItemDto;
  onItemAdded: () => void;
}

export function BowlBuilder({ menuItem, onItemAdded }: BowlBuilderProps) {
  const cart = useCart();

  const activeIngredients = useMemo(
    () =>
      (menuItem.availableIngredients ?? [])
        .filter((i) => i.active)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItem.availableIngredients]
  );

  const [quantities, setQuantities] = useState<Record<string, number>>(() => {
    const init: Record<string, number> = {};
    for (const ing of activeIngredients) {
      init[ing.id] = ing.isDefault ? 1 : 0;
    }
    return init;
  });

  const [bowlName, setBowlName] = useState("");
  const [bowlQty, setBowlQty] = useState(1);

  const grouped = useMemo(() => {
    const groups: Record<string, AvailableIngredientDto[]> = {};
    for (const ing of activeIngredients) {
      if (!groups[ing.groupName]) groups[ing.groupName] = [];
      groups[ing.groupName].push(ing);
    }
    return groups;
  }, [activeIngredients]);

  const runningTotal = useMemo(() => {
    let total = 0;
    for (const ing of activeIngredients) {
      total += ing.customerPrice * (quantities[ing.id] ?? 0);
    }
    return total;
  }, [activeIngredients, quantities]);

  const hasSelection = useMemo(
    () => Object.values(quantities).some((q) => q > 0),
    [quantities]
  );

  function updateQty(id: string, delta: number) {
    setQuantities((prev) => {
      const current = prev[id] ?? 0;
      const next = Math.max(0, Math.min(10, current + delta));
      return { ...prev, [id]: next };
    });
  }

  function getGridCols(count: number): string {
    if (count >= 5) return "grid-cols-2 sm:grid-cols-3 md:grid-cols-5";
    if (count >= 3) return "grid-cols-2 sm:grid-cols-3";
    return "grid-cols-2";
  }

  function handleAddToCart() {
    const selected: AvailableIngredientDto[] = [];
    for (const ing of activeIngredients) {
      const qty = quantities[ing.id] ?? 0;
      for (let i = 0; i < qty; i++) {
        selected.push(ing);
      }
    }

    cart.addItem({
      menuItem,
      variant: null,
      selectedIngredients: selected,
      quantity: bowlQty,
      notes: bowlName.trim() || null,
    });

    const qtyLabel = bowlQty > 1 ? ` x${bowlQty}` : "";
    toast.success(
      bowlName.trim()
        ? `"${bowlName.trim()}"${qtyLabel} added to cart`
        : `${menuItem.name}${qtyLabel} added to cart`
    );

    const reset: Record<string, number> = {};
    for (const ing of activeIngredients) {
      reset[ing.id] = ing.isDefault ? 1 : 0;
    }
    setQuantities(reset);
    setBowlName("");
    setBowlQty(1);
    onItemAdded();
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-bold tracking-tight text-white">
          Build Your Burrito Bowl
        </h2>
        <p className="text-[#7a9bb5]">
          Choose any items you&apos;d like &ndash; no rules, just delicious!
        </p>
      </div>

      {/* Bowl Name */}
      <div className="max-w-md mx-auto">
        <label className="text-sm font-medium mb-1.5 block text-[#7a9bb5]">
          Bowl Name (optional)
        </label>
        <Input
          placeholder="e.g., Dad's Bowl, Sarah's Veggie Bowl"
          value={bowlName}
          onChange={(e) => setBowlName(e.target.value)}
          className="bg-[#163a50] border-[#1e3a5f] text-white placeholder:text-[#4a6a85] focus-visible:ring-[#00e5ff]/50"
        />
      </div>

      {/* Ingredient Groups */}
      {Object.entries(grouped).map(([groupName, ingredients]) => (
        <div key={groupName}>
          <h3 className="text-lg font-bold mb-3 text-white">{groupName}</h3>
          <div className={`grid gap-3 ${getGridCols(ingredients.length)}`}>
            {ingredients.map((ing) => {
              const qty = quantities[ing.id] ?? 0;
              const isSelected = qty > 0;
              return (
                <div
                  key={ing.id}
                  className={`rounded-xl border transition-all duration-150 ${
                    isSelected
                      ? "border-[#00e5ff]/50 bg-[#00e5ff]/5 shadow-[0_0_15px_rgba(0,229,255,0.08)]"
                      : "border-[#1e3a5f] bg-[#163a50] hover:border-[#2a5080]"
                  }`}
                >
                  {/* Emoji + Name + Price */}
                  <div
                    className="flex flex-col items-center pt-4 pb-2 px-2 cursor-pointer"
                    onClick={() =>
                      qty === 0 ? updateQty(ing.id, 1) : updateQty(ing.id, -qty)
                    }
                  >
                    <span className="text-3xl mb-2" role="img">
                      {getIngredientEmoji(ing.ingredientName)}
                    </span>
                    <span className="text-sm font-medium text-center leading-tight text-white">
                      {ing.ingredientName}
                    </span>
                    <span className="text-sm font-semibold text-[#00e5ff] mt-0.5">
                      ${ing.customerPrice.toFixed(2)}
                    </span>
                  </div>

                  {/* Quantity Controls */}
                  <div className="flex items-center justify-center gap-1 pb-3 px-2">
                    <div className="flex items-center bg-[#1a3550] rounded-full">
                      <button
                        className="h-7 w-7 flex items-center justify-center rounded-full text-[#7a9bb5] hover:text-white hover:bg-[#1e3a5f] transition-colors disabled:opacity-30"
                        disabled={qty <= 0}
                        onClick={() => updateQty(ing.id, -1)}
                      >
                        <Minus className="h-3.5 w-3.5" />
                      </button>
                      <span className="w-6 text-center text-sm font-semibold text-white">
                        {qty}
                      </span>
                      <button
                        className="h-7 w-7 flex items-center justify-center rounded-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] transition-colors disabled:opacity-30"
                        disabled={qty >= 10}
                        onClick={() => updateQty(ing.id, 1)}
                      >
                        <Plus className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      ))}

      {/* Sticky Add to Cart Footer */}
      <div className="sticky bottom-0 bg-[#0f1f35]/95 backdrop-blur-sm border-t border-[#1e3a5f] pt-4 pb-2 -mx-4 px-4">
        <div className="flex items-center justify-between max-w-4xl mx-auto">
          <div>
            <p className="text-sm text-[#7a9bb5]">Bowl Total</p>
            <p className="text-2xl font-bold text-[#00e5ff]">
              ${(runningTotal * bowlQty).toFixed(2)}
            </p>
            {bowlQty > 1 && (
              <p className="text-xs text-[#4a6a85]">
                ${runningTotal.toFixed(2)} each
              </p>
            )}
          </div>
          <div className="flex items-center gap-3">
            <QuantitySelector value={bowlQty} onChange={setBowlQty} />
            <Button
              size="lg"
              className="px-8 bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
              onClick={handleAddToCart}
              disabled={!hasSelection}
            >
              Add to Cart
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
