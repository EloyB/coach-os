"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { ChevronsUpDown, Check } from "lucide-react";

import { switchOrganization } from "@/lib/api/auth";
import {
  getAuthUser,
  setAuthUser,
  setToken,
  type OrganizationMembershipInfo,
} from "@/lib/auth";

interface OrgSwitcherProps {
  memberships: OrganizationMembershipInfo[];
  activeOrganizationId: string | null | undefined;
}

export function OrgSwitcher({ memberships, activeOrganizationId }: OrgSwitcherProps) {
  const t = useTranslations("orgSwitcher");
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const [isSwitching, setIsSwitching] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    if (isOpen) document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  const active = memberships.find((m) => m.organizationId === activeOrganizationId) ?? memberships[0];

  async function handleSelect(organizationId: string) {
    if (organizationId === activeOrganizationId || isSwitching) {
      setIsOpen(false);
      return;
    }
    setIsSwitching(true);
    try {
      const response = await switchOrganization(organizationId);
      setToken(response.token);
      const user = getAuthUser();
      setAuthUser({
        userId: response.userId,
        email: response.email,
        firstName: response.firstName ?? user?.firstName,
        lastName: response.lastName ?? user?.lastName,
        organizationId: response.organizationId,
        role: response.role,
        memberships: response.memberships,
      });
      setIsOpen(false);
      // Refresh zodat alle server state opnieuw opgehaald wordt met nieuwe tenant.
      router.refresh();
      window.location.reload();
    } catch {
      setIsSwitching(false);
    }
  }

  if (memberships.length <= 1 || !active) return null;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        disabled={isSwitching}
        className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-gray-200 bg-white hover:bg-gray-50 transition-colors cursor-pointer disabled:opacity-50"
      >
        <span className="text-sm font-medium text-gray-800 max-w-[160px] truncate">
          {active.organizationName}
        </span>
        <ChevronsUpDown className="w-3.5 h-3.5 text-gray-400" />
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-64 rounded-lg border border-gray-100 bg-white shadow-lg z-50 overflow-hidden">
          <div className="px-3 py-2 text-xs font-semibold text-gray-400 uppercase tracking-wider border-b border-gray-100">
            {t("title")}
          </div>
          <ul className="py-1 max-h-80 overflow-y-auto">
            {memberships.map((m) => (
              <li key={m.organizationId}>
                <button
                  type="button"
                  onClick={() => handleSelect(m.organizationId)}
                  className="w-full flex items-center justify-between gap-2 px-3 py-2 text-left hover:bg-gray-50 transition-colors cursor-pointer"
                >
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-gray-800 truncate">
                      {m.organizationName}
                    </p>
                    <p className="text-xs text-gray-400">{m.role}</p>
                  </div>
                  {m.organizationId === active.organizationId && (
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
}
