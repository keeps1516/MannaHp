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
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import type { MenuItemVariantDto } from "@/types/api";

interface VariantFormSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  menuItemId: string;
  variant: MenuItemVariantDto | null; // null = create mode
  onSaved: () => void;
}

export function VariantFormSheet({
  open,
  onOpenChange,
  menuItemId,
  variant,
  onSaved,
}: VariantFormSheetProps) {
  const { token } = useAuth();
  const [saving, setSaving] = useState(false);

  const [name, setName] = useState("");
  const [price, setPrice] = useState("");
  const [sortOrder, setSortOrder] = useState("");
  const [active, setActive] = useState(true);

  const isEdit = variant !== null;

  useEffect(() => {
    if (variant) {
      setName(variant.name);
      setPrice(String(variant.price));
      setSortOrder(String(variant.sortOrder));
      setActive(variant.active);
    } else {
      setName("");
      setPrice("");
      setSortOrder("0");
      setActive(true);
    }
  }, [variant, open]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setSaving(true);

    try {
      const data = {
        name,
        price: Number(price),
        sortOrder: Number(sortOrder),
      };

      if (isEdit) {
        await adminApi.updateVariant(token, menuItemId, variant.id, {
          ...data,
          active,
        });
        toast.success("Variant updated");
      } else {
        await adminApi.createVariant(token, menuItemId, data);
        toast.success("Variant created");
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
            {isEdit ? "Edit Variant" : "Add Variant"}
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
              placeholder="e.g. Large (16oz)"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Price ($)</Label>
            <Input
              type="number"
              step="0.01"
              min="0"
              value={price}
              onChange={(e) => setPrice(e.target.value)}
              required
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="0.00"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Sort Order</Label>
            <Input
              type="number"
              min="0"
              value={sortOrder}
              onChange={(e) => setSortOrder(e.target.value)}
              required
              className="bg-[#0a1628] border-[#1e3a5f] text-white"
              placeholder="0"
            />
          </div>

          {isEdit && (
            <div className="flex items-center space-x-2">
              <Checkbox
                id="var-active"
                checked={active}
                onCheckedChange={(checked) => setActive(checked === true)}
              />
              <Label htmlFor="var-active" className="text-[#7a9bb5]">
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
            {isEdit ? "Update Variant" : "Create Variant"}
          </Button>
        </form>
      </SheetContent>
    </Sheet>
  );
}
