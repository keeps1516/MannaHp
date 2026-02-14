"use client";

import { useAuth } from "@/store/auth-context";

export default function AdminDashboardPage() {
  const { user } = useAuth();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Dashboard</h1>
        <p className="text-[#7a9bb5] mt-1">
          Welcome back, {user?.displayName ?? user?.email}
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {/* Placeholder cards — will be replaced with real data in Phase 5 */}
        <DashboardCard title="Active Orders" value="—" />
        <DashboardCard title="Today&apos;s Revenue" value="—" />
        <DashboardCard title="Low Stock Items" value="—" />
      </div>

      <div className="rounded-lg border border-white/10 bg-[#0d1f3c] p-6">
        <p className="text-[#7a9bb5] text-sm">
          Dashboard content coming soon. Use the sidebar to manage orders, menu
          items, and ingredients.
        </p>
      </div>
    </div>
  );
}

function DashboardCard({ title, value }: { title: string; value: string }) {
  return (
    <div className="rounded-lg border border-white/10 bg-[#0d1f3c] p-5 space-y-1">
      <p className="text-sm text-[#7a9bb5]">{title}</p>
      <p className="text-2xl font-bold text-white">{value}</p>
    </div>
  );
}
