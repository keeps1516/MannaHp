"use client";

import { useState, useEffect, useCallback } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Plus, Pencil, Trash2, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import type { AvailableIngredientDto, IngredientDto } from "@/types/api";

interface AvailableIngredientSectionProps {
  menuItemId: string;
  availableIngredients: AvailableIngredientDto[];
  onRefresh: () => void;
}

export function AvailableIngredientSection({
  menuItemId,
  availableIngredients,
  onRefresh,
}: AvailableIngredientSectionProps) {
  const { token } = useAuth();

  // All ingredients for the dropdown
  const [allIngredients, setAllIngredients] = useState<IngredientDto[]>([]);
  const [ingredientsLoaded, setIngredientsLoaded] = useState(false);

  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingItem, setEditingItem] =
    useState<AvailableIngredientDto | null>(null);
  const [deleteTarget, setDeleteTarget] =
    useState<AvailableIngredientDto | null>(null);

  // Form fields
  const [ingredientId, setIngredientId] = useState("");
  const [customerPrice, setCustomerPrice] = useState("");
  const [quantityUsed, setQuantityUsed] = useState("");
  const [isDefault, setIsDefault] = useState(false);
  const [groupName, setGroupName] = useState("");
  const [sortOrder, setSortOrder] = useState("");
  const [active, setActive] = useState(true);
  const [saving, setSaving] = useState(false);

  const fetchIngredients = useCallback(async () => {
    if (!token || ingredientsLoaded) return;
    try {
      const data = await adminApi.getIngredients(token);
      setAllIngredients(data.filter((i) => i.active));
      setIngredientsLoaded(true);
    } catch {
      // silent
    }
  }, [token, ingredientsLoaded]);

  // Load ingredients when the sheet opens
  useEffect(() => {
    if (sheetOpen) fetchIngredients();
  }, [sheetOpen, fetchIngredients]);

  const isEdit = editingItem !== null;

  useEffect(() => {
    if (editingItem) {
      setIngredientId(editingItem.ingredientId);
      setCustomerPrice(String(editingItem.customerPrice));
      setQuantityUsed(String(editingItem.quantityUsed));
      setIsDefault(editingItem.isDefault);
      setGroupName(editingItem.groupName);
      setSortOrder(String(editingItem.sortOrder));
      setActive(editingItem.active);
    } else {
      setIngredientId("");
      setCustomerPrice("");
      setQuantityUsed("1");
      setIsDefault(false);
      setGroupName("");
      setSortOrder("0");
      setActive(true);
    }
  }, [editingItem, sheetOpen]);

  function handleAdd() {
    setEditingItem(null);
    setSheetOpen(true);
  }

  function handleEdit(ai: AvailableIngredientDto) {
    setEditingItem(ai);
    setSheetOpen(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!token) return;
    setSaving(true);

    try {
      const data = {
        customerPrice: Number(customerPrice),
        quantityUsed: Number(quantityUsed),
        isDefault,
        groupName,
        sortOrder: Number(sortOrder),
      };

      if (isEdit) {
        await adminApi.updateAvailableIngredient(
          token,
          menuItemId,
          editingItem.id,
          { ...data, active }
        );
        toast.success("Available ingredient updated");
      } else {
        await adminApi.createAvailableIngredient(token, menuItemId, {
          ...data,
          ingredientId,
        });
        toast.success("Available ingredient added");
      }

      onRefresh();
      setSheetOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Failed to save");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!token || !deleteTarget) return;
    try {
      await adminApi.deleteAvailableIngredient(
        token,
        menuItemId,
        deleteTarget.id
      );
      toast.success("Available ingredient deactivated");
      setDeleteTarget(null);
      onRefresh();
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : "Failed to delete available ingredient"
      );
    }
  }

  // Group available ingredients by groupName
  const grouped = availableIngredients.reduce<
    Record<string, AvailableIngredientDto[]>
  >((acc, ai) => {
    const key = ai.groupName || "Ungrouped";
    if (!acc[key]) acc[key] = [];
    acc[key].push(ai);
    return acc;
  }, {});

  const groupNames = Object.keys(grouped).sort();

  // Existing unique group names for suggestions
  const existingGroups = [
    ...new Set(availableIngredients.map((ai) => ai.groupName).filter(Boolean)),
  ];

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-semibold text-emerald-400">
          Available Ingredients
        </h4>
        <Button
          onClick={handleAdd}
          size="sm"
          variant="ghost"
          className="h-7 text-xs text-emerald-400 hover:bg-emerald-400/10"
        >
          <Plus className="h-3 w-3 mr-1" />
          Add
        </Button>
      </div>

      {availableIngredients.length === 0 ? (
        <p className="text-xs text-[#4a6a85] text-center py-4">
          No ingredients configured. Add ingredients customers can select.
        </p>
      ) : (
        <div className="space-y-4">
          {groupNames.map((group) => (
            <div key={group}>
              <p className="text-xs font-medium text-[#7a9bb5] uppercase tracking-wider mb-2">
                {group}
              </p>
              <div className="rounded-md border border-white/5 overflow-hidden">
                <Table>
                  <TableHeader>
                    <TableRow className="border-white/5 hover:bg-transparent">
                      <TableHead className="text-[#7a9bb5] text-xs h-8">
                        Ingredient
                      </TableHead>
                      <TableHead className="text-[#7a9bb5] text-xs text-right h-8">
                        Price
                      </TableHead>
                      <TableHead className="text-[#7a9bb5] text-xs text-center h-8">
                        Default
                      </TableHead>
                      <TableHead className="text-[#7a9bb5] text-xs h-8">
                        Status
                      </TableHead>
                      <TableHead className="text-[#7a9bb5] text-xs text-right h-8">
                        Actions
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {grouped[group]
                      .sort((a, b) => a.sortOrder - b.sortOrder)
                      .map((ai) => (
                        <TableRow
                          key={ai.id}
                          className="border-white/5 hover:bg-white/5"
                        >
                          <TableCell className="text-white text-sm py-2">
                            {ai.ingredientName}
                          </TableCell>
                          <TableCell className="text-right text-[#7a9bb5] text-sm py-2">
                            ${ai.customerPrice.toFixed(2)}
                          </TableCell>
                          <TableCell className="text-center py-2">
                            {ai.isDefault && (
                              <Badge className="bg-[#00e5ff]/10 text-[#00e5ff] border-[#00e5ff]/20 text-xs hover:bg-[#00e5ff]/10">
                                Default
                              </Badge>
                            )}
                          </TableCell>
                          <TableCell className="py-2">
                            {ai.active ? (
                              <Badge className="bg-emerald-500/10 text-emerald-400 border-emerald-500/20 text-xs hover:bg-emerald-500/10">
                                Active
                              </Badge>
                            ) : (
                              <Badge className="bg-gray-500/10 text-gray-400 border-gray-500/20 text-xs hover:bg-gray-500/10">
                                Inactive
                              </Badge>
                            )}
                          </TableCell>
                          <TableCell className="text-right py-2">
                            <div className="flex items-center justify-end gap-1">
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleEdit(ai)}
                                className="h-7 w-7 text-[#7a9bb5] hover:text-[#00e5ff] hover:bg-[#00e5ff]/10"
                              >
                                <Pencil className="h-3 w-3" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => setDeleteTarget(ai)}
                                className="h-7 w-7 text-[#7a9bb5] hover:text-[#ff4757] hover:bg-[#ff4757]/10"
                              >
                                <Trash2 className="h-3 w-3" />
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Add/Edit Sheet */}
      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent className="bg-[#0d1f3c] border-white/10 overflow-y-auto">
          <SheetHeader>
            <SheetTitle className="text-white">
              {isEdit ? "Edit Available Ingredient" : "Add Available Ingredient"}
            </SheetTitle>
          </SheetHeader>

          <form onSubmit={handleSubmit} className="space-y-5 mt-6 px-1">
            {!isEdit && (
              <div className="space-y-2">
                <Label className="text-[#7a9bb5]">Ingredient</Label>
                <Select value={ingredientId} onValueChange={setIngredientId}>
                  <SelectTrigger className="bg-[#0a1628] border-[#1e3a5f] text-white">
                    <SelectValue placeholder="Select an ingredient" />
                  </SelectTrigger>
                  <SelectContent className="bg-[#0d1f3c] border-[#1e3a5f] max-h-[200px]">
                    {allIngredients.map((ing) => (
                      <SelectItem
                        key={ing.id}
                        value={ing.id}
                        className="text-white focus:bg-[#00e5ff]/10 focus:text-[#00e5ff]"
                      >
                        {ing.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {isEdit && (
              <div className="space-y-2">
                <Label className="text-[#7a9bb5]">Ingredient</Label>
                <Input
                  value={editingItem.ingredientName}
                  disabled
                  className="bg-[#0a1628] border-[#1e3a5f] text-[#7a9bb5]"
                />
              </div>
            )}

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Customer Price ($)</Label>
              <Input
                type="number"
                step="0.01"
                min="0"
                value={customerPrice}
                onChange={(e) => setCustomerPrice(e.target.value)}
                required
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
                placeholder="0.00"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Quantity Used</Label>
              <Input
                type="number"
                step="0.01"
                min="0"
                value={quantityUsed}
                onChange={(e) => setQuantityUsed(e.target.value)}
                required
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
                placeholder="1"
              />
            </div>

            <div className="space-y-2">
              <Label className="text-[#7a9bb5]">Group Name</Label>
              <Input
                value={groupName}
                onChange={(e) => setGroupName(e.target.value)}
                required
                className="bg-[#0a1628] border-[#1e3a5f] text-white"
                placeholder="e.g. Protein, Rice, Toppings"
                list="group-suggestions"
              />
              {existingGroups.length > 0 && (
                <datalist id="group-suggestions">
                  {existingGroups.map((g) => (
                    <option key={g} value={g} />
                  ))}
                </datalist>
              )}
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
                id="ai-default"
                checked={isDefault}
                onCheckedChange={(checked) => setIsDefault(checked === true)}
              />
              <Label htmlFor="ai-default" className="text-[#7a9bb5]">
                Pre-selected by default
              </Label>
            </div>

            {isEdit && (
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="ai-active"
                  checked={active}
                  onCheckedChange={(checked) => setActive(checked === true)}
                />
                <Label htmlFor="ai-active" className="text-[#7a9bb5]">
                  Active
                </Label>
              </div>
            )}

            <Button
              type="submit"
              disabled={saving || (!isEdit && !ingredientId)}
              className="w-full bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
            >
              {saving && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
              {isEdit ? "Update" : "Add Ingredient"}
            </Button>
          </form>
        </SheetContent>
      </Sheet>

      {/* Delete Confirmation */}
      <AlertDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent className="bg-[#0d1f3c] border-white/10">
          <AlertDialogHeader>
            <AlertDialogTitle className="text-white">
              Deactivate Available Ingredient
            </AlertDialogTitle>
            <AlertDialogDescription className="text-[#7a9bb5]">
              Are you sure you want to deactivate &quot;
              {deleteTarget?.ingredientName}&quot; from this menu item?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="bg-transparent border-[#1e3a5f] text-[#7a9bb5] hover:bg-white/5 hover:text-white">
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="bg-[#ff4757] text-white hover:bg-[#ff4757]/80"
            >
              Deactivate
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
