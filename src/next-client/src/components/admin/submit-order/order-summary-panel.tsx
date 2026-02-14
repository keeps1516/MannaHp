"use client";

import { useMemo } from "react";
import { Minus, Plus, Trash2, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import type { CartItem } from "@/types/cart";
import { getUnitPrice, getLineTotal, getDisplayName } from "@/types/cart";

const TAX_RATE = 0.0825;

interface OrderSummaryPanelProps {
  items: CartItem[];
  orderNotes: string;
  onOrderNotesChange: (notes: string) => void;
  onUpdateQuantity: (id: string, quantity: number) => void;
  onRemoveItem: (id: string) => void;
  onSubmit: () => void;
  submitting: boolean;
}

export function OrderSummaryPanel({
  items,
  orderNotes,
  onOrderNotesChange,
  onUpdateQuantity,
  onRemoveItem,
  onSubmit,
  submitting,
}: OrderSummaryPanelProps) {
  const { subtotal, tax, total } = useMemo(() => {
    const sub = items.reduce((sum, i) => sum + getLineTotal(i), 0);
    const t = Math.round(sub * TAX_RATE * 100) / 100;
    return { subtotal: sub, tax: t, total: sub + t };
  }, [items]);

  return (
    <div className="flex flex-col h-full">
      <h2 className="text-sm font-semibold text-white mb-4">Order Summary</h2>

      {items.length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <p className="text-sm text-[#4a6a85]">No items added yet</p>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto space-y-3 min-h-0">
          {items.map((item) => {
            const ingredientNames = item.selectedIngredients
              ?.map((i) => i.ingredientName)
              .filter((v, i, a) => a.indexOf(v) === i); // dedupe for display

            return (
              <div
                key={item.id}
                className="rounded-lg border border-white/10 bg-white/5 p-3 space-y-2"
              >
                {/* Name + price */}
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-white truncate">
                      {getDisplayName(item)}
                    </p>
                    {ingredientNames && ingredientNames.length > 0 && (
                      <p className="text-xs text-[#7a9bb5] line-clamp-2 mt-0.5">
                        {ingredientNames.join(", ")}
                      </p>
                    )}
                    {item.notes && (
                      <p className="text-xs text-amber-400/80 italic mt-0.5">
                        {item.notes}
                      </p>
                    )}
                  </div>
                  <span className="text-sm font-semibold text-white shrink-0">
                    ${getLineTotal(item).toFixed(2)}
                  </span>
                </div>

                {/* Quantity + remove */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() =>
                        onUpdateQuantity(item.id, item.quantity - 1)
                      }
                      disabled={item.quantity <= 1}
                      className="h-6 w-6 flex items-center justify-center rounded bg-white/10 text-white disabled:opacity-30 cursor-pointer disabled:cursor-default"
                    >
                      <Minus className="h-3 w-3" />
                    </button>
                    <span className="w-5 text-center text-xs text-white font-medium">
                      {item.quantity}
                    </span>
                    <button
                      type="button"
                      onClick={() =>
                        onUpdateQuantity(item.id, item.quantity + 1)
                      }
                      className="h-6 w-6 flex items-center justify-center rounded bg-white/10 text-white cursor-pointer"
                    >
                      <Plus className="h-3 w-3" />
                    </button>
                    <span className="text-xs text-[#4a6a85] ml-1">
                      @ ${getUnitPrice(item).toFixed(2)}
                    </span>
                  </div>
                  <button
                    type="button"
                    onClick={() => onRemoveItem(item.id)}
                    className="h-6 w-6 flex items-center justify-center rounded text-[#ff4757] hover:bg-[#ff4757]/10 cursor-pointer"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Order notes + totals + submit */}
      <div className="mt-4 space-y-3 border-t border-white/10 pt-4">
        <div>
          <label className="text-xs font-medium text-[#7a9bb5] mb-1 block">
            Order Notes
          </label>
          <Textarea
            value={orderNotes}
            onChange={(e) => onOrderNotesChange(e.target.value)}
            placeholder="Special instructions for this order..."
            className="bg-white/5 border-white/10 text-white placeholder:text-[#4a6a85] text-sm resize-none"
            rows={2}
          />
        </div>

        {items.length > 0 && (
          <div className="space-y-1 text-sm">
            <div className="flex items-center justify-between text-[#7a9bb5]">
              <span>Subtotal</span>
              <span>${subtotal.toFixed(2)}</span>
            </div>
            <div className="flex items-center justify-between text-[#7a9bb5]">
              <span>Tax (8.25%)</span>
              <span>${tax.toFixed(2)}</span>
            </div>
            <div className="flex items-center justify-between text-white font-semibold text-base pt-1">
              <span>Total</span>
              <span>${total.toFixed(2)}</span>
            </div>
          </div>
        )}

        <Button
          onClick={onSubmit}
          disabled={items.length === 0 || submitting}
          className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold cursor-pointer disabled:opacity-50"
        >
          {submitting ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin mr-2" />
              Submitting...
            </>
          ) : (
            `Submit Order${items.length > 0 ? ` \u2014 $${total.toFixed(2)}` : ""}`
          )}
        </Button>
      </div>
    </div>
  );
}
