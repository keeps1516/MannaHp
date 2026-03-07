"use client";

import { useEffect } from "react";
import { CartProvider } from "@/store/cart-context";
import { Header } from "@/components/header";
import { captureStoreTokenFromUrl } from "@/lib/api";

export default function CustomerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  useEffect(() => {
    captureStoreTokenFromUrl();
  }, []);

  return (
    <CartProvider>
      <Header />
      <main className="pt-16 pb-8 px-4 max-w-4xl mx-auto">{children}</main>
    </CartProvider>
  );
}
