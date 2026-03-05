"use client";

import { useEffect, useState, useCallback, useRef } from "react";
import { useRouter } from "next/navigation";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { Elements } from "@stripe/react-stripe-js";
import { ArrowLeft, Loader2, ShieldCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { useCart } from "@/store/cart-context";
import { api } from "@/lib/api";
import { PaymentMethod } from "@/types/api";
import { getLineTotal, getDisplayName } from "@/types/cart";
import { toast } from "sonner";
import { CheckoutForm } from "@/components/checkout-form";

export default function CheckoutPage() {
  const cart = useCart();
  const router = useRouter();
  const [stripePromise, setStripePromise] = useState<Promise<Stripe | null> | null>(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [orderId, setOrderId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const orderCreatedRef = useRef(false);

  // Redirect if cart is empty
  useEffect(() => {
    if (cart.items.length === 0 && !orderId) {
      router.replace("/");
    }
  }, [cart.items.length, orderId, router]);

  // Create the order + PaymentIntent on mount (once only)
  useEffect(() => {
    if (cart.items.length === 0) return;
    if (orderCreatedRef.current) return;
    orderCreatedRef.current = true;

    let cancelled = false;

    async function createPaymentIntent() {
      try {
        const response = await api.createOrder({
          paymentMethod: PaymentMethod.Card,
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

        if (cancelled) return;

        if (!response.clientSecret || !response.stripePublishableKey) {
          setError("Card payments are not yet available. Please pay in-store instead.");
          return;
        }

        setOrderId(response.order.id);
        setClientSecret(response.clientSecret);
        setStripePromise(loadStripe(response.stripePublishableKey));
      } catch {
        if (!cancelled) {
          setError("Failed to create order. Please try again.");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    createPaymentIntent();
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handlePaymentSuccess = useCallback(() => {
    cart.clear();
    if (orderId) {
      router.push(`/order/${orderId}`);
    }
  }, [cart, orderId, router]);

  const handlePaymentError = useCallback((message: string) => {
    toast.error(message);
  }, []);

  if (cart.items.length === 0 && !orderId) {
    return null; // Will redirect
  }

  return (
    <div className="max-w-lg mx-auto space-y-6 mt-4">
      <Button
        variant="ghost"
        className="text-[#7a9bb5] hover:text-white hover:bg-white/5 -ml-2"
        onClick={() => router.back()}
      >
        <ArrowLeft className="h-4 w-4 mr-2" />
        Back to cart
      </Button>

      <div>
        <h1 className="text-2xl font-bold text-white">Checkout</h1>
        <p className="text-sm text-[#7a9bb5] mt-1">
          Complete your payment to place your order
        </p>
      </div>

      {/* Order Summary */}
      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
        <h2 className="font-semibold text-white text-sm">Order Summary</h2>
        <Separator className="bg-[#1e3a5f]" />
        {cart.items.map((item) => (
          <div key={item.id} className="flex justify-between text-sm">
            <div>
              <span className="text-white">
                {item.quantity}x {getDisplayName(item)}
              </span>
              {item.selectedIngredients && item.selectedIngredients.length > 0 && (
                <p className="text-xs text-[#7a9bb5]">
                  {item.selectedIngredients
                    .map((i) => i.ingredientName)
                    .join(", ")}
                </p>
              )}
            </div>
            <span className="font-medium text-[#00e5ff] shrink-0 ml-4">
              ${getLineTotal(item).toFixed(2)}
            </span>
          </div>
        ))}
        <Separator className="bg-[#1e3a5f]" />
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
      </div>

      {/* Payment Section */}
      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-4">
        <div className="flex items-center gap-2">
          <ShieldCheck className="h-4 w-4 text-[#00e5ff]" />
          <h2 className="font-semibold text-white text-sm">Payment Details</h2>
        </div>
        <Separator className="bg-[#1e3a5f]" />

        {loading && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-[#00e5ff]" />
            <span className="ml-2 text-sm text-[#7a9bb5]">
              Setting up payment...
            </span>
          </div>
        )}

        {error && (
          <div className="rounded-lg border border-[#ff4757]/40 bg-[#ff4757]/10 p-3 text-[#ff4757] text-sm">
            {error}
            <Button
              variant="ghost"
              size="sm"
              className="mt-2 text-[#ff4757] hover:text-white"
              onClick={() => router.back()}
            >
              Go back
            </Button>
          </div>
        )}

        {clientSecret && stripePromise && orderId && (
          <Elements
            stripe={stripePromise}
            options={{
              clientSecret,
              appearance: {
                theme: "night",
                variables: {
                  colorPrimary: "#00e5ff",
                  colorBackground: "#0f1f35",
                  colorText: "#ffffff",
                  colorTextSecondary: "#7a9bb5",
                  colorDanger: "#ff4757",
                  borderRadius: "8px",
                  fontFamily: "inherit",
                },
                rules: {
                  ".Input": {
                    backgroundColor: "#0a1628",
                    border: "1px solid rgba(255,255,255,0.1)",
                  },
                  ".Input:focus": {
                    borderColor: "#00e5ff",
                    boxShadow: "0 0 0 1px #00e5ff",
                  },
                  ".Label": {
                    color: "#7a9bb5",
                  },
                },
              },
            }}
          >
            <CheckoutForm
              orderId={orderId}
              total={cart.total}
              onSuccess={handlePaymentSuccess}
              onError={handlePaymentError}
            />
          </Elements>
        )}

        <p className="text-xs text-[#7a9bb5] text-center">
          Payments processed securely by Stripe
        </p>
      </div>
    </div>
  );
}
