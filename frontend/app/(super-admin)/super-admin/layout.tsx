"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";
import { Button } from "@/components/ui/button";
import { clearAuth, getAuthUser, isAuthenticated, isSuperAdmin, type AuthUser } from "@/lib/auth";

export default function SuperAdminLayout({ children }: { children: React.ReactNode }) {
  const t = useTranslations("superAdmin");
  const router = useRouter();
  const pathname = usePathname();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace("/login");
      return;
    }
    if (!isSuperAdmin()) {
      router.replace("/dashboard");
      return;
    }
    setUser(getAuthUser());
    setChecked(true);
  }, [router]);

  function handleLogout() {
    clearAuth();
    router.replace("/login");
  }

  if (!checked || !user) return null;

  const navItems = [
    { href: "/super-admin/dashboard", label: t("navDashboard") },
    { href: "/super-admin/admins", label: t("navAdmins") },
    { href: "/super-admin/organizations", label: t("navOrganizations") },
  ];

  return (
    <div className="flex h-screen bg-ink text-white overflow-hidden">
      <aside className="w-64 shrink-0 flex flex-col border-r border-white/10 p-5">
        <div className="flex items-center gap-2 mb-8">
          <div className="w-7 h-7 rounded-md bg-tennis-lime grid place-items-center">
            <Mono className="text-ink font-extrabold text-[12px]">c/</Mono>
          </div>
          <div className="leading-tight">
            <div className="font-bold text-base">CoachOS</div>
            <Mono className="text-[10px] text-tennis-lime tracking-wider">SUPER ADMIN</Mono>
          </div>
        </div>

        <nav className="flex flex-col gap-1">
          {navItems.map((item) => {
            const active = pathname?.startsWith(item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`px-3 py-2 rounded-md text-sm font-medium transition ${
                  active
                    ? "bg-tennis-lime text-ink"
                    : "text-white/70 hover:bg-white/5 hover:text-white"
                }`}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="mt-auto pt-6 border-t border-white/10">
          <div className="text-xs text-white/60 mb-1">{user.firstName} {user.lastName}</div>
          <div className="text-[11px] text-white/40 mb-3 truncate">{user.email}</div>
          <Button
            variant="outline"
            size="sm"
            className="w-full bg-transparent text-white border-white/20 hover:bg-white/10 hover:text-white"
            onClick={handleLogout}
          >
            {t("logout")}
          </Button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden min-w-0 bg-canvas text-ink">
        <header className="bg-paper border-b border-rule px-7 py-3.5 flex items-center shrink-0">
          <SlashLabel>{t("topbarLabel")}</SlashLabel>
        </header>
        <main className="flex-1 overflow-y-auto px-7 py-6">{children}</main>
      </div>
    </div>
  );
}
