"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DashboardSidebar } from "@/components/layouts/dashboard-sidebar";
import { MobileBottomNav } from "@/components/layouts/dashboard-bottom-nav";
import { OrgSwitcher } from "@/components/layouts/org-switcher";
import { getAuthUser, isAuthenticated, type AuthUser } from "@/lib/auth";

const ROLE_LABELS: Record<string, string> = {
  Admin: "Beheerder",
  Trainer: "Trainer",
  Student: "Leerling",
};

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace("/login");
    } else {
      setUser(getAuthUser());
    }
    setChecked(true);
  }, [router]);

  if (!checked || !user) return null;

  const fullName = `${user.firstName} ${user.lastName}`.trim() || "Coach";
  const roleLabel = ROLE_LABELS[user.role] ?? user.role;
  const initials =
    `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toUpperCase() || "C";

  return (
    <div className="flex h-screen bg-[#F5F4F1] overflow-hidden">
      <DashboardSidebar />

      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Topbar */}
        <header className="bg-white border-b border-gray-100 px-8 py-4 flex items-center justify-between shrink-0">
          <p className="text-xs text-gray-400 font-medium uppercase tracking-wider">
            Tennis &amp; Padel Platform
          </p>
          <div className="flex items-center gap-3">
            {user.memberships && user.memberships.length > 1 && (
              <OrgSwitcher
                memberships={user.memberships}
                activeOrganizationId={user.organizationId}
              />
            )}
            <div className="text-right">
              <p className="text-sm font-semibold text-gray-800 leading-none">{fullName}</p>
              <p className="text-xs text-gray-400 mt-0.5">{roleLabel}</p>
            </div>
            <div className="w-9 h-9 rounded-full bg-tennis-green flex items-center justify-center shrink-0">
              <span className="text-tennis-lime text-sm font-bold">{initials}</span>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto px-8 py-8 pb-16 lg:pb-8">{children}</main>
      </div>

      <MobileBottomNav />
    </div>
  );
}
