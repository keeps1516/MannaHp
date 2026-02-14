"use client";

import { useState, useMemo } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { QuantitySelector } from "@/components/quantity-selector";
import { useCart } from "@/store/cart-context";
import type { MenuItemDto, MenuItemVariantDto } from "@/types/api";
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

  const [selectedVariant, setSelectedVariant] =
    useState<MenuItemVariantDto | null>(activeVariants[0] ?? null);
  const [quantity, setQuantity] = useState(1);
  const [notes, setNotes] = useState("");

  const lineTotal = (selectedVariant?.price ?? 0) * quantity;

  function handleAddToCart() {
    if (!selectedVariant) return;

    cart.addItem({
      menuItem,
      variant: selectedVariant,
      selectedIngredients: null,
      quantity,
      notes: notes.trim() || null,
    });

    toast.success(
      `${menuItem.name} (${selectedVariant.name}) added to cart`
    );

    setQuantity(1);
    setNotes("");
  }

  return (
    <div className="space-y-6">
      {/* Back button */}
      <button
        onClick={() => router.back()}
        className="flex items-center gap-1.5 text-[#7a9bb5] hover:text-[#00e5ff] transition-colors text-sm"
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
