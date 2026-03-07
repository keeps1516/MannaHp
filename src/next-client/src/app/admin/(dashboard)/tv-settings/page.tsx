"use client";

import { useEffect, useState, useCallback } from "react";
import { Loader2, Tv, ExternalLink } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Checkbox } from "@/components/ui/checkbox";
import { adminApi } from "@/lib/admin-api";
import { useAuth } from "@/store/auth-context";
import type {
  CategoryDto,
  MenuItemDto,
  TvMenuConfig,
  SampleBowlConfig,
} from "@/types/api";
import { toast } from "sonner";

const DEFAULT_CONFIG: TvMenuConfig = {
  visibleCategoryIds: [],
  hiddenItemIds: [],
  showAllIngredients: false,
  orderOnlineUrl: "",
  sampleBowls: {},
};

export default function TvSettingsPage() {
  const { token: authToken } = useAuth();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [config, setConfig] = useState<TvMenuConfig>(DEFAULT_CONFIG);

  const fetchData = useCallback(async () => {
    if (!authToken) return;
    try {
      const [cats, items, settings] = await Promise.all([
        adminApi.getCategories(authToken),
        adminApi.getMenuItems(authToken),
        adminApi.getSettings(authToken),
      ]);
      setCategories(cats.filter((c) => c.active).sort((a, b) => a.sortOrder - b.sortOrder));
      setMenuItems(items.filter((i) => i.active).sort((a, b) => a.sortOrder - b.sortOrder));

      const tvSetting = settings.find((s) => s.key === "TvMenuConfig");
      if (tvSetting) {
        try {
          const parsed = JSON.parse(tvSetting.value) as TvMenuConfig;
          setConfig({
            visibleCategoryIds: parsed.visibleCategoryIds ?? [],
            hiddenItemIds: parsed.hiddenItemIds ?? [],
            showAllIngredients: parsed.showAllIngredients ?? false,
            orderOnlineUrl: parsed.orderOnlineUrl ?? "",
            sampleBowls: parsed.sampleBowls ?? {},
          });
        } catch {
          setConfig(DEFAULT_CONFIG);
        }
      }
    } catch {
      toast.error("Failed to load settings");
    } finally {
      setLoading(false);
    }
  }, [authToken]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  async function handleSave() {
    if (!authToken) return;
    setSaving(true);
    try {
      await adminApi.updateSettings(authToken, [
        { key: "TvMenuConfig", value: JSON.stringify(config) },
      ]);
      toast.success("TV settings saved");
    } catch {
      toast.error("Failed to save settings");
    } finally {
      setSaving(false);
    }
  }

  function toggleCategory(categoryId: string) {
    setConfig((prev) => {
      const ids = new Set(prev.visibleCategoryIds);
      if (ids.has(categoryId)) {
        ids.delete(categoryId);
      } else {
        ids.add(categoryId);
      }
      return { ...prev, visibleCategoryIds: Array.from(ids) };
    });
  }

  function toggleItem(itemId: string) {
    setConfig((prev) => {
      const ids = new Set(prev.hiddenItemIds);
      if (ids.has(itemId)) {
        ids.delete(itemId);
      } else {
        ids.add(itemId);
      }
      return { ...prev, hiddenItemIds: Array.from(ids) };
    });
  }

  function updateSampleBowl(
    menuItemId: string,
    update: Partial<SampleBowlConfig>
  ) {
    setConfig((prev) => {
      const existing = prev.sampleBowls[menuItemId] ?? {
        label: "",
        ingredientIds: [],
      };
      return {
        ...prev,
        sampleBowls: {
          ...prev.sampleBowls,
          [menuItemId]: { ...existing, ...update },
        },
      };
    });
  }

  function toggleSampleBowlIngredient(
    menuItemId: string,
    ingredientId: string
  ) {
    setConfig((prev) => {
      const existing = prev.sampleBowls[menuItemId] ?? {
        label: "",
        ingredientIds: [],
      };
      const ids = new Set(existing.ingredientIds);
      if (ids.has(ingredientId)) {
        ids.delete(ingredientId);
      } else {
        ids.add(ingredientId);
      }
      return {
        ...prev,
        sampleBowls: {
          ...prev.sampleBowls,
          [menuItemId]: { ...existing, ingredientIds: Array.from(ids) },
        },
      };
    });
  }

  // Group items by category
  const itemsByCategory = new Map<string, MenuItemDto[]>();
  for (const item of menuItems) {
    const list = itemsByCategory.get(item.categoryId) ?? [];
    list.push(item);
    itemsByCategory.set(item.categoryId, list);
  }

  const customizableItems = menuItems.filter((i) => i.isCustomizable);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-6 w-6 animate-spin text-[#00e5ff]" />
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white flex items-center gap-2">
            <Tv className="h-6 w-6 text-[#00e5ff]" />
            TV Menu Settings
          </h1>
          <p className="text-sm text-[#7a9bb5] mt-1">
            Configure what appears on the in-store TV menu display
          </p>
        </div>
        <Button
          onClick={() => window.open("/admin/tv-menu", "_blank")}
          variant="outline"
          className="border-[#1e3a5f] text-[#7a9bb5] hover:text-white hover:bg-white/5"
        >
          <ExternalLink className="h-4 w-4 mr-2" />
          Launch TV Display
        </Button>
      </div>

      {/* Order Online URL */}
      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
        <h2 className="font-semibold text-white text-sm">Order Online URL</h2>
        <Separator className="bg-[#1e3a5f]" />
        <p className="text-xs text-[#7a9bb5]">
          The URL for the &quot;Order Online&quot; QR code on the TV display.
        </p>
        <Input
          value={config.orderOnlineUrl}
          onChange={(e) =>
            setConfig((prev) => ({ ...prev, orderOnlineUrl: e.target.value }))
          }
          placeholder="https://manna.example.com"
          className="bg-[#0a1628] border-white/10 text-white placeholder:text-[#7a9bb5]/50"
        />
      </div>

      {/* Visible Categories */}
      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
        <h2 className="font-semibold text-white text-sm">Visible Categories</h2>
        <Separator className="bg-[#1e3a5f]" />
        <p className="text-xs text-[#7a9bb5]">
          Select which categories to show on the TV. If none selected, all active categories are shown.
        </p>
        <div className="space-y-2">
          {categories.map((cat) => (
            <label
              key={cat.id}
              className="flex items-center gap-3 cursor-pointer py-1"
            >
              <Checkbox
                checked={config.visibleCategoryIds.includes(cat.id)}
                onCheckedChange={() => toggleCategory(cat.id)}
              />
              <span className="text-sm text-white">{cat.name}</span>
              <span className="text-xs text-[#7a9bb5]">
                ({(itemsByCategory.get(cat.id) ?? []).length} items)
              </span>
            </label>
          ))}
        </div>
      </div>

      {/* Visible Items */}
      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
        <h2 className="font-semibold text-white text-sm">Hidden Items</h2>
        <Separator className="bg-[#1e3a5f]" />
        <p className="text-xs text-[#7a9bb5]">
          Check items you want to hide from the TV display.
        </p>
        {categories.map((cat) => {
          const items = itemsByCategory.get(cat.id) ?? [];
          if (items.length === 0) return null;
          return (
            <div key={cat.id} className="space-y-1">
              <p className="text-xs font-medium text-[#a0c4d8] mt-2">
                {cat.name}:
              </p>
              <div className="flex flex-wrap gap-x-4 gap-y-1 pl-2">
                {items.map((item) => (
                  <label
                    key={item.id}
                    className="flex items-center gap-2 cursor-pointer py-0.5"
                  >
                    <Checkbox
                      checked={config.hiddenItemIds.includes(item.id)}
                      onCheckedChange={() => toggleItem(item.id)}
                    />
                    <span
                      className={`text-sm ${
                        config.hiddenItemIds.includes(item.id)
                          ? "text-[#4a6a85] line-through"
                          : "text-white"
                      }`}
                    >
                      {item.name}
                    </span>
                  </label>
                ))}
              </div>
            </div>
          );
        })}
      </div>

      {/* Sample Bowl Config */}
      {customizableItems.length > 0 && (
        <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-4">
          <h2 className="font-semibold text-white text-sm">
            Sample Bowl Configuration
          </h2>
          <Separator className="bg-[#1e3a5f]" />
          <p className="text-xs text-[#7a9bb5]">
            Configure a sample bowl with a label and selected ingredients to
            show a representative price on the TV.
          </p>

          {customizableItems.map((item) => {
            const bowl = config.sampleBowls[item.id] ?? {
              label: "",
              ingredientIds: [],
            };
            const selectedPrice = (item.availableIngredients ?? [])
              .filter((ai) => ai.active && bowl.ingredientIds.includes(ai.id))
              .reduce((sum, ai) => sum + ai.customerPrice, 0);

            return (
              <div
                key={item.id}
                className="border border-[#1e3a5f] rounded-lg p-3 space-y-3"
              >
                <p className="text-sm font-medium text-white">{item.name}</p>

                <div className="flex items-center gap-3">
                  <label className="text-xs text-[#7a9bb5] shrink-0">
                    Label:
                  </label>
                  <Input
                    value={bowl.label}
                    onChange={(e) =>
                      updateSampleBowl(item.id, { label: e.target.value })
                    }
                    placeholder="e.g. Popular Bowl"
                    className="bg-[#0a1628] border-white/10 text-white placeholder:text-[#7a9bb5]/50 text-sm h-8"
                  />
                </div>

                <div>
                  <p className="text-xs text-[#7a9bb5] mb-1">Ingredients:</p>
                  <div className="flex flex-wrap gap-x-3 gap-y-1 pl-1">
                    {(item.availableIngredients ?? [])
                      .filter((ai) => ai.active)
                      .map((ai) => (
                        <label
                          key={ai.id}
                          className="flex items-center gap-1.5 cursor-pointer py-0.5"
                        >
                          <Checkbox
                            checked={bowl.ingredientIds.includes(ai.id)}
                            onCheckedChange={() =>
                              toggleSampleBowlIngredient(item.id, ai.id)
                            }
                          />
                          <span className="text-xs text-white">
                            {ai.ingredientName}
                          </span>
                        </label>
                      ))}
                  </div>
                </div>

                {bowl.ingredientIds.length > 0 && (
                  <div className="bg-[#0a1628] rounded-lg px-3 py-2">
                    <p className="text-xs text-[#7a9bb5]">Preview:</p>
                    <p className="text-sm text-white mt-1">
                      <span className="text-[#a0c4d8]">
                        {bowl.label || "Sample Bowl"}:
                      </span>{" "}
                      {(item.availableIngredients ?? [])
                        .filter(
                          (ai) =>
                            ai.active && bowl.ingredientIds.includes(ai.id)
                        )
                        .map((ai) => ai.ingredientName.toLowerCase())
                        .join(", ")}{" "}
                      —{" "}
                      <span className="text-[#00e5ff] font-semibold">
                        ${selectedPrice.toFixed(2)}
                      </span>
                    </p>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Show All Ingredients */}
      <div className="bg-[#163a50] border border-[#1e3a5f] rounded-xl p-4 space-y-3">
        <label className="flex items-center gap-3 cursor-pointer">
          <Checkbox
            checked={config.showAllIngredients}
            onCheckedChange={(checked) =>
              setConfig((prev) => ({
                ...prev,
                showAllIngredients: checked === true,
              }))
            }
          />
          <div>
            <span className="text-sm text-white font-medium">
              Show all available ingredients on TV
            </span>
            <p className="text-xs text-[#7a9bb5]">
              Lists all available ingredients below each customizable item on
              the TV display.
            </p>
          </div>
        </label>
      </div>

      {/* Save */}
      <div className="flex justify-end">
        <Button
          onClick={handleSave}
          disabled={saving}
          className="bg-[#00e5ff] text-[#0a1628] hover:bg-[#00e5ff]/80"
        >
          {saving && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
          Save Settings
        </Button>
      </div>
    </div>
  );
}
