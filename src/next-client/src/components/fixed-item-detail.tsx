"use client";

import { useState, useMemo } from "react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { ArrowLeft, Minus, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { QuantitySelector } from "@/components/quantity-selector";
import { useCart } from "@/store/cart-context";
import { getIngredientEmoji } from "@/lib/ingredient-emoji";
import { formatMeasurement } from "@/lib/unit-label";
import type {
  MenuItemDto,
  MenuItemVariantDto,
  AvailableIngredientDto,
} from "@/types/api";
import { toast } from "sonner";

interface FixedItemDetailProps {
  menuItem: MenuItemDto;
}

export function FixedItemDetail({ menuItem }: FixedItemDetailProps) {
  const cart = useCart();
  const router = useRouter();

  const activeVariants = useMemo(
    () =>
      menuItem.variants
        .filter((v) => v.active)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItem.variants]
  );

  const activeAddOns = useMemo(
    () =>
      (menuItem.availableIngredients ?? [])
        .filter((i) => i.active)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItem.availableIngredients]
  );

  const addOnGroups = useMemo(() => {
    const groups: Record<string, AvailableIngredientDto[]> = {};
    for (const addOn of activeAddOns) {
      if (!groups[addOn.groupName]) groups[addOn.groupName] = [];
      groups[addOn.groupName].push(addOn);
    }
    return groups;
  }, [activeAddOns]);

  const hasAddOns = activeAddOns.length > 0;

  const [selectedVariant, setSelectedVariant] =
    useState<MenuItemVariantDto | null>(activeVariants[0] ?? null);
  const [addOnQtys, setAddOnQtys] = useState<Record<string, number>>(() => {
    const init: Record<string, number> = {};
    for (const addOn of activeAddOns) {
      init[addOn.id] = addOn.isDefault ? 1 : 0;
    }
    return init;
  });
  const [quantity, setQuantity] = useState(1);
  const [notes, setNotes] = useState("");

  const addOnTotal = useMemo(() => {
    let total = 0;
    for (const addOn of activeAddOns) {
      total += addOn.customerPrice * (addOnQtys[addOn.id] ?? 0);
    }
    return total;
  }, [activeAddOns, addOnQtys]);

  const unitPrice = (selectedVariant?.price ?? 0) + addOnTotal;
  const lineTotal = unitPrice * quantity;

  function updateAddOnQty(id: string, delta: number) {
    setAddOnQtys((prev) => {
      const current = prev[id] ?? 0;
      const next = Math.max(0, Math.min(10, current + delta));
      return { ...prev, [id]: next };
    });
  }

  function handleAddToCart() {
    if (!selectedVariant) return;

    // Build selected ingredients array (repeat for qty > 1, same as bowl builder)
    const selected: AvailableIngredientDto[] = [];
    for (const addOn of activeAddOns) {
      const qty = addOnQtys[addOn.id] ?? 0;
      for (let i = 0; i < qty; i++) {
        selected.push(addOn);
      }
    }

    cart.addItem({
      menuItem,
      variant: selectedVariant,
      selectedIngredients: selected.length > 0 ? selected : null,
      quantity,
      notes: notes.trim() || null,
    });

    toast.success(
      `${menuItem.name} (${selectedVariant.name}) added to cart`
    );

    setQuantity(1);
    setNotes("");
    // Reset add-ons to defaults
    const reset: Record<string, number> = {};
    for (const addOn of activeAddOns) {
      reset[addOn.id] = addOn.isDefault ? 1 : 0;
    }
    setAddOnQtys(reset);
  }

  return (
    <div className="space-y-6">
      {/* Back button */}
      <button
        onClick={() => router.back()}
        className="sticky top-16 z-40 flex items-center gap-1.5 text-[#7a9bb5] hover:text-[#00e5ff] transition-colors text-sm bg-[#0f1f35]/95 backdrop-blur-sm py-2 -mx-4 px-4 border-b border-[#1e3a5f]/50"
      >
        <ArrowLeft className="h-4 w-4" />
        Back
      </button>

      {/* Item header */}
      <div>
        <h2 className="text-2xl font-bold text-white">{menuItem.name}</h2>
        {menuItem.description && (
          <p className="text-[#7a9bb5] mt-1">{menuItem.description}</p>
        )}
      </div>

      {/* Hero image */}
      {menuItem.imageUrl && (
        <div className="relative w-full aspect-[16/9] rounded-xl overflow-hidden bg-[#163a50]">
          <Image
            src={menuItem.imageUrl}
            alt={menuItem.name}
            fill
            className="object-cover"
            sizes="(max-width: 768px) 100vw, 768px"
            priority
          />
          {menuItem.imageApproximate && (
            <span className="absolute bottom-2 right-2 text-[10px] text-white/70 bg-black/50 px-2 py-0.5 rounded-full">
              Not an accurate image
            </span>
          )}
        </div>
      )}

      {/* Variant selector */}
      {activeVariants.length > 0 && (
        <div className="space-y-3">
          <h3 className="text-sm font-semibold text-[#7a9bb5] uppercase tracking-wide">
            {activeVariants.length > 1 ? "Choose a size" : "Size"}
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
            {activeVariants.map((variant) => {
              const isSelected = selectedVariant?.id === variant.id;
              return (
                <button
                  key={variant.id}
                  onClick={() => setSelectedVariant(variant)}
                  className={`rounded-xl border p-4 text-left transition-all duration-150 ${
                    isSelected
                      ? "border-[#00e5ff]/50 bg-[#00e5ff]/5 shadow-[0_0_15px_rgba(0,229,255,0.08)]"
                      : "border-[#1e3a5f] bg-[#163a50] hover:border-[#2a5080]"
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span
                      className={`font-medium ${
                        isSelected ? "text-[#00e5ff]" : "text-white"
                      }`}
                    >
                      {variant.name}
                    </span>
                    <span
                      className={`font-semibold ${
                        isSelected ? "text-[#00e5ff]" : "text-[#7a9bb5]"
                      }`}
                    >
                      ${variant.price.toFixed(2)}
                    </span>
                  </div>
                </button>
              );
            })}
          </div>
        </div>
      )}

      {/* Add-ons */}
      {hasAddOns &&
        Object.entries(addOnGroups).map(([groupName, addOns]) => (
          <div key={groupName} className="space-y-3">
            <h3 className="text-sm font-semibold text-[#7a9bb5] uppercase tracking-wide">
              {groupName}
            </h3>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
              {addOns.map((addOn) => {
                const qty = addOnQtys[addOn.id] ?? 0;
                const isSelected = qty > 0;
                return (
                  <div
                    key={addOn.id}
                    className={`rounded-xl border transition-all duration-150 ${
                      isSelected
                        ? "border-[#00e5ff]/50 bg-[#00e5ff]/5 shadow-[0_0_15px_rgba(0,229,255,0.08)]"
                        : "border-[#1e3a5f] bg-[#163a50] hover:border-[#2a5080]"
                    }`}
                  >
                    <div
                      className="flex flex-col items-center pt-3 pb-1.5 px-2 cursor-pointer"
                      onClick={() =>
                        qty === 0
                          ? updateAddOnQty(addOn.id, 1)
                          : updateAddOnQty(addOn.id, -qty)
                      }
                    >
                      <span className="text-2xl mb-1" role="img">
                        {getIngredientEmoji(addOn.ingredientName)}
                      </span>
                      <span className="text-sm font-medium text-center leading-tight text-white">
                        {addOn.ingredientName}
                      </span>
                      {formatMeasurement(
                        addOn.quantityUsed,
                        addOn.ingredientUnit
                      ) && (
                        <span className="text-xs text-white mt-0.5">
                          {formatMeasurement(
                            addOn.quantityUsed,
                            addOn.ingredientUnit
                          )}
                        </span>
                      )}
                      <span className="text-sm font-semibold text-[#00e5ff] mt-0.5">
                        +${addOn.customerPrice.toFixed(2)}
                      </span>
                    </div>

                    {/* Quantity controls */}
                    <div className="flex items-center justify-center gap-1 pb-2.5 px-2">
                      <div className="flex items-center bg-[#1a3550] rounded-full">
                        <button
                          className="h-7 w-7 flex items-center justify-center rounded-full text-[#7a9bb5] hover:text-white hover:bg-[#1e3a5f] transition-colors disabled:opacity-30"
                          disabled={qty <= 0}
                          onClick={() => updateAddOnQty(addOn.id, -1)}
                        >
                          <Minus className="h-3.5 w-3.5" />
                        </button>
                        <span className="w-6 text-center text-sm font-semibold text-white">
                          {qty}
                        </span>
                        <button
                          className="h-7 w-7 flex items-center justify-center rounded-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] transition-colors disabled:opacity-30"
                          disabled={qty >= 10}
                          onClick={() => updateAddOnQty(addOn.id, 1)}
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

      {/* Quantity */}
      <div className="space-y-3">
        <h3 className="text-sm font-semibold text-[#7a9bb5] uppercase tracking-wide">
          Quantity
        </h3>
        <QuantitySelector value={quantity} onChange={setQuantity} />
      </div>

      {/* Notes */}
      <div className="space-y-3">
        <h3 className="text-sm font-semibold text-[#7a9bb5] uppercase tracking-wide">
          Special requests (optional)
        </h3>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="e.g., extra hot, oat milk, etc."
          rows={2}
          className="w-full rounded-lg border border-[#1e3a5f] bg-[#163a50] text-white placeholder:text-[#4a6a85] p-3 text-sm focus:outline-none focus:ring-2 focus:ring-[#00e5ff]/50 resize-none"
        />
      </div>

      {/* Add to cart footer */}
      <div className="sticky bottom-0 bg-[#0f1f35]/95 backdrop-blur-sm border-t border-[#1e3a5f] pt-4 pb-2 -mx-4 px-4">
        <div className="flex items-center justify-between max-w-4xl mx-auto">
          <div>
            <p className="text-sm text-[#7a9bb5]">Total</p>
            <p className="text-2xl font-bold text-[#00e5ff]">
              ${lineTotal.toFixed(2)}
            </p>
            {addOnTotal > 0 && (
              <p className="text-xs text-[#4a6a85]">
                includes +${addOnTotal.toFixed(2)} add-ons
              </p>
            )}
          </div>
          <Button
            size="lg"
            className="px-8 bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
            onClick={handleAddToCart}
            disabled={!selectedVariant}
          >
            Add to Cart
          </Button>
        </div>
      </div>
    </div>
  );
}
