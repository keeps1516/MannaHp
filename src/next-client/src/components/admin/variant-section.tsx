"use client";

import { useState } from "react";
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
import { Plus, Pencil, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { VariantFormSheet } from "@/components/admin/variant-form-sheet";
import type { MenuItemVariantDto } from "@/types/api";

interface VariantSectionProps {
  menuItemId: string;
  variants: MenuItemVariantDto[];
  onRefresh: () => void;
}

export function VariantSection({
  menuItemId,
  variants,
  onRefresh,
}: VariantSectionProps) {
  const { token } = useAuth();

  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingVariant, setEditingVariant] =
    useState<MenuItemVariantDto | null>(null);
  const [deleteTarget, setDeleteTarget] =
    useState<MenuItemVariantDto | null>(null);

  function handleAdd() {
    setEditingVariant(null);
    setSheetOpen(true);
  }

  function handleEdit(v: MenuItemVariantDto) {
    setEditingVariant(v);
    setSheetOpen(true);
  }

  async function handleDelete() {
    if (!token || !deleteTarget) return;
    try {
      await adminApi.deleteVariant(token, menuItemId, deleteTarget.id);
      toast.success("Variant deactivated");
      setDeleteTarget(null);
      onRefresh();
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to delete variant"
      );
    }
  }

  const sorted = [...variants].sort((a, b) => a.sortOrder - b.sortOrder);

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-semibold text-[#00e5ff]">Variants</h4>
        <Button
          onClick={handleAdd}
          size="sm"
          variant="ghost"
          className="h-7 text-xs text-[#00e5ff] hover:bg-[#00e5ff]/10"
        >
          <Plus className="h-3 w-3 mr-1" />
          Add
        </Button>
      </div>

      {sorted.length === 0 ? (
        <p className="text-xs text-[#4a6a85] text-center py-4">
          No variants yet. Add at least one variant (e.g. &quot;Regular&quot;).
        </p>
      ) : (
        <div className="rounded-md border border-white/5 overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow className="border-white/5 hover:bg-transparent">
                <TableHead className="text-[#7a9bb5] text-xs h-8">
                  Name
                </TableHead>
                <TableHead className="text-[#7a9bb5] text-xs text-right h-8">
                  Price
                </TableHead>
                <TableHead className="text-[#7a9bb5] text-xs text-right h-8">
                  Order
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
              {sorted.map((v) => (
                <TableRow
                  key={v.id}
                  className="border-white/5 hover:bg-white/5"
                >
                  <TableCell className="text-white text-sm py-2">
                    {v.name}
                  </TableCell>
                  <TableCell className="text-right text-[#7a9bb5] text-sm py-2">
                    ${v.price.toFixed(2)}
                  </TableCell>
                  <TableCell className="text-right text-[#7a9bb5] text-sm py-2">
                    {v.sortOrder}
                  </TableCell>
                  <TableCell className="py-2">
                    {v.active ? (
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
                        onClick={() => handleEdit(v)}
                        className="h-7 w-7 text-[#7a9bb5] hover:text-[#00e5ff] hover:bg-[#00e5ff]/10"
                      >
                        <Pencil className="h-3 w-3" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setDeleteTarget(v)}
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
      )}

      {/* Variant Form Sheet */}
      <VariantFormSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        menuItemId={menuItemId}
        variant={editingVariant}
        onSaved={onRefresh}
      />

      {/* Delete Confirmation */}
      <AlertDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent className="bg-[#0d1f3c] border-white/10">
          <AlertDialogHeader>
            <AlertDialogTitle className="text-white">
              Deactivate Variant
            </AlertDialogTitle>
            <AlertDialogDescription className="text-[#7a9bb5]">
              Are you sure you want to deactivate &quot;{deleteTarget?.name}
              &quot;?
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
