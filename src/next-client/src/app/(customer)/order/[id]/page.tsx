"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { CheckCircle, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { api } from "@/lib/api";
import type { OrderDto } from "@/types/api";

export default function OrderConfirmationPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!params.id) return;
    api
      .getOrder(params.id)
      .then(setOrder)
      .catch((e) =>
        setError(e instanceof Error ? e.message : "Failed to load order")
      )
      .finally(() => setLoading(false));
  }, [params.id]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-[#00e5ff]" />
      </div>
    );
  }

  if (error || !order) {
    const is404 = error?.includes("404");
    return (
      <div className="text-center mt-12 space-y-4">
        <p className="text-[#7a9bb5] text-lg">
          {is404 ? "Order not found" : (error ?? "Order not found")}
        </p>
        {!is404 && error && (
          <p className="text-sm text-[#ff4757]">{error}</p>
        )}
        <button
          onClick={() => router.push("/")}
          className="text-[#00e5ff] hover:underline text-sm"
        >
          Back to menu
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-lg mx-auto space-y-6 mt-4">
      <div className="text-center space-y-2">
        <CheckCircle className="h-16 w-16 text-[#00e5ff] mx-auto" />
        <p className="text-4xl font-bold text-[#00e5ff]">
          #{order.orderNumber}
        </p>
        <h1 className="text-2xl font-bold text-white">Order Placed!</h1>
        <p className="text-[#7a9bb5]">
          Thank you for your order. It will be ready soon.
        </p>
      </div>

      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
        <h2 className="font-semibold text-white">Order Details</h2>
        <Separator className="bg-[#1e3a5f]" />
        {order.items.map((item) => (
          <div key={item.id} className="flex justify-between text-sm">
            <div>
              <span className="text-white">
                {item.quantity}x {item.menuItemName}
                {item.variantName ? ` (${item.variantName})` : ""}
              </span>
              {item.ingredients && item.ingredients.length > 0 && (
                <p className="text-xs text-[#7a9bb5]">
                  {item.ingredients
                    .map((i) => i.ingredientName)
                    .join(", ")}
                </p>
              )}
            </div>
            <span className="font-medium text-[#00e5ff]">
              ${item.totalPrice.toFixed(2)}
            </span>
          </div>
        ))}
        <Separator className="bg-[#1e3a5f]" />
        <div className="flex justify-between text-sm">
          <span className="text-[#7a9bb5]">Subtotal</span>
          <span className="text-white">${order.subtotal.toFixed(2)}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-[#7a9bb5]">
            Tax ({(order.taxRate * 100).toFixed(2)}%)
          </span>
          <span className="text-[#7a9bb5]">${order.tax.toFixed(2)}</span>
        </div>
        <Separator className="bg-[#1e3a5f]" />
        <div className="flex justify-between font-semibold">
          <span className="text-white">Total</span>
          <span className="text-[#00e5ff]">${order.total.toFixed(2)}</span>
        </div>
      </div>

      <Button
        className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
        size="lg"
        onClick={() => router.push("/")}
      >
        Order Again
      </Button>
    </div>
  );
}
