"use client";

import { useState } from "react";
import Image from "next/image";
import { ShoppingCart } from "lucide-react";
import { Button } from "@/components/ui/button";
import { CartDrawer } from "@/components/cart-drawer";
import { useCart } from "@/store/cart-context";

export function Header() {
  const [cartOpen, setCartOpen] = useState(false);
  const cart = useCart();

  return (
    <>
      <header className="fixed top-0 left-0 right-0 z-50 h-14 bg-[#0a1628] border-b border-[#1e3a5f] flex items-center justify-between px-4 shadow-lg shadow-black/30">
        <div className="flex items-center gap-2.5">
          <Image
            src="/logo.png"
            alt="Manna + HP"
            width={36}
            height={36}
            className="rounded-full"
          />
          <div className="leading-tight">
            <h1 className="text-sm font-bold tracking-wide text-white">
              Manna + HP
            </h1>
            <p className="text-[10px] text-[#7a9bb5]">
              Boardgame Cafe &amp; Burrito Bowls
            </p>
          </div>
        </div>
        <Button
          variant="ghost"
          className="text-[#00e5ff] hover:bg-[#00e5ff]/10 gap-1.5 px-3 border border-[#1e3a5f] rounded-full"
          onClick={() => setCartOpen(true)}
        >
          <ShoppingCart className="h-4 w-4" />
          {cart.itemCount > 0 && (
            <span className="font-semibold text-sm">
              ${cart.subtotal.toFixed(2)}
            </span>
          )}
        </Button>
      </header>
      <CartDrawer open={cartOpen} onOpenChange={setCartOpen} />
    </>
  );
}
