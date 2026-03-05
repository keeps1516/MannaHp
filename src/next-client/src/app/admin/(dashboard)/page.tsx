"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import {
  Loader2,
  ShoppingBag,
  AlertTriangle,
  DollarSign,
  ArrowRight,
} from "lucide-react";
import { useAuth } from "@/store/auth-context";
import { adminApi } from "@/lib/admin-api";

export default function AdminDashboardPage() {
  const { user, token } = useAuth();
  const [activeOrders, setActiveOrders] = useState<number | null>(null);
  const [lowStockCount, setLowStockCount] = useState<number | null>(null);
  const [todayRevenue, setTodayRevenue] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchData = useCallback(async () => {
    if (!token) return;
    try {
      const [orders, ingredients, revenue] = await Promise.all([
        adminApi.getActiveOrders(token),
        adminApi.getIngredients(token),
        adminApi.getTodayRevenue(token),
      ]);

      setActiveOrders(orders.length);
      setLowStockCount(
        ingredients.filter(
          (i) => i.active && i.stockQuantity < i.lowStockThreshold
        ).length
      );
      setTodayRevenue(revenue.total);
    } catch {
      // silent — dashboard is non-critical
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">Dashboard</h1>
        <p className="text-[#7a9bb5] mt-1">
          Welcome back, {user?.displayName ?? user?.email}
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <DashboardCard
          title="Active Orders"
          value={loading ? null : String(activeOrders ?? "0")}
          icon={<ShoppingBag className="h-5 w-5 text-[#00e5ff]" />}
          iconBg="bg-[#00e5ff]/10"
        />
        <DashboardCard
          title="Low Stock Items"
          value={loading ? null : String(lowStockCount ?? "0")}
          icon={<AlertTriangle className="h-5 w-5 text-amber-400" />}
          iconBg="bg-amber-400/10"
          alert={lowStockCount !== null && lowStockCount > 0}
        />
        <DashboardCard
          title="Today's Revenue"
          value={loading ? null : `$${(todayRevenue ?? 0).toFixed(2)}`}
          icon={<DollarSign className="h-5 w-5 text-emerald-400" />}
          iconBg="bg-emerald-400/10"
        />
      </div>

      {/* Quick Links */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <QuickLink
          href="/admin/orders"
          title="View Active Orders"
          description="Manage the order queue"
          color="text-[#00e5ff]"
        />
        <QuickLink
          href="/admin/menu"
          title="Manage Menu"
          description="Items, categories, variants"
          color="text-violet-400"
        />
        <QuickLink
          href="/admin/ingredients"
          title="Check Inventory"
          description="Stock levels and ingredients"
          color="text-emerald-400"
        />
      </div>
    </div>
  );
}

function DashboardCard({
  title,
  value,
  icon,
  iconBg,
  alert,
  muted,
}: {
  title: string;
  value: string | null;
  icon: React.ReactNode;
  iconBg: string;
  alert?: boolean;
  muted?: boolean;
}) {
  return (
    <div className="rounded-lg border border-white/10 bg-[#0d1f3c] p-5 space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-sm text-[#7a9bb5]">{title}</p>
        <div
          className={`h-9 w-9 rounded-lg ${iconBg} flex items-center justify-center`}
        >
          {icon}
        </div>
      </div>
      {value === null ? (
        <Loader2 className="h-5 w-5 animate-spin text-[#7a9bb5]" />
      ) : (
        <p
          className={`text-2xl font-bold ${
            alert
              ? "text-amber-400"
              : muted
                ? "text-[#4a6a85] text-lg"
                : "text-white"
          }`}
        >
          {value}
        </p>
      )}
    </div>
  );
}

function QuickLink({
  href,
  title,
  description,
  color,
}: {
  href: string;
  title: string;
  description: string;
  color: string;
}) {
  return (
    <Link
      href={href}
      className="group rounded-lg border border-white/10 bg-[#0d1f3c] p-5 hover:border-white/20 hover:bg-[#0d1f3c]/80 transition-all"
    >
      <div className="flex items-center justify-between">
        <div>
          <p className={`font-semibold ${color}`}>{title}</p>
          <p className="text-sm text-[#7a9bb5] mt-1">{description}</p>
        </div>
        <ArrowRight className="h-4 w-4 text-[#4a6a85] group-hover:text-white transition-colors" />
      </div>
    </Link>
  );
}
