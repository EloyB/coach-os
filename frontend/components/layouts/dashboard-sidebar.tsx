"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { LogOut } from "lucide-react";
import { CourtLines } from "@/components/ui/court-lines";
import { TennisBallIcon } from "@/components/ui/tennis-ball-icon";
import { navItems } from "@/lib/nav-items";

export function DashboardSidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden lg:flex flex-col w-55 bg-tennis-green relative overflow-hidden shrink-0">
      <CourtLines />

      {/* Logo */}
      <div className="relative z-10 px-6 py-7 border-b border-white/10">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-full bg-tennis-lime flex items-center justify-center shrink-0">
            <TennisBallIcon className="w-4.5 h-4.5" />
          </div>
          <span className="text-white font-bold text-lg tracking-tight leading-none">
            Coach<span className="text-tennis-lime">OS</span>
          </span>
        </div>
      </div>

      {/* Nav */}
      <nav className="relative z-10 flex-1 px-3 py-5 space-y-0.5">
        {navItems.map(({ label, href, icon: Icon, exact }) => {
          const active = exact ? pathname === href : pathname.startsWith(href);
          return (
            <Link
              key={href}
              href={href}
              className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors group ${
                active
                  ? "bg-white/15 text-white"
                  : "text-white/60 hover:text-white hover:bg-white/10"
              }`}
            >
              {active && (
                <span
                  className="absolute left-3 w-0.5 h-5 bg-tennis-lime rounded-full"
                  aria-hidden="true"
                />
              )}
              <Icon
                size={16}
                className={`shrink-0 transition-colors ${
                  active
                    ? "text-tennis-lime"
                    : "text-white/50 group-hover:text-white/80"
                }`}
              />
              <span>{label}</span>
            </Link>
          );
        })}
      </nav>

      {/* Logout */}
      <div className="relative z-10 px-3 py-5 border-t border-white/10">
        <Link
          href="/login"
          className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-white/50 hover:text-white hover:bg-white/10 transition-colors group"
        >
          <LogOut size={16} className="shrink-0 group-hover:text-white/80" />
          <span>Uitloggen</span>
        </Link>
      </div>
    </aside>
  );
}
