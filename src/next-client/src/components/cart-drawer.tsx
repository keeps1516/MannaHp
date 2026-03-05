"use client";

import { useState, useRef, useCallback, type MouseEvent } from "react";
import { useRouter } from "next/navigation";
import { ShoppingCart, Trash2, CreditCard, Store } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetDescription,
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
  const [showVideo, setShowVideo] = useState(false);
  const videoRef = useRef<HTMLVideoElement>(null);
  const pendingOrderIdRef = useRef<string | null>(null);
  const submittingRef = useRef(false);

  const finishOrder = useCallback(() => {
    setShowVideo(false);
    if (pendingOrderIdRef.current) {
      router.push(`/order/${pendingOrderIdRef.current}`);
      pendingOrderIdRef.current = null;
    }
  }, [router]);

  async function handlePlaceOrder(e?: MouseEvent) {
    e?.preventDefault();
    if (cart.items.length === 0) return;
    if (submittingRef.current) return;
    submittingRef.current = true;
    setPlacing(true);
    try {
      const response = await api.createOrder({
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
      pendingOrderIdRef.current = response.order.id;
      setShowVideo(true);
    } catch {
      toast.error("Failed to place order. Please try again.");
    } finally {
      setPlacing(false);
      submittingRef.current = false;
    }
  }

  function handlePayWithCard() {
    onOpenChange(false);
    router.push("/checkout");
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="flex flex-col w-full sm:max-w-md p-0 bg-[#0f1f35] border-l border-[#1e3a5f] text-white">
        <SheetHeader className="px-6 py-4 border-b border-[#1e3a5f]">
          <SheetTitle className="text-white">Your Order</SheetTitle>
          <SheetDescription className="sr-only">
            Review and manage items in your cart
          </SheetDescription>
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

              <div className="flex gap-2 mt-2">
                <Button
                  className="flex-1 bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
                  size="lg"
                  onClick={handlePayWithCard}
                  disabled={placing}
                >
                  <CreditCard className="h-4 w-4 mr-2" />
                  Pay with Card
                </Button>
                <Button
                  className="flex-1 bg-white/10 text-white hover:bg-white/20 font-semibold border border-white/20"
                  variant="outline"
                  size="lg"
                  onClick={handlePlaceOrder}
                  disabled={placing}
                >
                  {placing ? (
                    "Placing..."
                  ) : (
                    <>
                      <Store className="h-4 w-4 mr-2" />
                      Pay In-Store
                    </>
                  )}
                </Button>
              </div>
            </div>
          </>
        )}
      </SheetContent>

      {showVideo && (
        <div
          className="fixed inset-0 z-[9999] bg-black flex items-center justify-center cursor-pointer"
          onClick={finishOrder}
        >
          <video
            ref={videoRef}
            src="/all-hearts-restored.mp4"
            autoPlay
            playsInline
            onEnded={finishOrder}
            className="w-full h-full object-cover"
          />
        </div>
      )}
    </Sheet>
  );
}
