"use client";

import { useState, useEffect, useCallback } from "react";
import { Loader2, ShoppingCart } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { PaymentMethod } from "@/types/api";
import type { MenuItemDto, CategoryDto } from "@/types/api";
import type { CartItem } from "@/types/cart";
import { getLineTotal } from "@/types/cart";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { VisuallyHidden } from "radix-ui";
import { MenuItemPicker } from "@/components/admin/submit-order/menu-item-picker";
import { ItemCustomizationSheet } from "@/components/admin/submit-order/item-customization-sheet";
import { OrderSummaryPanel } from "@/components/admin/submit-order/order-summary-panel";

const TAX_RATE = 0.0825;

export default function SubmitOrderPage() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeCategory, setActiveCategory] = useState<string | null>(null);

  // Order state
  const [orderItems, setOrderItems] = useState<CartItem[]>([]);
  const [orderNotes, setOrderNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);

  // Item customization
  const [editingItem, setEditingItem] = useState<MenuItemDto | null>(null);

  // Mobile summary sheet
  const [summaryOpen, setSummaryOpen] = useState(false);

  useEffect(() => {
    Promise.all([api.getCategories(), api.getMenuItems()])
      .then(([cats, items]) => {
        setCategories(cats);
        setMenuItems(items);
      })
      .catch(() => toast.error("Failed to load menu"))
      .finally(() => setLoading(false));
  }, []);

  const handleAddToOrder = useCallback(
    (item: Omit<CartItem, "id">) => {
      setOrderItems((prev) => [
        ...prev,
        { ...item, id: crypto.randomUUID() },
      ]);
      toast.success(`Added ${item.menuItem.name}`);
    },
    []
  );

  function handleUpdateQuantity(id: string, quantity: number) {
    if (quantity <= 0) {
      setOrderItems((prev) => prev.filter((i) => i.id !== id));
      return;
    }
    setOrderItems((prev) =>
      prev.map((i) => (i.id === id ? { ...i, quantity } : i))
    );
  }

  function handleRemoveItem(id: string) {
    setOrderItems((prev) => prev.filter((i) => i.id !== id));
  }

  async function handleSubmit() {
    if (orderItems.length === 0) return;
    setSubmitting(true);

    try {
      await api.createOrder({
        paymentMethod: PaymentMethod.InStore,
        notes: orderNotes.trim() || null,
        items: orderItems.map((item) => ({
          menuItemId: item.menuItem.id,
          variantId: item.variant?.id ?? null,
          quantity: item.quantity,
          notes: item.notes,
          selectedIngredientIds: item.selectedIngredients
            ? item.selectedIngredients.map((i) => i.id)
            : null,
        })),
      });

      setOrderItems([]);
      setOrderNotes("");
      setSummaryOpen(false);
      toast.success("Order submitted!");
    } catch {
      toast.error("Failed to submit order");
    } finally {
      setSubmitting(false);
    }
  }

  // Computed
  const itemCount = orderItems.reduce((sum, i) => sum + i.quantity, 0);
  const subtotal = orderItems.reduce((sum, i) => sum + getLineTotal(i), 0);
  const total = subtotal + Math.round(subtotal * TAX_RATE * 100) / 100;

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-[#00e5ff]" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">New Order</h1>
        <p className="text-[#7a9bb5] mt-1">
          Create an order for a walk-in customer
        </p>
      </div>

      {/* Two-panel layout */}
      <div className="flex flex-col lg:flex-row gap-6">
        {/* Left: Menu browser */}
        <div className="flex-1 min-w-0">
          <MenuItemPicker
            categories={categories}
            menuItems={menuItems}
            activeCategory={activeCategory}
            onCategoryChange={setActiveCategory}
            onSelectItem={setEditingItem}
          />
        </div>

        {/* Right: Order summary (desktop) */}
        <div className="hidden lg:block w-80 xl:w-96 shrink-0">
          <div className="sticky top-4 rounded-lg border border-white/10 bg-[#0d1f3c] p-4 max-h-[calc(100vh-8rem)] flex flex-col">
            <OrderSummaryPanel
              items={orderItems}
              orderNotes={orderNotes}
              onOrderNotesChange={setOrderNotes}
              onUpdateQuantity={handleUpdateQuantity}
              onRemoveItem={handleRemoveItem}
              onSubmit={handleSubmit}
              submitting={submitting}
            />
          </div>
        </div>
      </div>

      {/* Mobile: sticky bottom bar */}
      {itemCount > 0 && (
        <div className="fixed bottom-0 left-0 right-0 lg:hidden border-t border-white/10 bg-[#0d1f3c] p-4 z-40">
          <Button
            onClick={() => setSummaryOpen(true)}
            className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold cursor-pointer"
          >
            <ShoppingCart className="h-4 w-4 mr-2" />
            View Order ({itemCount} item{itemCount !== 1 && "s"}) &mdash; $
            {total.toFixed(2)}
          </Button>
        </div>
      )}

      {/* Mobile: order summary sheet */}
      <Sheet open={summaryOpen} onOpenChange={setSummaryOpen}>
        <SheetContent
          side="bottom"
          className="bg-[#0d1f3c] border-white/10 h-[85vh] rounded-t-2xl"
        >
          <VisuallyHidden.Root>
            <SheetTitle>Order Summary</SheetTitle>
          </VisuallyHidden.Root>
          <div className="h-full overflow-y-auto pt-2">
            <OrderSummaryPanel
              items={orderItems}
              orderNotes={orderNotes}
              onOrderNotesChange={setOrderNotes}
              onUpdateQuantity={handleUpdateQuantity}
              onRemoveItem={handleRemoveItem}
              onSubmit={handleSubmit}
              submitting={submitting}
            />
          </div>
        </SheetContent>
      </Sheet>

      {/* Item customization sheet */}
      <ItemCustomizationSheet
        menuItem={editingItem}
        onClose={() => setEditingItem(null)}
        onAddToOrder={handleAddToOrder}
      />
    </div>
  );
}
