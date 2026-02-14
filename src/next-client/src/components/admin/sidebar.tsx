"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  ShoppingCart,
  UtensilsCrossed,
  Wheat,
  Settings,
  LogOut,
  Menu,
} from "lucide-react";
import { useAuth } from "@/store/auth-context";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { VisuallyHidden } from "radix-ui";

const navItems = [
  { href: "/admin", label: "Dashboard", icon: LayoutDashboard },
  { href: "/admin/orders", label: "Orders", icon: ShoppingCart },
  { href: "/admin/menu", label: "Menu", icon: UtensilsCrossed },
  { href: "/admin/ingredients", label: "Ingredients", icon: Wheat },
  { href: "/admin/settings", label: "Settings", icon: Settings },
];

/** Shared sidebar content used by both desktop sidebar and mobile drawer */
function SidebarContent({ onNavClick }: { onNavClick?: () => void }) {
  const pathname = usePathname();
  const { user, logout } = useAuth();

  return (
    <div className="flex h-full flex-col">
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
              onClick={onNavClick}
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
    </div>
  );
}

/** Desktop sidebar — hidden on mobile */
export function AdminSidebar() {
  return (
    <aside className="hidden lg:flex h-screen w-64 flex-col border-r border-white/10 bg-[#0d1f3c]">
      <SidebarContent />
    </aside>
  );
}

/** Mobile top bar + drawer — hidden on desktop */
export function AdminMobileNav() {
  const [open, setOpen] = useState(false);

  return (
    <>
      {/* Top bar */}
      <div className="lg:hidden flex items-center justify-between border-b border-white/10 bg-[#0d1f3c] px-4 py-3">
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-[#00e5ff]/10 text-[#00e5ff] font-bold text-xs">
            M
          </div>
          <span className="font-semibold text-white text-sm">Manna + HP</span>
        </div>
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setOpen(true)}
          className="text-[#7a9bb5] hover:text-white hover:bg-white/5"
        >
          <Menu className="h-5 w-5" />
        </Button>
      </div>

      {/* Drawer */}
      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent
          side="left"
          showCloseButton={false}
          className="w-64 p-0 bg-[#0d1f3c] border-white/10"
        >
          <VisuallyHidden.Root>
            <SheetTitle>Navigation</SheetTitle>
          </VisuallyHidden.Root>
          <SidebarContent onNavClick={() => setOpen(false)} />
        </SheetContent>
      </Sheet>
    </>
  );
}
