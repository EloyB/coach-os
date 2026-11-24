"use client";

import { Suspense, useEffect } from "react";
import { useRouter } from "next/navigation";
import { InboxBell } from "@/components/dashboard/inbox-bell";
import { OrganizationParamSwitch } from "@/components/dashboard/organization-param-switch";
import { DashboardSidebar } from "@/components/layouts/dashboard-sidebar";
import { MobileBottomNav } from "@/components/layouts/dashboard-bottom-nav";
import { SlashLabel } from "@/components/ui/slash-label";
import { AccountBanners } from "@/components/dashboard/account-banners";
import { clearAuth, isAuthenticated, syncRoleCookie } from "@/lib/auth";
import { isStudentRole, STUDENT_HOME } from "@/lib/role-routing";
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

  const isStudent = isStudentRole(user?.role);

  useEffect(() => {
    if (authenticated === false) {
      // Achtergebleven cookies opruimen, anders stuurt de middleware /login meteen terug.
      clearAuth();
      router.replace("/login");
      return;
    }
    if (user && isStudent) {
      // Student op een staffpagina: naar zijn lessen, zonder uitloggen. Rolcookie eerst
      // rechtzetten zodat de middleware dezelfde beslissing neemt.
      syncRoleCookie(user.role);
      router.replace(STUDENT_HOME);
    }
  }, [authenticated, user, isStudent, router]);

  // Niets renderen voor een student: zo vertrekken er geen staff-API-calls (401 → uitloggen).
  if (!authenticated || !user || isStudent) return null;

  return (
    <div className="flex h-screen bg-canvas overflow-hidden">
      <Suspense fallback={null}>
        <OrganizationParamSwitch />
      </Suspense>
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
