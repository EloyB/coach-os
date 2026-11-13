"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import Link from "next/link";

import { clearAuth, syncRoleCookie } from "@/lib/auth";
import { homePathFor, isStudentRole } from "@/lib/role-routing";
import { useAuthUser } from "@/lib/hooks/use-auth-user";
import { LogoMark } from "@/components/ui/logo-mark";

export default function StudentLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const t = useTranslations("studentPortal");
  const user = useAuthUser();
  const isStudent = isStudentRole(user?.role);
  const firstName = isStudent ? (user?.firstName ?? null) : null;

  useEffect(() => {
    // undefined = nog onbekend (SSR/hydration): dan nog niet doorsturen.
    if (user === undefined || isStudent) return;
    if (user === null) {
      // Geen sessie meer: achtergebleven cookies opruimen, anders stuurt de middleware
      // /student-login meteen terug.
      clearAuth();
      router.replace("/student-login");
      return;
    }
    // Staff op een studentpagina: naar de eigen startpagina. Rolcookie eerst rechtzetten
    // zodat de middleware dezelfde beslissing neemt (geen heen-en-weer).
    syncRoleCookie(user.role);
    router.replace(homePathFor(user.role, user.isSuperAdmin === true));
  }, [user, isStudent, router]);

  // Niets renderen voor wie geen student is: zo vertrekken er geen student-API-calls.
  if (!isStudent) return null;

  function logout() {
    clearAuth();
    router.replace("/student-login");
  }

  return (
    <div className="min-h-screen bg-canvas flex flex-col">
      <header className="bg-paper border-b border-rule px-6 py-[18px] flex items-center justify-between">
        <Link href="/student/lessons" className="flex items-center gap-2.5">
          <LogoMark variant="green" />
          <span className="text-ink font-bold text-[13px]">CoachOS</span>
        </Link>
        <div className="flex items-center gap-3 text-xs text-ink-3">
          {firstName && <span>Hallo, {firstName} 👋</span>}
          <button
            onClick={logout}
            className="px-3 py-1.5 rounded-md border border-rule hover:bg-canvas transition-colors cursor-pointer text-ink-2 text-xs"
          >
            {t("logout")}
          </button>
        </div>
      </header>
      <main className="flex-1 max-w-[680px] mx-auto w-full px-6 py-5">
        {children}
      </main>
    </div>
  );
}
