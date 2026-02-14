"use client";

import { CartProvider } from "@/store/cart-context";
import { Header } from "@/components/header";

export default function CustomerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <CartProvider>
      <Header />
      <main className="pt-16 pb-8 px-4 max-w-4xl mx-auto">{children}</main>
    </CartProvider>
  );
}
