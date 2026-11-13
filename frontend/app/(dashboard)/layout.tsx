"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { InboxBell } from "@/components/dashboard/inbox-bell";
import { DashboardSidebar } from "@/components/layouts/dashboard-sidebar";
import { MobileBottomNav } from "@/components/layouts/dashboard-bottom-nav";
import { SlashLabel } from "@/components/ui/slash-label";
import { AccountBanners } from "@/components/dashboard/account-banners";
import { isAuthenticated } from "@/lib/auth";
import { useAuthUser } from "@/lib/hooks/use-auth-user";
import { useClientValue } from "@/lib/hooks/use-client-value";

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
  // null tijdens SSR/hydration; daarna true/false.
  const authenticated = useClientValue(isAuthenticated);
  const user = useAuthUser();

  useEffect(() => {
    if (authenticated === false) router.replace("/login");
  }, [authenticated, router]);

  if (!authenticated || !user) return null;

  return (
    <div className="flex h-screen bg-canvas overflow-hidden">
      <DashboardSidebar />

      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* Topbar */}
        <header className="bg-paper border-b border-rule px-7 py-3.5 flex items-center justify-between shrink-0">
          <SlashLabel>{formatDayHeader()}</SlashLabel>
          {user.role === "Admin" && <InboxBell />}
        </header>

        <AccountBanners user={user} />

        {/* Page content */}
        <main className="flex-1 overflow-y-auto px-7 py-6 pb-[calc(4.5rem_+_env(safe-area-inset-bottom))] lg:pb-6">
          {children}
        </main>
      </div>

      <MobileBottomNav />
    </div>
  );
}
