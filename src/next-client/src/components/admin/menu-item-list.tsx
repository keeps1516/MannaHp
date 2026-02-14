"use client";

import { useState, useEffect, useCallback } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
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
import {
  Loader2,
  Plus,
  Pencil,
  Trash2,
  Search,
  ChevronDown,
  ChevronRight,
  Utensils,
} from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { MenuItemFormSheet } from "@/components/admin/menu-item-form-sheet";
import { VariantSection } from "@/components/admin/variant-section";
import { AvailableIngredientSection } from "@/components/admin/available-ingredient-section";
import type { MenuItemDto, CategoryDto } from "@/types/api";

interface MenuItemListProps {
  categories: CategoryDto[];
}

export function MenuItemList({ categories }: MenuItemListProps) {
  const { token } = useAuth();
  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [expandedId, setExpandedId] = useState<string | null>(null);

  // Sheet state
  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<MenuItemDto | null>(null);

  // Delete dialog state
  const [deleteTarget, setDeleteTarget] = useState<MenuItemDto | null>(null);

  const fetchMenuItems = useCallback(async () => {
    if (!token) return;
    try {
      const data = await adminApi.getMenuItems(token);
      setMenuItems(data);
    } catch {
      toast.error("Failed to load menu items");
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchMenuItems();
  }, [fetchMenuItems]);

  // Build a category name lookup
  const categoryMap = categories.reduce<Record<string, string>>((acc, c) => {
    acc[c.id] = c.name;
    return acc;
  }, {});

  // Filter by search
  const filtered = menuItems.filter((item) =>
    item.name.toLowerCase().includes(search.toLowerCase())
  );

  // Group by category
  const grouped = filtered.reduce<Record<string, MenuItemDto[]>>((acc, item) => {
    const catName = categoryMap[item.categoryId] ?? "Uncategorized";
    if (!acc[catName]) acc[catName] = [];
    acc[catName].push(item);
    return acc;
  }, {});

  const groupNames = Object.keys(grouped).sort();

  function handleAdd() {
    setEditingItem(null);
    setSheetOpen(true);
  }

  function handleEdit(item: MenuItemDto) {
    setEditingItem(item);
    setSheetOpen(true);
  }

  function toggleExpanded(id: string) {
    setExpandedId((prev) => (prev === id ? null : id));
  }

  async function handleDelete() {
    if (!token || !deleteTarget) return;
    try {
      await adminApi.deleteMenuItem(token, deleteTarget.id);
      toast.success("Menu item deactivated");
      setDeleteTarget(null);
      fetchMenuItems();
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to delete menu item"
      );
    }
  }

  // Refresh a single menu item (for when variants/available ingredients change)
  async function refreshItem(itemId: string) {
    if (!token) return;
    try {
      const updated = await adminApi.getMenuItem(token, itemId);
      setMenuItems((prev) =>
        prev.map((item) => (item.id === itemId ? updated : item))
      );
    } catch {
      // Fallback: refetch all
      fetchMenuItems();
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
          {menuItems.length} menu item{menuItems.length !== 1 ? "s" : ""}
        </p>
        <Button
          onClick={handleAdd}
          size="sm"
          className="bg-[#00e5ff] text-[#0f1f35] hover:bg-[#00c8e0] font-semibold"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Menu Item
        </Button>
      </div>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#7a9bb5]" />
        <Input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search menu items..."
          className="pl-10 bg-[#0d1f3c] border-[#1e3a5f] text-white placeholder:text-[#4a6a85]"
        />
      </div>

      {/* Items grouped by category */}
      {groupNames.length === 0 ? (
        <div className="text-center text-[#7a9bb5] py-12">
          {search
            ? "No menu items match your search."
            : "No menu items yet. Add your first menu item to get started."}
        </div>
      ) : (
        <div className="space-y-6">
          {groupNames.map((catName) => (
            <div key={catName} className="space-y-2">
              {/* Category header */}
              <h3 className="text-sm font-semibold text-[#7a9bb5] uppercase tracking-wider border-b border-white/10 pb-2">
                {catName}
              </h3>

              {/* Items */}
              <div className="space-y-2">
                {grouped[catName]
                  .sort((a, b) => a.sortOrder - b.sortOrder)
                  .map((item) => {
                    const isExpanded = expandedId === item.id;
                    return (
                      <div
                        key={item.id}
                        className="rounded-lg border border-white/10 overflow-hidden"
                      >
                        {/* Item row */}
                        <div
                          className={`flex items-center gap-3 px-4 py-3 cursor-pointer hover:bg-white/5 transition-colors ${
                            isExpanded ? "bg-white/5" : ""
                          }`}
                          onClick={() => toggleExpanded(item.id)}
                        >
                          {/* Expand icon */}
                          <div className="text-[#7a9bb5]">
                            {isExpanded ? (
                              <ChevronDown className="h-4 w-4" />
                            ) : (
                              <ChevronRight className="h-4 w-4" />
                            )}
                          </div>

                          {/* Name */}
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2">
                              <span className="font-medium text-white truncate">
                                {item.name}
                              </span>
                              {item.isCustomizable && (
                                <Badge className="bg-violet-500/10 text-violet-400 border-violet-500/20 text-xs hover:bg-violet-500/10">
                                  <Utensils className="h-3 w-3 mr-1" />
                                  Customizable
                                </Badge>
                              )}
                            </div>
                            {item.description && (
                              <p className="text-xs text-[#4a6a85] truncate mt-0.5">
                                {item.description}
                              </p>
                            )}
                          </div>

                          {/* Variant count */}
                          <span className="text-xs text-[#7a9bb5] whitespace-nowrap">
                            {item.variants.length} variant
                            {item.variants.length !== 1 ? "s" : ""}
                          </span>

                          {/* Status */}
                          {item.active ? (
                            <Badge className="bg-emerald-500/10 text-emerald-400 border-emerald-500/20 text-xs hover:bg-emerald-500/10">
                              Active
                            </Badge>
                          ) : (
                            <Badge className="bg-gray-500/10 text-gray-400 border-gray-500/20 text-xs hover:bg-gray-500/10">
                              Inactive
                            </Badge>
                          )}

                          {/* Actions */}
                          <div
                            className="flex items-center gap-1"
                            onClick={(e) => e.stopPropagation()}
                          >
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => handleEdit(item)}
                              className="h-8 w-8 text-[#7a9bb5] hover:text-[#00e5ff] hover:bg-[#00e5ff]/10"
                            >
                              <Pencil className="h-4 w-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => setDeleteTarget(item)}
                              className="h-8 w-8 text-[#7a9bb5] hover:text-[#ff4757] hover:bg-[#ff4757]/10"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </div>

                        {/* Expanded content */}
                        {isExpanded && (
                          <div className="border-t border-white/10 bg-[#0a1628]/50 px-4 py-4 space-y-6">
                            {/* Variants */}
                            <VariantSection
                              menuItemId={item.id}
                              variants={item.variants}
                              onRefresh={() => refreshItem(item.id)}
                            />

                            {/* Available Ingredients (add-ons for all items) */}
                            <div className="border-t border-white/10 pt-4">
                              <AvailableIngredientSection
                                menuItemId={item.id}
                                availableIngredients={
                                  item.availableIngredients ?? []
                                }
                                onRefresh={() => refreshItem(item.id)}
                              />
                            </div>
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

      {/* Form Sheet */}
      <MenuItemFormSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        menuItem={editingItem}
        categories={categories}
        onSaved={fetchMenuItems}
      />

      {/* Delete Confirmation */}
      <AlertDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <AlertDialogContent className="bg-[#0d1f3c] border-white/10">
          <AlertDialogHeader>
            <AlertDialogTitle className="text-white">
              Deactivate Menu Item
            </AlertDialogTitle>
            <AlertDialogDescription className="text-[#7a9bb5]">
              Are you sure you want to deactivate &quot;{deleteTarget?.name}
              &quot;? It will be hidden from the customer menu but can be
              reactivated later.
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
