"use client";

import { useQuery } from "@tanstack/react-query";
import { getOrganizationSettings } from "@/lib/api/organizationSettings";
import { getAuthUser } from "@/lib/auth";
import { navItems, type NavItem } from "@/lib/nav-items";
import { useClientValue } from "@/lib/hooks/use-client-value";

/**
 * De navigatie-items die bij de rol van de ingelogde gebruiker horen. Een admin die zelf lesgeeft
 * (organisatie-instelling AdminsActAsTrainers) krijgt daarnaast de trainerpagina voor de eigen beschikbaarheid.
 */
export function useVisibleNavItems(): NavItem[] {
  const role = useClientValue(() => getAuthUser()?.role ?? null);

  const { data: settings } = useQuery({
    queryKey: ["organizationSettings"],
    queryFn: getOrganizationSettings,
    enabled: role === "Admin",
    staleTime: 5 * 60 * 1000,
  });
  const adminActsAsTrainer = role === "Admin" && settings?.adminsActAsTrainers === true;

  return navItems
    .filter(
      (item) =>
        (!item.adminOnly || role === "Admin") &&
        (!item.trainerOnly ||
          role === "Trainer" ||
          (item.adminActsAsTrainer === true && adminActsAsTrainer)),
    )
    .map((item) =>
      role === "Admin" && item.trainerOnly && item.adminLabel
        ? { ...item, label: item.adminLabel, shortLabel: item.adminShortLabel ?? item.adminLabel }
        : item,
    );
}
