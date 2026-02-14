"use client";

import { useState, useEffect, useCallback } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
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
import { Loader2, Plus, Pencil, Trash2, Search } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { unitLabel } from "@/lib/unit-options";
import { IngredientFormSheet } from "@/components/admin/ingredient-form-sheet";
import type { IngredientDto } from "@/types/api";

export default function IngredientsPage() {
  const { token } = useAuth();
  const [ingredients, setIngredients] = useState<IngredientDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  // Sheet state
  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingIngredient, setEditingIngredient] =
    useState<IngredientDto | null>(null);

  // Delete dialog state
  const [deleteTarget, setDeleteTarget] = useState<IngredientDto | null>(null);

  const fetchIngredients = useCallback(async () => {
    if (!token) return;
    try {
      const data = await adminApi.getIngredients(token);
      setIngredients(data);
    } catch (err) {
      toast.error("Failed to load ingredients");
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchIngredients();
  }, [fetchIngredients]);

  const filtered = ingredients.filter((i) =>
    i.name.toLowerCase().includes(search.toLowerCase())
  );

  function handleAdd() {
    setEditingIngredient(null);
    setSheetOpen(true);
  }

  function handleEdit(ingredient: IngredientDto) {
    setEditingIngredient(ingredient);
    setSheetOpen(true);
  }

  async function handleDelete() {
    if (!token || !deleteTarget) return;
    try {
      await adminApi.deleteIngredient(token, deleteTarget.id);
      toast.success("Ingredient deactivated");
      setDeleteTarget(null);
      fetchIngredients();
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to delete ingredient"
      );
    }
  }

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
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Ingredients</h1>
          <p className="text-[#7a9bb5] mt-1">
            {ingredients.length} ingredient{ingredients.length !== 1 && "s"}
          </p>
        </div>
        <Button
          onClick={handleAdd}
          className="bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Ingredient
        </Button>
      </div>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#7a9bb5]" />
        <Input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search ingredients..."
          className="pl-10 bg-[#0d1f3c] border-[#1e3a5f] text-white placeholder:text-[#4a6a85]"
        />
      </div>

      {/* Table */}
      <div className="rounded-lg border border-white/10 overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="border-white/10 hover:bg-transparent">
              <TableHead className="text-[#7a9bb5]">Name</TableHead>
              <TableHead className="text-[#7a9bb5]">Unit</TableHead>
              <TableHead className="text-[#7a9bb5] text-right">
                Cost/Unit
              </TableHead>
              <TableHead className="text-[#7a9bb5] text-right">
                Stock
              </TableHead>
              <TableHead className="text-[#7a9bb5] text-right">
                Threshold
              </TableHead>
              <TableHead className="text-[#7a9bb5]">Status</TableHead>
              <TableHead className="text-[#7a9bb5] text-right">
                Actions
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((ing) => {
              const isLowStock = ing.stockQuantity < ing.lowStockThreshold;
              return (
                <TableRow
                  key={ing.id}
                  className="border-white/10 hover:bg-white/5"
                >
                  <TableCell className="font-medium text-white">
                    {ing.name}
                  </TableCell>
                  <TableCell className="text-[#7a9bb5]">
                    {unitLabel(ing.unit)}
                  </TableCell>
                  <TableCell className="text-right text-[#7a9bb5]">
                    ${ing.costPerUnit.toFixed(4)}
                  </TableCell>
                  <TableCell
                    className={`text-right font-medium ${
                      isLowStock ? "text-[#ff4757]" : "text-white"
                    }`}
                  >
                    {ing.stockQuantity}
                    {isLowStock && (
                      <span className="ml-1 text-xs text-[#ff4757]">LOW</span>
                    )}
                  </TableCell>
                  <TableCell className="text-right text-[#7a9bb5]">
                    {ing.lowStockThreshold}
                  </TableCell>
                  <TableCell>
                    {ing.active ? (
                      <Badge className="bg-emerald-500/10 text-emerald-400 border-emerald-500/20 hover:bg-emerald-500/10">
                        Active
                      </Badge>
                    ) : (
                      <Badge className="bg-gray-500/10 text-gray-400 border-gray-500/20 hover:bg-gray-500/10">
                        Inactive
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleEdit(ing)}
                        className="h-8 w-8 text-[#7a9bb5] hover:text-[#00e5ff] hover:bg-[#00e5ff]/10"
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setDeleteTarget(ing)}
                        className="h-8 w-8 text-[#7a9bb5] hover:text-[#ff4757] hover:bg-[#ff4757]/10"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              );
            })}
            {filtered.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={7}
                  className="text-center text-[#7a9bb5] py-8"
                >
                  {search
                    ? "No ingredients match your search."
                    : "No ingredients yet."}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Form Sheet */}
      <IngredientFormSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        ingredient={editingIngredient}
        onSaved={fetchIngredients}
      />

      {/* Delete Confirmation */}
      <AlertDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent className="bg-[#0d1f3c] border-white/10">
          <AlertDialogHeader>
            <AlertDialogTitle className="text-white">
              Deactivate Ingredient
            </AlertDialogTitle>
            <AlertDialogDescription className="text-[#7a9bb5]">
              Are you sure you want to deactivate &quot;{deleteTarget?.name}
              &quot;? It will be hidden from menus but can be reactivated later.
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
