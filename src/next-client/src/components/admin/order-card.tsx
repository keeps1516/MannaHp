"use client";

import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { ChevronDown, Loader2 } from "lucide-react";
import { timeAgo } from "@/lib/time-ago";
import { formatMeasurement } from "@/lib/unit-label";
import { OrderStatus, PaymentMethod } from "@/types/api";
import type { OrderDto } from "@/types/api";

interface OrderCardProps {
  order: OrderDto;
  onAdvance: (orderId: string, nextStatus: OrderStatus) => Promise<void>;
  defaultOpen?: boolean;
}

const statusConfig: Record<
  number,
  { nextStatus: OrderStatus; label: string }
> = {
  [OrderStatus.Received]: {
    nextStatus: OrderStatus.Preparing,
    label: "Start Preparing",
  },
  [OrderStatus.Preparing]: {
    nextStatus: OrderStatus.Ready,
    label: "Mark Ready",
  },
  [OrderStatus.Ready]: {
    nextStatus: OrderStatus.Completed,
    label: "Complete",
  },
};

export function OrderCard({
  order,
  onAdvance,
  defaultOpen = true,
}: OrderCardProps) {
  const [advancing, setAdvancing] = useState(false);
  const [open, setOpen] = useState(defaultOpen);
  const config = statusConfig[order.status];

  async function handleAdvance(e: React.MouseEvent) {
    e.stopPropagation();
    if (!config) return;
    setAdvancing(true);
    try {
      await onAdvance(order.id, config.nextStatus);
    } finally {
      setAdvancing(false);
    }
  }

  const itemCount = order.items.reduce((sum, i) => sum + i.quantity, 0);

  return (
    <div className="rounded-lg border border-white/10 bg-[#0d1f3c] overflow-hidden">
      {/* Header — always visible, tap to toggle */}
      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className="w-full flex items-center justify-between p-4 cursor-pointer hover:bg-[#112240] active:bg-[#15294d] transition-colors"
      >
        <div className="flex items-center gap-3">
          <span className="text-sm font-mono text-white font-bold">
            #{order.orderNumber}
          </span>
          <span className="text-xs text-[#4a6a85]">
            {itemCount} item{itemCount !== 1 && "s"}
          </span>
          {advancing && (
            <Loader2 className="h-3.5 w-3.5 animate-spin text-[#00e5ff]" />
          )}
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs text-[#7a9bb5]">
            {timeAgo(order.createdAt)}
          </span>
          <ChevronDown
            className={`h-4 w-4 text-[#4a6a85] transition-transform ${
              open ? "rotate-180" : ""
            }`}
          />
        </div>
      </button>

      {/* Body — collapsible, tap to advance */}
      {open && (
        <button
          type="button"
          onClick={handleAdvance}
          disabled={advancing || !config}
          className="w-full text-left px-4 pb-4 space-y-3 cursor-pointer disabled:cursor-default"
        >
          {/* Items */}
          <div className="space-y-2">
            {order.items.map((item) => (
              <div key={item.id} className="text-sm">
                <div className="flex items-center justify-between">
                  <span className="text-white">
                    {item.quantity > 1 && (
                      <span className="text-[#00e5ff]">
                        {item.quantity}x{" "}
                      </span>
                    )}
                    {item.menuItemName}
                    {item.variantName && (
                      <span className="text-[#7a9bb5]">
                        {" "}
                        ({item.variantName})
                      </span>
                    )}
                  </span>
                  <span className="text-[#7a9bb5] text-xs">
                    ${item.totalPrice.toFixed(2)}
                  </span>
                </div>
                {item.ingredients && item.ingredients.length > 0 && (
                  <div className="ml-4 mt-0.5 space-y-0.5">
                    {item.ingredients.map((ing) => {
                      const measurement = formatMeasurement(
                        ing.quantityUsed,
                        ing.ingredientUnit
                      );
                      return (
                        <div
                          key={ing.ingredientId}
                          className="flex items-center justify-between text-xs"
                        >
                          <span className="text-white">
                            {ing.ingredientName}
                            {measurement && (
                              <span className="text-[#00e5ff]">
                                {" "}{measurement}
                              </span>
                            )}
                          </span>
                          {ing.priceCharged > 0 && (
                            <span className="text-[#4a6a85]">
                              ${ing.priceCharged.toFixed(2)}
                            </span>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
                {item.notes && (
                  <div className="text-xs text-amber-400/80 ml-4 italic">
                    {item.notes}
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Footer */}
          <div className="flex items-center justify-between pt-2 border-t border-white/5">
            <div className="flex items-center gap-2">
              <span className="text-sm font-semibold text-white">
                ${order.total.toFixed(2)}
              </span>
              <Badge
                className={
                  order.paymentMethod === PaymentMethod.InStore
                    ? "bg-amber-500/10 text-amber-400 border-amber-500/20 text-xs hover:bg-amber-500/10"
                    : "bg-emerald-500/10 text-emerald-400 border-emerald-500/20 text-xs hover:bg-emerald-500/10"
                }
              >
                {order.paymentMethod === PaymentMethod.InStore
                  ? "In-Store"
                  : "Card"}
              </Badge>
            </div>
            {config && (
              <span className="text-xs text-[#4a6a85]">
                Tap → {config.label}
              </span>
            )}
          </div>
        </button>
      )}
    </div>
  );
}
