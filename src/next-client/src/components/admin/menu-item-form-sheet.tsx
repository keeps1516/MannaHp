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
import { Textarea } from "@/components/ui/textarea";
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
import type { MenuItemDto, CategoryDto } from "@/types/api";

interface MenuItemFormSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  menuItem: MenuItemDto | null; // null = create mode
  categories: CategoryDto[];
  onSaved: () => void;
}

export function MenuItemFormSheet({
  open,
  onOpenChange,
  menuItem,
  categories,
  onSaved,
}: MenuItemFormSheetProps) {
  const { token } = useAuth();
  const [saving, setSaving] = useState(false);

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [isCustomizable, setIsCustomizable] = useState(false);
  const [sortOrder, setSortOrder] = useState("");
  const [active, setActive] = useState(true);

  const isEdit = menuItem !== null;

  useEffect(() => {
    if (menuItem) {
      setName(menuItem.name);
      setDescription(menuItem.description ?? "");
      setCategoryId(menuItem.categoryId);
      setIsCustomizable(menuItem.isCustomizable);
      setSortOrder(String(menuItem.sortOrder));
      setActive(menuItem.active);
    } else {
      setName("");
      setDescription("");
      setCategoryId(categories[0]?.id ?? "");
      setIsCustomizable(false);
      setSortOrder("0");
      setActive(true);
    }
  }, [menuItem, open, categories]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setSaving(true);

    try {
      if (isEdit) {
        await adminApi.updateMenuItem(token, menuItem.id, {
          name,
          description: description || null,
          imageUrl: menuItem.imageUrl,
          imageApproximate: menuItem.imageApproximate,
          isCustomizable,
          categoryId,
          sortOrder: Number(sortOrder),
          active,
        });
        toast.success("Menu item updated");
      } else {
        await adminApi.createMenuItem(token, {
          name,
          description: description || null,
          imageUrl: null,
          imageApproximate: false,
          isCustomizable,
          categoryId,
          sortOrder: Number(sortOrder),
        });
        toast.success("Menu item created");
      }

      onSaved();
      onOpenChange(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  }

  const activeCategories = categories.filter((c) => c.active);

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="bg-[#0d1f3c] border-white/10 overflow-y-auto">
        <SheetHeader>
          <SheetTitle className="text-white">
            {isEdit ? "Edit Menu Item" : "Add Menu Item"}
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
              placeholder="e.g. Burrito Bowl"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Description</Label>
            <Textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="bg-[#0a1628] border-[#1e3a5f] text-white min-h-[80px] resize-none"
              placeholder="Optional description..."
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[#7a9bb5]">Category</Label>
            <Select value={categoryId} onValueChange={setCategoryId}>
              <SelectTrigger className="bg-[#0a1628] border-[#1e3a5f] text-white">
                <SelectValue placeholder="Select a category" />
              </SelectTrigger>
              <SelectContent className="bg-[#0d1f3c] border-[#1e3a5f]">
                {activeCategories.map((cat) => (
                  <SelectItem
                    key={cat.id}
                    value={cat.id}
                    className="text-white focus:bg-[#00e5ff]/10 focus:text-[#00e5ff]"
                  >
                    {cat.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
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

          <div className="flex items-center space-x-2">
            <Checkbox
              id="customizable"
              checked={isCustomizable}
              onCheckedChange={(checked) => setIsCustomizable(checked === true)}
            />
            <Label htmlFor="customizable" className="text-[#7a9bb5]">
              Customizable (ingredient selection)
            </Label>
          </div>

          {isEdit && (
            <div className="flex items-center space-x-2">
              <Checkbox
                id="mi-active"
                checked={active}
                onCheckedChange={(checked) => setActive(checked === true)}
              />
              <Label htmlFor="mi-active" className="text-[#7a9bb5]">
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
            {isEdit ? "Update Menu Item" : "Create Menu Item"}
          </Button>
        </form>
      </SheetContent>
    </Sheet>
  );
}
