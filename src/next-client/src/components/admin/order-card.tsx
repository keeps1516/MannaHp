"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Loader2 } from "lucide-react";
import { timeAgo } from "@/lib/time-ago";
import { OrderStatus, PaymentMethod } from "@/types/api";
import type { OrderDto } from "@/types/api";

interface OrderCardProps {
  order: OrderDto;
  onAdvance: (orderId: string, nextStatus: OrderStatus) => Promise<void>;
}

const statusConfig: Record<
  number,
  { nextStatus: OrderStatus; label: string; color: string }
> = {
  [OrderStatus.Received]: {
    nextStatus: OrderStatus.Preparing,
    label: "Start Preparing",
    color: "bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0]",
  },
  [OrderStatus.Preparing]: {
    nextStatus: OrderStatus.Ready,
    label: "Mark Ready",
    color: "bg-emerald-500 text-white hover:bg-emerald-600",
  },
  [OrderStatus.Ready]: {
    nextStatus: OrderStatus.Completed,
    label: "Complete",
    color: "bg-violet-500 text-white hover:bg-violet-600",
  },
};

export function OrderCard({ order, onAdvance }: OrderCardProps) {
  const [advancing, setAdvancing] = useState(false);
  const config = statusConfig[order.status];

  async function handleAdvance() {
    if (!config) return;
    setAdvancing(true);
    try {
      await onAdvance(order.id, config.nextStatus);
    } finally {
      setAdvancing(false);
    }
  }

  return (
    <div className="rounded-lg border border-white/10 bg-[#0d1f3c] p-4 space-y-3">
      {/* Header */}
      <div className="flex items-center justify-between">
        <span className="text-sm font-mono text-white font-bold">
          #{order.id.slice(0, 8).toUpperCase()}
        </span>
        <span className="text-xs text-[#7a9bb5]">
          {timeAgo(order.createdAt)}
        </span>
      </div>

      {/* Items */}
      <div className="space-y-1.5">
        {order.items.map((item) => (
          <div key={item.id} className="text-sm">
            <div className="flex items-center justify-between">
              <span className="text-white">
                {item.quantity > 1 && (
                  <span className="text-[#00e5ff]">{item.quantity}x </span>
                )}
                {item.menuItemName}
                {item.variantName && (
                  <span className="text-[#7a9bb5]"> ({item.variantName})</span>
                )}
              </span>
              <span className="text-[#7a9bb5] text-xs">
                ${item.totalPrice.toFixed(2)}
              </span>
            </div>
            {item.ingredients && item.ingredients.length > 0 && (
              <div className="text-xs text-[#4a6a85] ml-4">
                {item.ingredients.map((ing) => ing.ingredientName).join(", ")}
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
      </div>

      {/* Action Button */}
      {config && (
        <Button
          onClick={handleAdvance}
          disabled={advancing}
          className={`w-full font-semibold ${config.color}`}
          size="sm"
        >
          {advancing && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
          {config.label}
        </Button>
      )}
    </div>
  );
}
