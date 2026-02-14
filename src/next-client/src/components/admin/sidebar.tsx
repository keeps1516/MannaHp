"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  ShoppingCart,
  UtensilsCrossed,
  Wheat,
  Settings,
  LogOut,
} from "lucide-react";
import { useAuth } from "@/store/auth-context";
import { cn } from "@/lib/utils";

const navItems = [
  { href: "/admin", label: "Dashboard", icon: LayoutDashboard },
  { href: "/admin/orders", label: "Orders", icon: ShoppingCart },
  { href: "/admin/menu", label: "Menu", icon: UtensilsCrossed },
  { href: "/admin/ingredients", label: "Ingredients", icon: Wheat },
  { href: "/admin/settings", label: "Settings", icon: Settings },
];

export function AdminSidebar() {
  const pathname = usePathname();
  const { user, logout } = useAuth();

  return (
    <aside className="flex h-screen w-64 flex-col border-r border-white/10 bg-[#0d1f3c]">
      {/* Brand */}
      <div className="flex items-center gap-3 border-b border-white/10 px-6 py-5">
        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-[#00e5ff]/10 text-[#00e5ff] font-bold text-sm">
          M
        </div>
        <div>
          <p className="font-semibold text-white text-sm">Manna + HP</p>
          <p className="text-xs text-[#7a9bb5]">Admin Panel</p>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 space-y-1 px-3 py-4">
        {navItems.map((item) => {
          const isActive =
            pathname === item.href ||
            (item.href !== "/admin" && pathname.startsWith(item.href));

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors",
                isActive
                  ? "bg-[#00e5ff]/10 text-[#00e5ff]"
                  : "text-[#7a9bb5] hover:bg-white/5 hover:text-white"
              )}
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </Link>
          );
        })}
      </nav>

      {/* User + Logout */}
      <div className="border-t border-white/10 px-3 py-4 space-y-2">
        <div className="px-3">
          <p className="text-sm font-medium text-white truncate">
            {user?.displayName ?? user?.email}
          </p>
          <p className="text-xs text-[#7a9bb5] truncate">{user?.role}</p>
        </div>
        <button
          onClick={logout}
          className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-[#ff4757] hover:bg-[#ff4757]/10 transition-colors"
        >
          <LogOut className="h-4 w-4" />
          Sign Out
        </button>
      </div>
    </aside>
  );
}
