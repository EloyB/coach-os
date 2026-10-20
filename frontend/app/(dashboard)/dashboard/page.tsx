"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { format, parse } from "date-fns";
import { StatStrip } from "@/components/ui/stat-strip";
import {
  getDashboardSummary,
  getDashboardMetrics,
} from "@/lib/api/dashboard";
import { useTranslations } from "next-intl";
import { WelcomeModal } from "@/components/dashboard/onboarding/welcome-modal";
import { getAuthUser, isHeadTrainerViewer } from "@/lib/auth";
import { getOnboardingState } from "@/lib/api/onboarding";
import { HeadTrainerDashboard } from "@/components/dashboard/head-trainer-dashboard";
import {
  LessonAgenda,
  TrainerDashboard,
} from "@/components/dashboard/trainer-dashboard";
import { TrainerLessonDialog } from "@/components/dashboard/trainer-lesson-dialog";
import { useClientValue } from "@/lib/hooks/use-client-value";

function AdminDashboard() {
  const t = useTranslations("dashboard");
  const [isAdmin, setIsAdmin] = useState(false);

  // getAuthUser leest localStorage en mag pas na mount draaien om SSR-hydration mismatches
  // te vermijden. Tot dan houden we isAdmin op false zodat de checklist niets rendert.
  useEffect(() => {
    setIsAdmin(getAuthUser()?.role === "Admin");
  }, []);

  // Vooruit ophalen zodat de WelcomeModal direct kan beslissen of hij open mag.
  const { data: onboarding } = useQuery({
    queryKey: ["onboarding", "state"],
    queryFn: getOnboardingState,
    enabled: isAdmin,
    staleTime: 30_000,
  });

  const todayKey = useClientValue(() => format(new Date(), "yyyy-MM-dd"));
  const userId = useClientValue(() => getAuthUser()?.userId ?? "") ?? "";
  const [selectedLessonId, setSelectedLessonId] = useState<string | null>(null);

  const { data: summary, isLoading } = useQuery({
    queryKey: ["dashboard"],
    queryFn: getDashboardSummary,
  });

  const { data: metrics } = useQuery({
    queryKey: ["dashboard", "metrics"],
    queryFn: () => getDashboardMetrics(),
  });

  const statItems = [
    {
      value: String(summary?.lessonsTodayCount ?? 0),
      label: t("statLessonsToday", { count: summary?.lessonsTodayCount ?? 0 }),
      color: "text-tennis-lime",
    },
    {
      value: String(summary?.lessonsThisWeekCount ?? 0),
      label: t("statThisWeek"),
    },
    {
      value: `${summary?.totalEnrollmentCount ?? 0}`,
      label: t("statEnrollments", { count: summary?.totalEnrollmentCount ?? 0 }),
    },
    {
      value: String(summary?.activeTrainerCount ?? 0),
      label: t("statTrainers", { count: summary?.activeTrainerCount ?? 0 }),
      mobileHidden: true,
    },
    {
      value: String(summary?.activeSeriesCount ?? 0),
      label: t("statSeries", { count: summary?.activeSeriesCount ?? 0 }),
    },
  ];

  const lessonsData = metrics?.lessons?.map((m) => m.value) ?? [];
  const occupancyData = metrics?.occupancyPct?.map((m) => m.value) ?? [];
  const outstandingData = metrics?.outstandingCents?.map((m) => m.value) ?? [];

  const lastLessons = lessonsData.length > 0 ? lessonsData[lessonsData.length - 1] : 0;
  const lastOccupancy = occupancyData.length > 0 ? occupancyData[occupancyData.length - 1] : 0;
  const lastOutstandingCents = outstandingData.length > 0 ? outstandingData[outstandingData.length - 1] : 0;
  const lastOutstandingEur = (lastOutstandingCents / 100).toFixed(0);

  return (
    <div className="flex min-h-full flex-col">
      <h1 className="sr-only">{t("title")}</h1>

      {/* Welkomstmodal voor nieuwe admins; de stappen zelf staan in de sidenav. */}
      <WelcomeModal shouldShow={isAdmin && (onboarding?.shouldShow ?? false)} />

      {/* Dark hero stat strip */}
      {isLoading ? (
        <div className="bg-ink rounded-xl p-4 mb-3.5 animate-pulse h-[72px]" />
      ) : (
        <StatStrip items={statItems} className="mb-3.5" />
      )}

      {/* Weekcijfers */}
      <div className="mb-3.5 grid grid-cols-1 gap-3.5 sm:grid-cols-3">
        {[
          {
            label: t("sparkLessons"),
            val: String(lessonsData.length > 0 ? lastLessons : (summary?.lessonsThisWeekCount ?? 0)),
            sub: t("sparkLessonsVsPrev"),
          },
          {
            label: t("sparkOccupancy"),
            val: occupancyData.length > 0 ? `${lastOccupancy}%` : "\u2014",
            sub: t("sparkOccupancyPct"),
          },
          {
            label: t("sparkOutstanding"),
            val: outstandingData.length > 0 ? `\u20AC${lastOutstandingEur}` : "\u2014",
            sub: t("sparkOutstandingLabel"),
          },
        ].map((card) => (
          <div key={card.label} className="bg-paper border border-rule rounded-xl p-3.5">
            <p className="text-[10.5px] text-ink-3 font-medium uppercase tracking-[0.05em]">
              {card.label}
            </p>
            <p className="text-[22px] font-bold text-ink tracking-tight mt-1 tabular-nums">
              {card.val}
            </p>
            <p className="text-[10.5px] text-ink-3 mt-0.5">{card.sub}</p>
          </div>
        ))}
      </div>

      {/* Agenda: alle ingeplande lessen van de organisatie, met een kleur per trainer */}
      {todayKey && (
        <LessonAgenda
          today={parse(todayKey, "yyyy-MM-dd", new Date())}
          userId={userId}
          allSource="organization"
          allLabel={t("trainerScopeOrganization")}
          onSelectLesson={setSelectedLessonId}
          className="xl:min-h-[560px]"
        />
      )}
      <TrainerLessonDialog
        lessonId={selectedLessonId}
        onClose={() => setSelectedLessonId(null)}
      />
    </div>
  );
}

export default function DashboardPage() {
  // De rol zit in localStorage en is pas na mount bekend. Tot dan renderen we niets, zodat een
  // trainer nooit even de (organisatiebrede) beheerdersqueries afvuurt.
  // Een hoofdtrainer is een Trainer met minstens één hoofdtrainer-club en krijgt een eigen dashboard.
  const dashboard = useClientValue(() => {
    const role = getAuthUser()?.role ?? "";
    if (role === "Trainer") return isHeadTrainerViewer() ? "head-trainer" : "trainer";
    return role;
  });

  if (dashboard === null) return null;
  if (dashboard === "head-trainer") return <HeadTrainerDashboard />;
  if (dashboard === "trainer") return <TrainerDashboard />;
  return <AdminDashboard />;
}
