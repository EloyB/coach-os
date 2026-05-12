"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { LogOut, ChevronRight, Check } from "lucide-react";
import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";
import { navItems } from "@/lib/nav-items";
import { switchOrganization } from "@/lib/api/auth";
import { getAuthUser, setAuthUser, setToken, clearAuth, type AuthUser } from "@/lib/auth";

export function DashboardSidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const tSwitcher = useTranslations("orgSwitcher");
  const [user, setUser] = useState<AuthUser | null>(null);
  const [switcherOpen, setSwitcherOpen] = useState(false);
  const [isSwitching, setIsSwitching] = useState(false);
  const switcherRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setUser(getAuthUser());
  }, []);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (switcherRef.current && !switcherRef.current.contains(event.target as Node)) {
        setSwitcherOpen(false);
      }
    }
    if (switcherOpen) document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [switcherOpen]);

  async function handleSelectOrg(organizationId: string) {
    if (organizationId === user?.organizationId || isSwitching) {
      setSwitcherOpen(false);
      return;
    }
    setIsSwitching(true);
    try {
      const response = await switchOrganization(organizationId);
      setToken(response.token);
      const current = getAuthUser();
      setAuthUser({
        userId: response.userId,
        email: response.email,
        firstName: response.firstName ?? current?.firstName,
        lastName: response.lastName ?? current?.lastName,
        organizationId: response.organizationId,
        role: response.role,
        memberships: response.memberships,
      });
      setSwitcherOpen(false);
      router.refresh();
      window.location.reload();
    } catch {
      setIsSwitching(false);
    }
  }

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
    <aside className="hidden lg:flex flex-col w-56 bg-tennis-green relative shrink-0">
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
        {user?.memberships && user.memberships.length > 0 && (() => {
          const activeName =
            user.memberships.find((m) => m.organizationId === user.organizationId)
              ?.organizationName ?? "Club";
          const monogram = (activeName[0] ?? "C").toUpperCase();
          const canSwitch = user.memberships.length > 1;

          const tileClass =
            "w-full px-2.5 py-2 bg-black/[.18] rounded-md flex items-center gap-2 text-left";
          const tileInner = (
            <>
              <div className="w-5 h-5 rounded bg-tennis-lime/20 grid place-items-center">
                <Mono className="text-tennis-lime text-[10px] font-bold">
                  {monogram}
                </Mono>
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-[10px] text-white/50 uppercase tracking-[0.08em] m-0">
                  Club
                </p>
                <p className="text-[11.5px] font-semibold text-white m-0 truncate">
                  {activeName}
                </p>
              </div>
              <ChevronRight className="w-3 h-3 text-white/50 rotate-90" />
            </>
          );

          return (
            <div ref={switcherRef} className="relative mt-4">
              {canSwitch ? (
                <button
                  type="button"
                  onClick={() => setSwitcherOpen((v) => !v)}
                  disabled={isSwitching}
                  className={`${tileClass} cursor-pointer disabled:opacity-50`}
                >
                  {tileInner}
                </button>
              ) : (
                <div className={tileClass}>{tileInner}</div>
              )}

              {switcherOpen && canSwitch && (
                <div className="absolute left-full ml-2 top-0 w-64 rounded-lg border border-gray-100 bg-white shadow-lg z-50 overflow-hidden">
                  <div className="px-3 py-2 text-xs font-semibold text-gray-400 uppercase tracking-wider border-b border-gray-100">
                    {tSwitcher("title")}
                  </div>
                  <ul className="py-1 max-h-80 overflow-y-auto">
                    {user.memberships.map((m) => (
                      <li key={m.organizationId}>
                        <button
                          type="button"
                          onClick={() => handleSelectOrg(m.organizationId)}
                          className="w-full flex items-center justify-between gap-2 px-3 py-2 text-left hover:bg-gray-50 transition-colors cursor-pointer"
                        >
                          <div className="min-w-0">
                            <p className="text-sm font-medium text-gray-800 truncate">
                              {m.organizationName}
                            </p>
                            <p className="text-xs text-gray-400">{m.role}</p>
                          </div>
                          {m.organizationId === user.organizationId && (
                            <Check className="w-4 h-4 text-tennis-green shrink-0" />
                          )}
                        </button>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          );
        })()}
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
                  ? "bg-tennis-lime/[.12] text-white"
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
