"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DashboardSidebar } from "@/components/layouts/dashboard-sidebar";
import { MobileBottomNav } from "@/components/layouts/dashboard-bottom-nav";
import { TrialBanner } from "@/components/dashboard/trial-banner";
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

  return (
    <div className="flex h-screen bg-canvas overflow-hidden">
      <DashboardSidebar />

      <div className="flex-1 flex flex-col overflow-hidden min-w-0">
        {/* Topbar */}
        <header className="bg-paper border-b border-rule px-7 py-3.5 flex items-center shrink-0">
          <SlashLabel>{formatDayHeader()}</SlashLabel>
        </header>

        <TrialBanner />

        {/* Page content */}
        <main className="flex-1 overflow-y-auto px-7 py-6 pb-16 lg:pb-6">
          {children}
        </main>
      </div>

      <MobileBottomNav />
    </div>
  );
}
