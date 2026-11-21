"use client";

import { useEffect, useRef } from "react";
import { usePathname, useSearchParams } from "next/navigation";
import { getAuthUser } from "@/lib/auth";
import { ORGANIZATION_PARAM, organizationToSwitchTo } from "@/lib/organization-param";
import { switchToOrganization } from "@/lib/organization-switch";

/**
 * Opent de organisatie uit `?org=` (bv. de exportlink in de verwijdermail), ook voor een admin
 * van meerdere organisaties. Zonder lidmaatschap blijft alles zoals het is. Rendert niets.
 */
export function OrganizationParamSwitch() {
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const started = useRef(false);

  useEffect(() => {
    const requested = searchParams.get(ORGANIZATION_PARAM);
    if (!requested || started.current) return;
    started.current = true;

    const params = new URLSearchParams(searchParams.toString());
    params.delete(ORGANIZATION_PARAM);
    const query = params.toString();
    const cleanUrl = query ? `${pathname}?${query}` : pathname;

    const target = organizationToSwitchTo(requested, getAuthUser());
    if (!target) {
      window.history.replaceState(null, "", cleanUrl);
      return;
    }

    switchToOrganization(target)
      .then(() => window.location.replace(cleanUrl))
      .catch(() => window.history.replaceState(null, "", cleanUrl));
  }, [searchParams, pathname]);

  return null;
}
