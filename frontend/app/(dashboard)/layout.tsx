"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DashboardSidebar } from "@/components/layouts/dashboard-sidebar";
import { MobileBottomNav } from "@/components/layouts/dashboard-bottom-nav";
import { OrgSwitcher } from "@/components/layouts/org-switcher";
import { SlashLabel } from "@/components/ui/slash-label";
import { getAuthUser, isAuthenticated, type AuthUser } from "@/lib/auth";

function formatDayHeader(): string {
  const now = new Date();
  const day = now.toLocaleDateString("nl-BE", { weekday: "short" }).toUpperCase().replace(".", "");
  const date = now.toLocaleDateString("nl-BE", { day: "numeric", month: "short", year: "numeric" }).toUpperCase();
  const week = getISOWeek(now);
  return `${day} · ${date} · WEEK ${week}`;
}

function getISOWeek(d: Date): number {
  const date = new Date(d.getTime());
  date.setHours(0, 0, 0, 0);
  date.setDate(date.getDate() + 3 - ((date.getDay() + 6) % 7));
  const week1 = new Date(date.getFullYear(), 0, 4);
  return 1 + Math.round(((date.getTime() - week1.getTime()) / 86400000 - 3 + ((week1.getDay() + 6) % 7)) / 7);
}

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

  const initials =
    `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toUpperCase() || "C";

  return (
    <div className="flex h-screen bg-canvas overflow-hidden">
      <DashboardSidebar />

      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* Topbar */}
        <header className="bg-paper border-b border-rule px-7 py-3.5 flex items-center justify-between shrink-0">
          <div>
            <SlashLabel>{formatDayHeader()}</SlashLabel>
          </div>
          <div className="flex items-center gap-2.5">
            {user.memberships && user.memberships.length > 1 && (
              <OrgSwitcher
                memberships={user.memberships}
                activeOrganizationId={user.organizationId}
              />
            )}
            <div className="w-[30px] h-[30px] rounded-full bg-tennis-green grid place-items-center shrink-0">
              <span className="text-tennis-lime text-[11px] font-bold">
                {initials}
              </span>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto px-7 py-6 pb-16 lg:pb-6">
          {children}
        </main>
      </div>

      <MobileBottomNav />
    </div>
  );
}
