"use client";

import { useState, useEffect, useCallback } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import { CategoryList } from "@/components/admin/category-list";
import { MenuItemList } from "@/components/admin/menu-item-list";
import type { CategoryDto } from "@/types/api";

export default function MenuPage() {
  const { token } = useAuth();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [loading, setLoading] = useState(true);

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
        <h1 className="text-2xl font-bold text-white">Menu</h1>
        <p className="text-[#7a9bb5] mt-1">
          Manage your menu items, categories, variants, and ingredients.
        </p>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="items" className="space-y-4">
        <TabsList className="bg-[#0d1f3c] border border-white/10">
          <TabsTrigger
            value="items"
            className="data-[state=active]:bg-[#00e5ff]/10 data-[state=active]:text-[#00e5ff] text-[#7a9bb5]"
          >
            Menu Items
          </TabsTrigger>
          <TabsTrigger
            value="categories"
            className="data-[state=active]:bg-[#00e5ff]/10 data-[state=active]:text-[#00e5ff] text-[#7a9bb5]"
          >
            Categories
          </TabsTrigger>
        </TabsList>

        <TabsContent value="items">
          <MenuItemList categories={categories} />
        </TabsContent>

        <TabsContent value="categories">
          <CategoryList />
        </TabsContent>
      </Tabs>
    </div>
  );
}
