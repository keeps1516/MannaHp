"use client";

import { useState, useRef, useCallback, type MouseEvent } from "react";
import { useRouter } from "next/navigation";
import { ShoppingCart, Trash2, Globe, Store, Pencil, QrCode, ScanLine } from "lucide-react";
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
  const [confirmedOrderNumber, setConfirmedOrderNumber] = useState<number | null>(null);
  const [tokenPrompt, setTokenPrompt] = useState<string | null>(null);
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

  async function fetchTokenMessage(): Promise<string> {
    const fallback = "Please scan the QR code at our counter to place an in-store order.";
    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5082"}/api/settings/public`
      );
      const data = await res.json();
      return data.storeTokenRequiredMessage ?? fallback;
    } catch {
      return fallback;
    }
  }

  async function handlePlaceOrder(e?: MouseEvent) {
    e?.preventDefault();
    if (cart.items.length === 0) return;
    if (submittingRef.current) return;
    submittingRef.current = true;
    setPlacing(true);
    setTokenPrompt(null);

    // Validate store token before placing order
    const storeToken = localStorage.getItem("storeToken");
    if (!storeToken) {
      const msg = await fetchTokenMessage();
      setTokenPrompt(msg);
      setPlacing(false);
      submittingRef.current = false;
      return;
    }

    try {
      const validation = await api.validateStoreToken(storeToken);
      if (!validation.valid) {
        localStorage.removeItem("storeToken");
        const msg = await fetchTokenMessage();
        setTokenPrompt(msg);
        setPlacing(false);
        submittingRef.current = false;
        return;
      }
    } catch {
      const msg = await fetchTokenMessage();
      setTokenPrompt(msg);
      setPlacing(false);
      submittingRef.current = false;
      return;
    }

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
      setConfirmedOrderNumber(response.order.orderNumber);
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
                      <div className="flex gap-1">
                        {item.menuItem.isCustomizable && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-[#00e5ff] hover:text-[#00c8e0] hover:bg-[#00e5ff]/10"
                            onClick={() => {
                              cart.setEditingItem(item);
                              onOpenChange(false);
                              router.push(`/category/${item.menuItem.categoryId}`);
                            }}
                            aria-label="Edit"
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                        )}
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-[#ff4757] hover:text-[#ff6b81] hover:bg-[#ff4757]/10"
                          onClick={() => cart.removeItem(item.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
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
                <span className="text-[#7a9bb5]">Tax ({(cart.taxRate * 100).toFixed(2)}%)</span>
                <span className="text-[#7a9bb5]">${cart.tax.toFixed(2)}</span>
              </div>
              <Separator className="bg-[#1e3a5f]" />
              <div className="flex justify-between font-semibold">
                <span className="text-white">Total</span>
                <span className="text-[#00e5ff]">${cart.total.toFixed(2)}</span>
              </div>

              {tokenPrompt && (
                <div className="rounded-2xl border-2 border-amber-400 bg-amber-500/10 p-8 mt-4 animate-in fade-in slide-in-from-bottom-2 duration-300">
                  <div className="flex flex-col items-center text-center space-y-5">
                    <div className="relative">
                      <div className="flex h-28 w-28 items-center justify-center rounded-full bg-amber-400/20 ring-4 ring-amber-400/30">
                        <ScanLine className="h-14 w-14 text-amber-300" />
                      </div>
                      <div className="absolute -top-1 -right-1 h-5 w-5 rounded-full bg-amber-400 animate-pulse" />
                    </div>
                    <p className="text-xl text-white font-bold leading-snug">
                      {tokenPrompt}
                    </p>
                  </div>
                </div>
              )}

              <div className="flex gap-2 mt-3">
                <Button
                  className="flex-1 bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
                  size="lg"
                  onClick={handlePayWithCard}
                  disabled={placing}
                >
                  <Globe className="h-4 w-4 mr-2" />
                  Pay Online
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
          {confirmedOrderNumber && (
            <div className="absolute top-[20%] left-0 right-0 text-center">
              <div className="inline-block bg-black/70 rounded-xl px-8 py-4 backdrop-blur-sm">
                <p className="text-white text-sm mb-1">Your Order</p>
                <p className="text-[#00e5ff] text-5xl font-bold">
                  #{confirmedOrderNumber}
                </p>
                <p className="text-white/60 text-xs mt-2">
                  Tap anywhere to view status
                </p>
              </div>
            </div>
          )}
        </div>
      )}
    </Sheet>
  );
}
