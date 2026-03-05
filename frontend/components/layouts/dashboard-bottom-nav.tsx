"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { navItems } from "@/lib/nav-items";
import { getAuthUser } from "@/lib/auth";

export function MobileBottomNav() {
  const pathname = usePathname();
  const [role, setRole] = useState<string | null>(null);

  useEffect(() => {
    setRole(getAuthUser()?.role ?? null);
  }, []);

  const visibleItems = navItems.filter(
    (item) => !("adminOnly" in item && item.adminOnly) || role === "Admin"
  );

  return (
    <nav
      className="lg:hidden fixed bottom-0 left-0 right-0 z-50 bg-tennis-green flex"
      style={{ paddingBottom: "env(safe-area-inset-bottom)" }}
    >
      {visibleItems.map(({ label, href, icon: Icon, exact }) => {
        const active = exact ? pathname === href : pathname.startsWith(href);
        return (
          <Link
            key={href}
            href={href}
            className={`flex-1 flex flex-col items-center justify-center gap-1 py-3 transition-colors ${
              active ? "text-tennis-lime" : "text-white/60 hover:text-white"
            }`}
          >
            {active && (
              <span className="absolute top-0 w-8 h-0.5 bg-tennis-lime rounded-full" aria-hidden="true" />
            )}
            <Icon size={20} className="shrink-0" />
            <span className="text-[10px] font-medium leading-none">{label}</span>
          </Link>
        );
      })}
    </nav>
  );
}
