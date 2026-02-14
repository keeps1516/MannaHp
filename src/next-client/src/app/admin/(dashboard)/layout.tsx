"use client";

import { AuthProvider } from "@/store/auth-context";
import { AdminAuthGuard } from "@/components/admin/auth-guard";
import { AdminSidebar, AdminMobileNav } from "@/components/admin/sidebar";

export default function AdminDashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthProvider>
      <AdminAuthGuard>
        <div className="flex h-screen bg-[#0a1628]">
          <AdminSidebar />
          <div className="flex-1 flex flex-col overflow-hidden">
            <AdminMobileNav />
            <main className="flex-1 overflow-y-auto p-4 lg:p-8">
              {children}
            </main>
          </div>
        </div>
      </AdminAuthGuard>
    </AuthProvider>
  );
}
