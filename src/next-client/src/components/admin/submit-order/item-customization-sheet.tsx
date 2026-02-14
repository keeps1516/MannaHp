"use client";

import { useState, useMemo } from "react";
import { Minus, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Checkbox } from "@/components/ui/checkbox";
import { formatMeasurement } from "@/lib/unit-label";
import type {
  MenuItemDto,
  MenuItemVariantDto,
  AvailableIngredientDto,
} from "@/types/api";
import type { CartItem } from "@/types/cart";

interface ItemCustomizationSheetProps {
  menuItem: MenuItemDto | null;
  onClose: () => void;
  onAddToOrder: (item: Omit<CartItem, "id">) => void;
}

export function ItemCustomizationSheet({
  menuItem,
  onClose,
  onAddToOrder,
}: ItemCustomizationSheetProps) {
  return (
    <Sheet open={menuItem !== null} onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="w-full sm:max-w-md bg-[#0d1f3c] border-white/10 overflow-y-auto">
        {menuItem && (
          <SheetInner
            menuItem={menuItem}
            onClose={onClose}
            onAddToOrder={onAddToOrder}
          />
        )}
      </SheetContent>
    </Sheet>
  );
}

/** Inner component so state resets when menuItem changes */
function SheetInner({
  menuItem,
  onClose,
  onAddToOrder,
}: {
  menuItem: MenuItemDto;
  onClose: () => void;
  onAddToOrder: (item: Omit<CartItem, "id">) => void;
}) {
  if (menuItem.isCustomizable) {
    return (
      <CustomizableItemForm
        menuItem={menuItem}
        onClose={onClose}
        onAddToOrder={onAddToOrder}
      />
    );
  }
  return (
    <FixedItemForm
      menuItem={menuItem}
      onClose={onClose}
      onAddToOrder={onAddToOrder}
    />
  );
}

// ─── Customizable Item (Bowls) ────────────────────────────────────────

