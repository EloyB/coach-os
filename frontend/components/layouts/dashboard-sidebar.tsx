"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { LogOut, ChevronRight } from "lucide-react";
import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";
import { navItems } from "@/lib/nav-items";
import { getAuthUser, clearAuth, type AuthUser } from "@/lib/auth";

export function DashboardSidebar() {
  const pathname = usePathname();
  const [user, setUser] = useState<AuthUser | null>(null);

  useEffect(() => {
    setUser(getAuthUser());
  }, []);

  const role = user?.role ?? null;
  const visibleItems = navItems.filter(
    (item) => !("adminOnly" in item && item.adminOnly) || role === "Admin"
  );

  const fullName = user
    ? `${user.firstName} ${user.lastName}`.trim() || "Coach"
    : "Coach";
  const initials = user
    ? `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toUpperCase() || "C"
    : "C";

  return (
    <aside className="hidden lg:flex flex-col w-56 bg-tennis-green relative overflow-hidden shrink-0">
      <CourtLines opacity={0.05} />

      {/* Logo + c/ monogram */}
      <div className="relative z-10 px-[18px] pt-5 pb-[18px]">
        <div className="flex items-center gap-2.5">
          <div className="w-7 h-7 rounded-md bg-tennis-lime grid place-items-center">
            <Mono className="text-tennis-green font-extrabold text-[13px]">
              c/
            </Mono>
          </div>
          <span className="text-white font-bold text-[15.5px] tracking-tight">
            CoachOS
          </span>
        </div>

        {/* Club switcher */}
        {user?.memberships && user.memberships.length > 0 && (
          <div className="mt-4 px-2.5 py-2 bg-black/[.18] rounded-md flex items-center gap-2">
            <div className="w-5 h-5 rounded bg-tennis-lime/20 grid place-items-center">
              <Mono className="text-tennis-lime text-[10px] font-bold">
                {(user.memberships.find(m => m.organizationId === user.organizationId)?.organizationName ?? "TC")[0].toUpperCase()}
              </Mono>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-[10px] text-white/50 uppercase tracking-[0.08em] m-0">
                Club
              </p>
              <p className="text-[11.5px] font-semibold text-white m-0 truncate">
                {user.memberships.find(m => m.organizationId === user.organizationId)?.organizationName ?? "Club"}
              </p>
            </div>
            <ChevronRight className="w-3 h-3 text-white/50 rotate-90" />
          </div>
        )}
      </div>

      {/* Nav */}
      <nav className="relative z-10 flex-1 px-2.5 flex flex-col gap-px">
        <p className="mx-3 mt-2.5 mb-1.5 text-[9.5px] text-white/35 font-bold uppercase tracking-[0.12em]">
          Werk
        </p>
        {visibleItems.map(({ label, href, icon: Icon, exact }) => {
          const active = exact ? pathname === href : pathname.startsWith(href);
          return (
            <Link
              key={href}
              href={href}
              className={`flex items-center gap-[11px] px-3 py-2 rounded-md text-[12.5px] font-medium transition-colors ${
                active
                  ? "bg-tennis-lime/[.12] text-white shadow-[inset_2px_0_0_#D0FF14]"
                  : "text-white/70 hover:text-white hover:bg-white/10"
              }`}
            >
              <Icon
                size={14.5}
                className={`shrink-0 transition-colors ${
                  active ? "text-tennis-lime" : "text-white/50"
                }`}
              />
              <span className="flex-1">{label}</span>
            </Link>
          );
        })}
      </nav>

      {/* Profile */}
      <div className="relative z-10 mx-2.5 mb-2.5 px-3 py-2.5 bg-black/[.18] rounded-lg flex items-center gap-2.5">
        <div className="w-7 h-7 rounded-full bg-tennis-lime grid place-items-center shrink-0">
          <span className="text-tennis-green font-bold text-[11px]">
            {initials}
          </span>
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-[11.5px] font-semibold text-white m-0 truncate">
            {fullName}
          </p>
          <p className="text-[10px] text-white/50 m-0">
            {role === "Admin" ? "Beheerder" : role === "Trainer" ? "Trainer" : ""}
          </p>
        </div>
        <button
          onClick={() => {
            clearAuth();
            window.location.href = "/login";
          }}
          className="text-white/40 hover:text-white/80 transition-colors cursor-pointer"
          aria-label="Uitloggen"
        >
          <LogOut size={13} />
        </button>
      </div>
    </aside>
  );
}
