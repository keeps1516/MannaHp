"use client";

import { useState, useEffect, useCallback } from "react";
import { Button } from "@/components/ui/button";
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
import { Loader2, Plus, Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { CategoryFormSheet } from "@/components/admin/category-form-sheet";
import type { CategoryDto } from "@/types/api";

export function CategoryList() {
  const { token } = useAuth();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);

  // Sheet state
  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<CategoryDto | null>(
    null
  );

  // Delete dialog state
  const [deleteTarget, setDeleteTarget] = useState<CategoryDto | null>(null);

  const fetchCategories = useCallback(async () => {
    if (!token) return;
    try {
      const data = await adminApi.getCategories(token);
      setCategories(data);
    } catch {
      toast.error("Failed to load categories");
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchCategories();
  }, [fetchCategories]);

  function handleAdd() {
    setEditingCategory(null);
    setSheetOpen(true);
  }

  function handleEdit(cat: CategoryDto) {
    setEditingCategory(cat);
    setSheetOpen(true);
  }

  async function handleDelete() {
    if (!token || !deleteTarget) return;
    try {
      await adminApi.deleteCategory(token, deleteTarget.id);
      toast.success("Category deactivated");
      setDeleteTarget(null);
      fetchCategories();
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to delete category"
      );
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[30vh]">
        <Loader2 className="h-8 w-8 animate-spin text-[#00e5ff]" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <p className="text-[#7a9bb5] text-sm">
          {categories.length} categor{categories.length !== 1 ? "ies" : "y"}
        </p>
        <Button
          onClick={handleAdd}
          size="sm"
          className="bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Category
        </Button>
      </div>

      {/* Table */}
      <div className="rounded-lg border border-white/10 overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="border-white/10 hover:bg-transparent">
              <TableHead className="text-[#7a9bb5]">Name</TableHead>
              <TableHead className="text-[#7a9bb5] text-right">
                Sort Order
              </TableHead>
              <TableHead className="text-[#7a9bb5]">Status</TableHead>
              <TableHead className="text-[#7a9bb5] text-right">
                Actions
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {categories.map((cat) => (
              <TableRow
                key={cat.id}
                className="border-white/10 hover:bg-white/5"
              >
                <TableCell className="font-medium text-white">
                  {cat.name}
                </TableCell>
                <TableCell className="text-right text-[#7a9bb5]">
                  {cat.sortOrder}
                </TableCell>
                <TableCell>
                  {cat.active ? (
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
                      onClick={() => handleEdit(cat)}
                      className="h-8 w-8 text-[#7a9bb5] hover:text-[#00e5ff] hover:bg-[#00e5ff]/10"
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => setDeleteTarget(cat)}
                      className="h-8 w-8 text-[#7a9bb5] hover:text-[#ff4757] hover:bg-[#ff4757]/10"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {categories.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={4}
                  className="text-center text-[#7a9bb5] py-8"
                >
                  No categories yet. Add your first category to get started.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Form Sheet */}
      <CategoryFormSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        category={editingCategory}
        onSaved={fetchCategories}
      />

      {/* Delete Confirmation */}
      <AlertDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent className="bg-[#0d1f3c] border-white/10">
          <AlertDialogHeader>
            <AlertDialogTitle className="text-white">
              Deactivate Category
            </AlertDialogTitle>
            <AlertDialogDescription className="text-[#7a9bb5]">
              Are you sure you want to deactivate &quot;{deleteTarget?.name}
              &quot;? Menu items in this category will still exist but the
              category will be hidden.
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
