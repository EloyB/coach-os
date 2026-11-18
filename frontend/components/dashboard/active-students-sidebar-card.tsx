"use client";

import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getActiveStudents } from "@/lib/api/subscription";
import { activeStudentsQueryKey, getActiveStudentsUsage } from "@/lib/active-students";
import type { AuthUser } from "@/lib/auth";
import { formatDateNL } from "@/lib/date-utils";
import { cn } from "@/lib/utils";

/**
 * Teller in de sidenav met het aantal unieke actieve leerlingen in het lopende abonnementsjaar.
 * Enkel voor admins; bij laden of fout tonen we niets. Puur informatief, blokkeert niets.
 */
export function ActiveStudentsSidebarCard({ user }: { user: AuthUser | null }) {
  const t = useTranslations("activeStudents");
  const isAdmin = user?.role === "Admin";

  const { data } = useQuery({
    queryKey: activeStudentsQueryKey(user?.organizationId),
    queryFn: getActiveStudents,
    enabled: isAdmin,
    staleTime: 5 * 60_000,
    retry: false,
  });

  // Informatief: bij een ongeldig antwoord tonen we niets in plaats van de zijbalk te breken.
  if (!isAdmin || typeof data?.count !== "number" || typeof data.periodEnd !== "string") {
    return null;
  }

  const usage = getActiveStudentsUsage(data.count, data.limit);
  const exceeded = usage.level === "exceeded";

  return (
    <div
      role="status"
      data-testid="active-students-card"
      data-level={usage.level}
      className="relative z-10 mx-2.5 mb-2.5 rounded-lg bg-black/[.18] px-3 py-2.5"
    >
      <p
        className={cn(
          "text-[12px] font-semibold",
          usage.level === "normal" && "text-white",
          usage.level === "warning" && "text-amber-300",
          exceeded && "text-red-300",
        )}
      >
        {usage.limit !== null
          ? t("withLimit", { count: data.count, limit: usage.limit })
          : t("withoutLimit", { count: data.count })}
      </p>
      {usage.limit !== null && (
        <div
          data-testid="active-students-bar"
          className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-white/15"
        >
          <div
            className={cn(
              "h-full rounded-full transition-[width]",
              usage.level === "normal" && "bg-tennis-lime",
              usage.level === "warning" && "bg-amber-400",
              exceeded && "bg-red-400",
            )}
            style={{ width: `${usage.percentage}%` }}
          />
        </div>
      )}
      {exceeded && (
        <p className="mt-1 text-[10.5px] font-medium leading-snug text-red-300">
          {t("limitReached")}
        </p>
      )}
      <p className="mt-1 text-[10.5px] leading-snug text-white/55">
        {t("periodUntil", { date: formatDateNL(data.periodEnd) })}
      </p>
    </div>
  );
}