function CustomizableItemForm({
  menuItem,
  onClose,
  onAddToOrder,
}: {
  menuItem: MenuItemDto;
  onClose: () => void;
  onAddToOrder: (item: Omit<CartItem, "id">) => void;
}) {
  const activeIngredients = useMemo(
    () =>
      (menuItem.availableIngredients ?? [])
        .filter((i) => i.active)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItem.availableIngredients]
  );

  const grouped = useMemo(() => {
    const groups: Record<string, AvailableIngredientDto[]> = {};
    for (const ing of activeIngredients) {
      if (!groups[ing.groupName]) groups[ing.groupName] = [];
      groups[ing.groupName].push(ing);
    }
    return groups;
  }, [activeIngredients]);

  const [quantities, setQuantities] = useState<Record<string, number>>(() => {
    const init: Record<string, number> = {};
    for (const ing of activeIngredients) {
      init[ing.id] = ing.isDefault ? 1 : 0;
    }
    return init;
  });
  const [qty, setQty] = useState(1);
  const [notes, setNotes] = useState("");

  const runningTotal = useMemo(() => {
    let total = 0;
    for (const ing of activeIngredients) {
      total += ing.customerPrice * (quantities[ing.id] ?? 0);
    }
    return total;
  }, [activeIngredients, quantities]);

  const hasSelection = Object.values(quantities).some((q) => q > 0);

  function updateIngQty(id: string, delta: number) {
    setQuantities((prev) => {
      const current = prev[id] ?? 0;
      const next = Math.max(0, Math.min(10, current + delta));
      return { ...prev, [id]: next };
    });
  }

  function handleAdd() {
    const selected: AvailableIngredientDto[] = [];
    for (const ing of activeIngredients) {
      const q = quantities[ing.id] ?? 0;
      for (let i = 0; i < q; i++) {
        selected.push(ing);
      }
    }

    onAddToOrder({
      menuItem,
      variant: null,
      selectedIngredients: selected,
      quantity: qty,
      notes: notes.trim() || null,
    });
    onClose();
  }

  return (
    <>
      <SheetHeader>
        <SheetTitle className="text-white">{menuItem.name}</SheetTitle>
        {menuItem.description && (
          <p className="text-sm text-[#7a9bb5]">{menuItem.description}</p>
        )}
      </SheetHeader>

      <div className="mt-6 space-y-5">
        {Object.entries(grouped).map(([group, ingredients]) => (
          <div key={group}>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-[#00e5ff] mb-2">
              {group}
            </h3>
            <div className="space-y-1">
              {ingredients.map((ing) => {
                const q = quantities[ing.id] ?? 0;
                const measurement = formatMeasurement(
                  ing.quantityUsed,
                  ing.ingredientUnit
                );
                return (
                  <div
                    key={ing.id}
                    className="flex items-center justify-between rounded-md px-3 py-2 bg-white/5"
                  >
                    <div className="min-w-0">
                      <span className="text-sm text-white">
                        {ing.ingredientName}
                      </span>
                      {measurement && (
                        <span className="text-xs text-[#4a6a85] ml-1.5">
                          {measurement}
                        </span>
                      )}
                      {ing.customerPrice > 0 && (
                        <span className="text-xs text-[#7a9bb5] ml-1.5">
                          ${ing.customerPrice.toFixed(2)}
                        </span>
                      )}
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      <button
                        type="button"
                        onClick={() => updateIngQty(ing.id, -1)}
                        disabled={q === 0}
                        className="h-7 w-7 flex items-center justify-center rounded bg-white/10 text-white disabled:opacity-30 cursor-pointer disabled:cursor-default"
                      >
                        <Minus className="h-3.5 w-3.5" />
                      </button>
                      <span className="w-5 text-center text-sm text-white font-medium">
                        {q}
                      </span>
                      <button
                        type="button"
                        onClick={() => updateIngQty(ing.id, 1)}
                        disabled={q >= 10}
                        className="h-7 w-7 flex items-center justify-center rounded bg-white/10 text-white disabled:opacity-30 cursor-pointer disabled:cursor-default"
                      >
                        <Plus className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        ))}

        {/* Notes */}
        <div>
          <label className="text-xs font-medium text-[#7a9bb5] mb-1 block">
            Item Notes
          </label>
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="e.g. extra hot, no onions..."
            className="bg-white/5 border-white/10 text-white placeholder:text-[#4a6a85] text-sm resize-none"
            rows={2}
          />
        </div>

        {/* Quantity */}
        <div className="flex items-center justify-between">
          <span className="text-sm text-[#7a9bb5]">Quantity</span>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setQty((q) => Math.max(1, q - 1))}
              disabled={qty <= 1}
              className="h-8 w-8 flex items-center justify-center rounded-lg bg-white/10 text-white disabled:opacity-30 cursor-pointer disabled:cursor-default"
            >
              <Minus className="h-4 w-4" />
            </button>
            <span className="w-6 text-center text-white font-semibold">
              {qty}
            </span>
            <button
              type="button"
              onClick={() => setQty((q) => Math.min(20, q + 1))}
              className="h-8 w-8 flex items-center justify-center rounded-lg bg-white/10 text-white cursor-pointer"
            >
              <Plus className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Running total + Add button */}
        <div className="border-t border-white/10 pt-4 space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sm text-[#7a9bb5]">Item Total</span>
            <span className="text-lg font-bold text-white">
              ${(runningTotal * qty).toFixed(2)}
            </span>
          </div>
          <Button
            onClick={handleAdd}
            disabled={!hasSelection}
            className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold cursor-pointer disabled:opacity-50"
          >
            Add to Order
          </Button>
        </div>
      </div>
    </>
  );
}

// ─── Fixed Item (Coffee, Drinks) ──────────────────────────────────────

function FixedItemForm({
  menuItem,
  onClose,
  onAddToOrder,
}: {
  menuItem: MenuItemDto;
  onClose: () => void;
  onAddToOrder: (item: Omit<CartItem, "id">) => void;
}) {
  const activeVariants = useMemo(
    () =>
      menuItem.variants
        .filter((v) => v.active)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItem.variants]
  );

  const activeAddOns = useMemo(
    () =>
      (menuItem.availableIngredients ?? [])
        .filter((i) => i.active)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    [menuItem.availableIngredients]
  );

  const addOnGroups = useMemo(() => {
    const groups: Record<string, AvailableIngredientDto[]> = {};
    for (const addOn of activeAddOns) {
      if (!groups[addOn.groupName]) groups[addOn.groupName] = [];
      groups[addOn.groupName].push(addOn);
    }
    return groups;
  }, [activeAddOns]);

  const [selectedVariant, setSelectedVariant] =
    useState<MenuItemVariantDto | null>(activeVariants[0] ?? null);
  const [addOnQtys, setAddOnQtys] = useState<Record<string, number>>(() => {
    const init: Record<string, number> = {};
    for (const addOn of activeAddOns) {
      init[addOn.id] = addOn.isDefault ? 1 : 0;
    }
    return init;
  });
  const [qty, setQty] = useState(1);
  const [notes, setNotes] = useState("");

  const addOnTotal = useMemo(() => {
    let total = 0;
    for (const addOn of activeAddOns) {
      total += addOn.customerPrice * (addOnQtys[addOn.id] ?? 0);
    }
    return total;
  }, [activeAddOns, addOnQtys]);

  const unitPrice = (selectedVariant?.price ?? 0) + addOnTotal;
  const lineTotal = unitPrice * qty;

  function updateAddOnQty(id: string, delta: number) {
    setAddOnQtys((prev) => {
      const current = prev[id] ?? 0;
      const next = Math.max(0, Math.min(10, current + delta));
      return { ...prev, [id]: next };
    });
  }

  function handleAdd() {
    if (!selectedVariant) return;

    const selected: AvailableIngredientDto[] = [];
    for (const addOn of activeAddOns) {
      const q = addOnQtys[addOn.id] ?? 0;
      for (let i = 0; i < q; i++) {
        selected.push(addOn);
      }
    }

    onAddToOrder({
      menuItem,
      variant: selectedVariant,
      selectedIngredients: selected.length > 0 ? selected : null,
      quantity: qty,
      notes: notes.trim() || null,
    });
    onClose();
  }

  return (
    <>
      <SheetHeader>
        <SheetTitle className="text-white">{menuItem.name}</SheetTitle>
        {menuItem.description && (
          <p className="text-sm text-[#7a9bb5]">{menuItem.description}</p>
        )}
      </SheetHeader>

      <div className="mt-6 space-y-5">
        {/* Variant selector */}
        {activeVariants.length > 1 && (
          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wider text-[#00e5ff] mb-2">
              Size
            </h3>
            <div className="grid grid-cols-2 gap-2">
              {activeVariants.map((v) => (
                <button
                  key={v.id}
                  type="button"
                  onClick={() => setSelectedVariant(v)}
                  className={`rounded-lg border p-3 text-left transition-all cursor-pointer ${
                    selectedVariant?.id === v.id
                      ? "border-[#00e5ff] bg-[#00e5ff]/10"
                      : "border-white/10 bg-white/5 hover:border-white/20"
                  }`}
                >
                  <p className="text-sm font-medium text-white">{v.name}</p>
                  <p className="text-xs text-[#7a9bb5]">
                    ${v.price.toFixed(2)}
                  </p>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Add-ons */}
        {Object.entries(addOnGroups).length > 0 && (
          <div className="space-y-4">
            {Object.entries(addOnGroups).map(([group, addOns]) => (
              <div key={group}>
                <h3 className="text-xs font-semibold uppercase tracking-wider text-[#00e5ff] mb-2">
                  {group}
                </h3>
                <div className="space-y-1">
                  {addOns.map((addOn) => {
                    const q = addOnQtys[addOn.id] ?? 0;
                    return (
                      <div
                        key={addOn.id}
                        className="flex items-center justify-between rounded-md px-3 py-2 bg-white/5"
                      >
                        <div className="flex items-center gap-2">
                          <Checkbox
                            checked={q > 0}
                            onCheckedChange={(checked) =>
                              setAddOnQtys((prev) => ({
                                ...prev,
                                [addOn.id]: checked ? 1 : 0,
                              }))
                            }
                            className="border-white/20 data-[state=checked]:bg-[#00e5ff] data-[state=checked]:border-[#00e5ff]"
                          />
                          <span className="text-sm text-white">
                            {addOn.ingredientName}
                          </span>
                          {addOn.customerPrice > 0 && (
                            <span className="text-xs text-[#7a9bb5]">
                              +${addOn.customerPrice.toFixed(2)}
                            </span>
                          )}
                        </div>
                        {q > 0 && (
                          <div className="flex items-center gap-2 shrink-0">
                            <button
                              type="button"
                              onClick={() => updateAddOnQty(addOn.id, -1)}
                              disabled={q <= 1}
                              className="h-6 w-6 flex items-center justify-center rounded bg-white/10 text-white disabled:opacity-30 cursor-pointer disabled:cursor-default"
                            >
                              <Minus className="h-3 w-3" />
                            </button>
                            <span className="w-4 text-center text-xs text-white">
                              {q}
                            </span>
                            <button
                              type="button"
                              onClick={() => updateAddOnQty(addOn.id, 1)}
                              disabled={q >= 10}
                              className="h-6 w-6 flex items-center justify-center rounded bg-white/10 text-white disabled:opacity-30 cursor-pointer disabled:cursor-default"
                            >
                              <Plus className="h-3 w-3" />
                            </button>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Notes */}
        <div>
          <label className="text-xs font-medium text-[#7a9bb5] mb-1 block">
            Item Notes
          </label>
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="e.g. extra hot, oat milk..."
            className="bg-white/5 border-white/10 text-white placeholder:text-[#4a6a85] text-sm resize-none"
            rows={2}
          />
        </div>

        {/* Quantity */}
        <div className="flex items-center justify-between">
          <span className="text-sm text-[#7a9bb5]">Quantity</span>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setQty((q) => Math.max(1, q - 1))}
              disabled={qty <= 1}
              className="h-8 w-8 flex items-center justify-center rounded-lg bg-white/10 text-white disabled:opacity-30 cursor-pointer disabled:cursor-default"
            >
              <Minus className="h-4 w-4" />
            </button>
            <span className="w-6 text-center text-white font-semibold">
              {qty}
            </span>
            <button
              type="button"
              onClick={() => setQty((q) => Math.min(20, q + 1))}
              className="h-8 w-8 flex items-center justify-center rounded-lg bg-white/10 text-white cursor-pointer"
            >
              <Plus className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Total + Add button */}
        <div className="border-t border-white/10 pt-4 space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sm text-[#7a9bb5]">Item Total</span>
            <span className="text-lg font-bold text-white">
              ${lineTotal.toFixed(2)}
            </span>
          </div>
          <Button
            onClick={handleAdd}
            disabled={!selectedVariant}
            className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold cursor-pointer disabled:opacity-50"
          >
            Add to Order
          </Button>
        </div>
      </div>
    </>
  );
}
