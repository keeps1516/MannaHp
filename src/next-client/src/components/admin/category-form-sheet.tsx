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
import type { CategoryDto } from "@/types/api";

interface CategoryFormSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  category: CategoryDto | null; // null = create mode
  onSaved: () => void;
}

export function CategoryFormSheet({
  open,
  onOpenChange,
  category,
  onSaved,
}: CategoryFormSheetProps) {
  const { token } = useAuth();
  const [saving, setSaving] = useState(false);

  const [name, setName] = useState("");
  const [sortOrder, setSortOrder] = useState("");
  const [active, setActive] = useState(true);

  const isEdit = category !== null;

  useEffect(() => {
    if (category) {
      setName(category.name);
      setSortOrder(String(category.sortOrder));
      setActive(category.active);
    } else {
      setName("");
      setSortOrder("0");
      setActive(true);
    }
  }, [category, open]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setSaving(true);

    try {
      const data = {
        name,
        sortOrder: Number(sortOrder),
      };

      if (isEdit) {
        await adminApi.updateCategory(token, category.id, {
          ...data,
          active,
        });
        toast.success("Category updated");
      } else {
        await adminApi.createCategory(token, data);
        toast.success("Category created");
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
            {isEdit ? "Edit Category" : "Add Category"}
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
              placeholder="e.g. Bowls"
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
                id="cat-active"
                checked={active}
                onCheckedChange={(checked) => setActive(checked === true)}
              />
              <Label htmlFor="cat-active" className="text-[#7a9bb5]">
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
            {isEdit ? "Update Category" : "Create Category"}
          </Button>
        </form>
      </SheetContent>
    </Sheet>
  );
}
