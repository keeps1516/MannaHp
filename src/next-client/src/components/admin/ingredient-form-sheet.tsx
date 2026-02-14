"use client";

import { useState, useEffect } from "react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { unitOptions } from "@/lib/unit-options";
import type { IngredientDto, UnitOfMeasure } from "@/types/api";

interface IngredientFormSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  ingredient: IngredientDto | null; // null = create mode
  onSaved: () => void;
}

export function IngredientFormSheet({
  open,
  onOpenChange,
  ingredient,
  onSaved,
}: IngredientFormSheetProps) {
  const { token } = useAuth();
  const [saving, setSaving] = useState(false);

  const [name, setName] = useState("");
  const [unit, setUnit] = useState<string>("0");
  const [costPerUnit, setCostPerUnit] = useState("");
  const [stockQuantity, setStockQuantity] = useState("");
  const [lowStockThreshold, setLowStockThreshold] = useState("");
  const [active, setActive] = useState(true);

  const isEdit = ingredient !== null;

  useEffect(() => {
    if (ingredient) {
      setName(ingredient.name);
      setUnit(String(ingredient.unit));
      setCostPerUnit(String(ingredient.costPerUnit));
      setStockQuantity(String(ingredient.stockQuantity));
      setLowStockThreshold(String(ingredient.lowStockThreshold));
      setActive(ingredient.active);
    } else {
      setName("");
      setUnit("0");
      setCostPerUnit("");
      setStockQuantity("");
      setLowStockThreshold("");
      setActive(true);
    }
  }, [ingredient, open]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setSaving(true);

    try {
      const data = {
        name,
        unit: Number(unit) as UnitOfMeasure,
        costPerUnit: Number(costPerUnit),
        stockQuantity: Number(stockQuantity),
        lowStockThreshold: Number(lowStockThreshold),
      };

      if (isEdit) {
        await adminApi.updateIngredient(token, ingredient.id, {
          ...data,
          active,
        });
        toast.success("Ingredient updated");
      } else {
        await adminApi.createIngredient(token, data);
        toast.success("Ingredient created");
      }

      onSaved();
      onOpenChange(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="bg-[#0d1f3c] border-white/10 overflow-y-auto">
        <SheetHeader>
          <SheetTitle className="text-white">
            {isEdit ? "Edit Ingredient" : "Add Ingredient"}
          </SheetTitle>
        </SheetHeader>

        <form onSubmit={handleSubmit} className="space-y-5 mt-6 px-1">
          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Name</Label>
            <Input
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="e.g. Chicken"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Unit of Measure</Label>
            <Select value={unit} onValueChange={setUnit}>
              <SelectTrigger className="bg-[#0a1628] border-[#1e3a5f] text-white">
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="bg-[#0d1f3c] border-[#1e3a5f]">
                {unitOptions.map((opt) => (
                  <SelectItem
                    key={opt.value}
                    value={String(opt.value)}
                    className="text-white focus:bg-[#00e5ff]/10 focus:text-[#00e5ff]"
                  >
                    {opt.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Cost Per Unit ($)</Label>
            <Input
              type="number"
              step="0.0001"
              min="0"
              value={costPerUnit}
              onChange={(e) => setCostPerUnit(e.target.value)}
              required
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="0.00"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Stock Quantity</Label>
            <Input
              type="number"
              step="0.01"
              min="0"
              value={stockQuantity}
              onChange={(e) => setStockQuantity(e.target.value)}
              required
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="100"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Low Stock Threshold</Label>
            <Input
              type="number"
              step="0.01"
              min="0"
              value={lowStockThreshold}
              onChange={(e) => setLowStockThreshold(e.target.value)}
              required
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="10"
            />
          </div>

          {isEdit && (
            <div className="flex items-center space-x-2">
              <Checkbox
                id="active"
                checked={active}
                onCheckedChange={(checked) => setActive(checked === true)}
              />
              <Label htmlFor="active" className="text-[#7a9bb5]">
                Active
              </Label>
            </div>
          )}

          <Button
            type="submit"
            disabled={saving}
            className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
          >
            {saving && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
            {isEdit ? "Update Ingredient" : "Create Ingredient"}
          </Button>
        </form>
      </SheetContent>
    </Sheet>
  );
}
