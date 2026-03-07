"use client";

import { useState, useEffect, useCallback, useRef } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  Loader2,
  PackagePlus,
  Search,
  X,
  Pencil,
  Plus,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { unitShortLabel, unitOptions } from "@/lib/unit-options";
import { QuantityCalculator } from "@/components/admin/quantity-calculator";
import type { IngredientDto, UnitOfMeasure } from "@/types/api";

interface DeliveryItem {
  ingredientId: string;
  ingredientName: string;
  unit: UnitOfMeasure;
  currentStock: number;
  quantity: number;
  costPaid: number;
  isNew?: boolean;
  // Fields for new ingredient creation
  newUnit?: UnitOfMeasure;
  newThreshold?: number;
}

export default function RestockPage() {
  const { token } = useAuth();
  const [ingredients, setIngredients] = useState<IngredientDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  // Search
  const [search, setSearch] = useState("");
  const [showDropdown, setShowDropdown] = useState(false);
  const searchRef = useRef<HTMLDivElement>(null);

  // Add-to-delivery card
  const [selectedIngredient, setSelectedIngredient] = useState<IngredientDto | null>(null);
  const [addQuantity, setAddQuantity] = useState("");
  const [addCost, setAddCost] = useState("");
  const [editingIndex, setEditingIndex] = useState<number | null>(null);

  // New ingredient inline form
  const [creatingNew, setCreatingNew] = useState(false);
  const [newName, setNewName] = useState("");
  const [newUnit, setNewUnit] = useState<UnitOfMeasure>(0);
  const [newThreshold, setNewThreshold] = useState("0");

  // Delivery list
  const [deliveryItems, setDeliveryItems] = useState<DeliveryItem[]>([]);

  const fetchIngredients = useCallback(async () => {
    if (!token) return;
    try {
      const data = await adminApi.getIngredients(token);
      setIngredients(data.filter((i) => i.active));
    } catch {
      toast.error("Failed to load ingredients");
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchIngredients();
  }, [fetchIngredients]);

  // Close dropdown on outside click
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (searchRef.current && !searchRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  const filteredIngredients = search.trim()
    ? ingredients.filter((i) =>
        i.name.toLowerCase().includes(search.toLowerCase())
      )
    : [];

  const hasExactMatch = filteredIngredients.some(
    (i) => i.name.toLowerCase() === search.trim().toLowerCase()
  );

  function selectIngredient(ing: IngredientDto) {
    setSelectedIngredient(ing);
    setSearch("");
    setShowDropdown(false);
    setAddQuantity("");
    setAddCost("");
    setCreatingNew(false);
    setEditingIndex(null);
  }

  function startCreateNew() {
    setCreatingNew(true);
    setNewName(search.trim());
    setSelectedIngredient(null);
    setShowDropdown(false);
    setAddQuantity("");
    setAddCost("");
    setEditingIndex(null);
  }

  function addToDelivery() {
    const qty = Number(addQuantity);
    const cost = Number(addCost);
    if (qty <= 0) return;

    const item: DeliveryItem = creatingNew
      ? {
          ingredientId: `new-${Date.now()}`,
          ingredientName: newName,
          unit: newUnit,
          currentStock: 0,
          quantity: qty,
          costPaid: cost || 0,
          isNew: true,
          newUnit: newUnit,
          newThreshold: Number(newThreshold) || 0,
        }
      : {
          ingredientId: selectedIngredient!.id,
          ingredientName: selectedIngredient!.name,
          unit: selectedIngredient!.unit,
          currentStock: selectedIngredient!.stockQuantity,
          quantity: qty,
          costPaid: cost || 0,
        };

    if (editingIndex !== null) {
      setDeliveryItems((prev) => prev.map((d, i) => (i === editingIndex ? item : d)));
    } else {
      setDeliveryItems((prev) => [...prev, item]);
    }

    // Reset
    setSelectedIngredient(null);
    setCreatingNew(false);
    setAddQuantity("");
    setAddCost("");
    setSearch("");
    setEditingIndex(null);
  }

  function editItem(index: number) {
    const item = deliveryItems[index];
    if (item.isNew) {
      setCreatingNew(true);
      setNewName(item.ingredientName);
      setNewUnit(item.unit);
      setNewThreshold(String(item.newThreshold ?? 0));
      setSelectedIngredient(null);
    } else {
      const ing = ingredients.find((i) => i.id === item.ingredientId);
      if (ing) setSelectedIngredient(ing);
      setCreatingNew(false);
    }
    setAddQuantity(String(item.quantity));
    setAddCost(String(item.costPaid || ""));
    setEditingIndex(index);
  }

  function removeItem(index: number) {
    setDeliveryItems((prev) => prev.filter((_, i) => i !== index));
  }

  async function handleSubmit() {
    if (!token || deliveryItems.length === 0) return;

    setSubmitting(true);
    try {
      // Create new ingredients first
      const newItems = deliveryItems.filter((d) => d.isNew);
      const idMap = new Map<string, string>();

      for (const item of newItems) {
        const created = await adminApi.createIngredient(token, {
          name: item.ingredientName,
          unit: item.newUnit!,
          costPerUnit: 0,
          stockQuantity: 0,
          lowStockThreshold: item.newThreshold ?? 0,
        });
        idMap.set(item.ingredientId, created.id);
      }

      // Build bulk restock request
      const items = deliveryItems.map((d) => ({
        ingredientId: d.isNew ? idMap.get(d.ingredientId)! : d.ingredientId,
        quantity: d.quantity,
        costPaid: d.costPaid,
      }));

      await adminApi.bulkRestock(token, { items });

      toast.success(
        `Delivery submitted: ${items.length} item${items.length !== 1 ? "s" : ""} restocked`
      );
      setDeliveryItems([]);
      fetchIngredients();
    } catch {
      toast.error("Failed to submit delivery");
    } finally {
      setSubmitting(false);
    }
  }

  const costPerUnit =
    Number(addQuantity) > 0 && Number(addCost) > 0
      ? Number(addCost) / Number(addQuantity)
      : null;

  const activeUnit = creatingNew
    ? newUnit
    : selectedIngredient?.unit;

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
        <Link
          href="/admin/ingredients"
          className="flex items-center gap-1 text-[#7a9bb5] hover:text-[#00e5ff] text-sm mb-2"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Ingredients
        </Link>
        <h1 className="text-2xl font-bold text-white">Check In Delivery</h1>
        <p className="text-[#7a9bb5] mt-1">
          Search for ingredients, enter quantities and costs, then submit
        </p>
      </div>

      {/* Search */}
      <div ref={searchRef} className="relative">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#7a9bb5]" />
          <Input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setShowDropdown(true);
            }}
            onFocus={() => setShowDropdown(true)}
            placeholder="Search ingredients..."
            className="bg-[#0d1f3c] border-[#1e3a5f] text-white placeholder:text-[#4a6a85] pl-10"
            data-testid="ingredient-search"
          />
        </div>

        {showDropdown && search.trim() && (
          <div className="absolute z-10 w-full mt-1 bg-[#0d1f3c] border border-[#1e3a5f] rounded-lg shadow-lg max-h-60 overflow-y-auto">
            {filteredIngredients.map((ing) => (
              <button
                key={ing.id}
                type="button"
                onClick={() => selectIngredient(ing)}
                className="w-full text-left px-4 py-3 hover:bg-[#1e3a5f]/50 flex justify-between items-center"
                data-testid={`search-result-${ing.id}`}
              >
                <span className="text-white">{ing.name}</span>
                <span className="text-sm text-[#7a9bb5]">
                  {ing.stockQuantity} {unitShortLabel(ing.unit)}
                </span>
              </button>
            ))}
            {!hasExactMatch && search.trim() && (
              <button
                type="button"
                onClick={startCreateNew}
                className="w-full text-left px-4 py-3 hover:bg-[#1e3a5f]/50 flex items-center gap-2 border-t border-[#1e3a5f]"
                data-testid="add-new-ingredient"
              >
                <Plus className="h-4 w-4 text-[#00e5ff]" />
                <span className="text-[#00e5ff]">
                  Add new ingredient: &quot;{search.trim()}&quot;
                </span>
              </button>
            )}
          </div>
        )}
      </div>

      {/* New ingredient inline form */}
      {creatingNew && (
        <div className="rounded-lg border border-[#1e3a5f] bg-[#0d1f3c] p-4 space-y-3" data-testid="new-ingredient-form">
          <h3 className="text-white font-medium">New Ingredient</h3>
          <div className="space-y-2">
            <Input
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Name"
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              data-testid="new-ingredient-name"
            />
            <div className="flex gap-2">
              <select
                value={newUnit}
                onChange={(e) => setNewUnit(Number(e.target.value) as UnitOfMeasure)}
                className="bg-[#0a1628] border border-[#1e3a5f] text-white rounded px-3 py-2 text-sm flex-1"
                data-testid="new-ingredient-unit"
              >
                {unitOptions.map((o) => (
                  <option key={o.value} value={o.value}>
                    {o.label}
                  </option>
                ))}
              </select>
              <Input
                type="number"
                min="0"
                value={newThreshold}
                onChange={(e) => setNewThreshold(e.target.value)}
                placeholder="Low stock threshold"
                className="bg-[#0a1628] border-[#1e3a5f] text-white w-32"
                data-testid="new-ingredient-threshold"
              />
            </div>
          </div>
        </div>
      )}

      {/* Add-to-delivery card */}
      {(selectedIngredient || creatingNew) && (
        <div className="rounded-lg border border-[#00e5ff]/30 bg-[#0d1f3c] p-4 space-y-4" data-testid="add-to-delivery-card">
          <div className="flex justify-between items-start">
            <div>
              <h3 className="text-white font-medium">
                {creatingNew ? newName : selectedIngredient!.name}
              </h3>
              {!creatingNew && (
                <p className="text-sm text-[#7a9bb5]">
                  Current stock: {selectedIngredient!.stockQuantity}{" "}
                  {unitShortLabel(selectedIngredient!.unit)}
                </p>
              )}
            </div>
            <button
              type="button"
              onClick={() => {
                setSelectedIngredient(null);
                setCreatingNew(false);
                setEditingIndex(null);
              }}
              className="text-[#7a9bb5] hover:text-white"
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          {/* Quantity */}
          <div className="space-y-2">
            <label className="text-sm text-[#7a9bb5]">
              Quantity ({activeUnit !== undefined ? unitShortLabel(activeUnit) : ""})
            </label>
            <Input
              type="number"
              min="0"
              step="0.01"
              value={addQuantity}
              onChange={(e) => setAddQuantity(e.target.value)}
              placeholder="0"
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              inputMode="decimal"
              data-testid="delivery-quantity"
            />
            {activeUnit !== undefined && (
              <QuantityCalculator
                unit={activeUnit}
                onCalculated={(total) => setAddQuantity(String(total))}
              />
            )}
          </div>

          {/* Cost paid */}
          <div className="space-y-2">
            <label className="text-sm text-[#7a9bb5]">Cost paid ($)</label>
            <Input
              type="number"
              min="0"
              step="0.01"
              value={addCost}
              onChange={(e) => setAddCost(e.target.value)}
              placeholder="0.00"
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              inputMode="decimal"
              data-testid="delivery-cost"
            />
            {costPerUnit !== null && (
              <p className="text-sm text-[#00e5ff]" data-testid="cost-per-unit">
                ${costPerUnit.toFixed(4)} per{" "}
                {activeUnit !== undefined ? unitShortLabel(activeUnit) : "unit"}
              </p>
            )}
          </div>

          {/* Add button */}
          <Button
            type="button"
            onClick={addToDelivery}
            disabled={Number(addQuantity) <= 0}
            className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
            data-testid="add-to-delivery-btn"
          >
            {editingIndex !== null ? "Update in Delivery" : "Add to Delivery"}
          </Button>
        </div>
      )}

      {/* Delivery list */}
      {deliveryItems.length > 0 && (
        <div className="space-y-2" data-testid="delivery-list">
          <h2 className="text-lg font-semibold text-white">
            Delivery ({deliveryItems.length} item{deliveryItems.length !== 1 ? "s" : ""})
          </h2>
          {deliveryItems.map((item, index) => (
            <div
              key={`${item.ingredientId}-${index}`}
              className="rounded-lg border border-white/10 bg-[#0d1f3c] p-3 flex items-center gap-3"
              data-testid={`delivery-item-${index}`}
            >
              <div className="flex-1 min-w-0">
                <p className="font-medium text-white truncate">
                  {item.ingredientName}
                  {item.isNew && (
                    <span className="text-xs text-[#00e5ff] ml-2">(new)</span>
                  )}
                </p>
                <p className="text-sm text-[#7a9bb5]">
                  {item.quantity} {unitShortLabel(item.unit)}
                  {item.costPaid > 0 && ` · $${item.costPaid.toFixed(2)}`}
                  {item.costPaid > 0 && item.quantity > 0 && (
                    <span className="text-[#00e5ff]">
                      {" "}
                      (${(item.costPaid / item.quantity).toFixed(4)}/
                      {unitShortLabel(item.unit)})
                    </span>
                  )}
                </p>
              </div>
              <button
                type="button"
                onClick={() => editItem(index)}
                className="text-[#7a9bb5] hover:text-[#00e5ff] p-1"
                aria-label={`Edit ${item.ingredientName}`}
              >
                <Pencil className="h-4 w-4" />
              </button>
              <button
                type="button"
                onClick={() => removeItem(index)}
                className="text-[#7a9bb5] hover:text-[#ff4757] p-1"
                aria-label={`Remove ${item.ingredientName}`}
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Submit */}
      <div className="sticky bottom-0 bg-[#0f1f35]/95 backdrop-blur-sm border-t border-[#1e3a5f] pt-4 pb-2 -mx-4 px-4">
        <Button
          onClick={handleSubmit}
          disabled={submitting || deliveryItems.length === 0}
          className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
          data-testid="submit-delivery-btn"
        >
          {submitting ? (
            <Loader2 className="h-4 w-4 animate-spin mr-2" />
          ) : (
            <PackagePlus className="h-4 w-4 mr-2" />
          )}
          Submit Delivery
        </Button>
      </div>
    </div>
  );
}
