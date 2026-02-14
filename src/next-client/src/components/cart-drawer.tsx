"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ShoppingCart, Trash2, X } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import { QuantitySelector } from "@/components/quantity-selector";
import { useCart } from "@/store/cart-context";
import { api } from "@/lib/api";
import { PaymentMethod } from "@/types/api";
import { getLineTotal, getDisplayName } from "@/types/cart";
import { toast } from "sonner";

interface CartDrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CartDrawer({ open, onOpenChange }: CartDrawerProps) {
  const cart = useCart();
  const router = useRouter();
  const [placing, setPlacing] = useState(false);

  async function handlePlaceOrder() {
    if (cart.items.length === 0) return;
    setPlacing(true);
    try {
      const order = await api.createOrder({
        paymentMethod: PaymentMethod.InStore,
        notes: null,
        items: cart.items.map((item) => ({
          menuItemId: item.menuItem.id,
          variantId: item.variant?.id ?? null,
          quantity: item.quantity,
          notes: item.notes,
          selectedIngredientIds: item.selectedIngredients
            ? item.selectedIngredients.map((i) => i.id)
            : null,
        })),
      });
      cart.clear();
      onOpenChange(false);
      router.push(`/order/${order.id}`);
    } catch {
      toast.error("Failed to place order. Please try again.");
    } finally {
      setPlacing(false);
    }
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="flex flex-col w-full sm:max-w-md p-0 bg-[#0f1f35] border-l border-[#1e3a5f] text-white">
        <SheetHeader className="px-6 py-4 border-b border-[#1e3a5f]">
          <div className="flex items-center justify-between">
            <SheetTitle className="text-white">Your Order</SheetTitle>
            <Button
              variant="ghost"
              size="icon"
              className="text-[#7a9bb5] hover:text-white hover:bg-[#1a3550]"
              onClick={() => onOpenChange(false)}
            >
              <X className="h-5 w-5" />
            </Button>
          </div>
        </SheetHeader>

        {cart.items.length === 0 ? (
          <div className="flex-1 flex flex-col items-center justify-center gap-3 text-[#7a9bb5]">
            <ShoppingCart className="h-12 w-12" />
            <p className="font-medium">Your cart is empty</p>
            <p className="text-sm">Tap a menu item to get started</p>
          </div>
        ) : (
          <>
            <ScrollArea className="flex-1 px-6 py-4">
              <div className="space-y-4">
                {cart.items.map((item) => (
                  <div
                    key={item.id}
                    className="bg-[#163a50] border border-[#1e3a5f] rounded-lg p-3 space-y-2"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <p className="font-medium text-sm text-white">
                          {getDisplayName(item)}
                        </p>
                        {item.selectedIngredients &&
                          item.selectedIngredients.length > 0 && (
                            <p className="text-xs text-[#7a9bb5] mt-0.5">
                              {item.selectedIngredients
                                .map((i) => i.ingredientName)
                                .join(", ")}
                            </p>
                          )}
                        {item.notes && (
                          <p className="text-xs text-[#00e5ff]/70 italic mt-0.5">
                            {item.notes}
                          </p>
                        )}
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-[#ff4757] hover:text-[#ff6b81] hover:bg-[#ff4757]/10"
                        onClick={() => cart.removeItem(item.id)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                    <div className="flex items-center justify-between">
                      <QuantitySelector
                        value={item.quantity}
                        onChange={(qty) => cart.updateQuantity(item.id, qty)}
                      />
                      <span className="font-medium text-sm text-[#00e5ff]">
                        ${getLineTotal(item).toFixed(2)}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </ScrollArea>

            <div className="border-t border-[#1e3a5f] px-6 py-4 space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-[#7a9bb5]">Subtotal</span>
                <span className="text-white">${cart.subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-[#7a9bb5]">Tax (8.25%)</span>
                <span className="text-[#7a9bb5]">${cart.tax.toFixed(2)}</span>
              </div>
              <Separator className="bg-[#1e3a5f]" />
              <div className="flex justify-between font-semibold">
                <span className="text-white">Total</span>
                <span className="text-[#00e5ff]">${cart.total.toFixed(2)}</span>
              </div>
              <Button
                className="w-full mt-2 bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
                size="lg"
                onClick={handlePlaceOrder}
                disabled={placing}
              >
                {placing ? "Placing Order..." : "Place Order"}
              </Button>
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}
