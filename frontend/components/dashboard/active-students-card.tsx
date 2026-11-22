"use client";

import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getActiveStudents } from "@/lib/api/subscription";
import {
  activeStudentsQueryKey,
  getActiveStudentsUsage,
  type ActiveStudentsLevel,
} from "@/lib/active-students";
import type { AuthUser } from "@/lib/auth";
import { formatDateNL } from "@/lib/date-utils";
import { cn } from "@/lib/utils";

type ActiveStudentsCardVariant = "sidebar" | "page";

interface ActiveStudentsCardProps {
  user: AuthUser | null;
  variant: ActiveStudentsCardVariant;
}

interface VariantStyles {
  testId: string;
  barTestId: string;
  container: string;
  count: Record<ActiveStudentsLevel, string>;
  track: string;
  limitReached: string;
  period: string;
  countSize: string;
  noteSize: string;
}

const VARIANT_STYLES: Record<ActiveStudentsCardVariant, VariantStyles> = {
  sidebar: {
    testId: "active-students-card",
    barTestId: "active-students-bar",
    container: "relative z-10 mx-2.5 mb-2.5 rounded-lg bg-black/[.18] px-3 py-2.5",
    count: { normal: "text-white", warning: "text-amber-300", exceeded: "text-red-300" },
    track: "bg-white/15",
    limitReached: "text-red-300",
    period: "text-white/55",
    countSize: "text-[12px]",
    noteSize: "text-[10.5px]",
  },
  page: {
    testId: "active-students-page-card",
    barTestId: "active-students-page-bar",
    container: "mb-6 rounded-xl bg-white px-6 py-4 shadow-sm shadow-gray-100",
    count: { normal: "text-gray-800", warning: "text-amber-600", exceeded: "text-red-600" },
    track: "bg-gray-100",
    limitReached: "text-red-600",
    period: "text-gray-400",
    countSize: "text-sm",
    noteSize: "text-xs",
  },
};

const BAR_COLORS: Record<ActiveStudentsLevel, string> = {
  normal: "bg-tennis-lime",
  warning: "bg-amber-400",
  exceeded: "bg-red-400",
};

const PAGE_BAR_COLORS: Record<ActiveStudentsLevel, string> = {
  normal: "bg-tennis-green",
  warning: "bg-amber-400",
  exceeded: "bg-red-500",
};

/**
 * Teller met het aantal unieke actieve leerlingen in het lopende abonnementsjaar.
 * Enkel voor admins; bij laden of fout tonen we niets. Puur informatief, blokkeert niets.
 */
export function ActiveStudentsCard({ user, variant }: ActiveStudentsCardProps) {
  const t = useTranslations("activeStudents");
  const isAdmin = user?.role === "Admin";

  const { data } = useQuery({
    queryKey: activeStudentsQueryKey(user?.organizationId),
    queryFn: getActiveStudents,
    enabled: isAdmin,
    staleTime: 5 * 60_000,
    retry: false,
  });

  // Informatief: bij een ongeldig antwoord tonen we niets in plaats van de pagina te breken.
  if (!isAdmin || typeof data?.count !== "number" || typeof data.periodEnd !== "string") {
    return null;
  }

  const styles = VARIANT_STYLES[variant];
  const barColors = variant === "page" ? PAGE_BAR_COLORS : BAR_COLORS;
  const usage = getActiveStudentsUsage(data.count, data.limit);
  const exceeded = usage.level === "exceeded";

  return (
    <div data-testid={styles.testId} data-level={usage.level} className={styles.container}>
      <p className={cn("font-semibold", styles.countSize, styles.count[usage.level])}>
        {usage.limit !== null
          ? t("withLimit", { count: data.count, limit: usage.limit })
          : t("withoutLimit", { count: data.count })}
      </p>
      {usage.limit !== null && (
        <div
          role="progressbar"
          aria-label={t("progressLabel")}
          aria-valuemin={0}
          aria-valuemax={usage.limit}
          aria-valuenow={Math.min(data.count, usage.limit)}
          data-testid={styles.barTestId}
          className={cn("mt-1.5 h-1.5 w-full overflow-hidden rounded-full", styles.track)}
        >
          <div
            className={cn("h-full rounded-full transition-[width]", barColors[usage.level])}
            style={{ width: `${usage.percentage}%` }}
          />
        </div>
      )}
      {exceeded && (
        <p className={cn("mt-1 font-medium leading-snug", styles.noteSize, styles.limitReached)}>
          {t("limitReached")}
        </p>
      )}
      <p className={cn("mt-1 leading-snug", styles.noteSize, styles.period)}>
        {t("periodUntil", { date: formatDateNL(data.periodEnd) })}
      </p>
    </div>
  );
}
