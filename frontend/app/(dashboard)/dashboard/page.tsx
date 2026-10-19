"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Plus, Clock, Calendar } from "lucide-react";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import { StatStrip } from "@/components/ui/stat-strip";
import { Mono } from "@/components/ui/mono";
import {
  getDashboardSummary,
  getDashboardMetrics,
} from "@/lib/api/dashboard";
import { useTranslations } from "next-intl";
import { WelcomeModal } from "@/components/dashboard/onboarding/welcome-modal";
import { getAuthUser, isHeadTrainerViewer } from "@/lib/auth";
import { getOnboardingState } from "@/lib/api/onboarding";
import { HeadTrainerDashboard } from "@/components/dashboard/head-trainer-dashboard";
import { TrainerDashboard } from "@/components/dashboard/trainer-dashboard";
import { useClientValue } from "@/lib/hooks/use-client-value";

function formatDate(dateStr: string): string {
  const [year, month, day] = dateStr.split("-");
  return `${day}/${month}/${year}`;
}

function formatTime(timeStr: string): string {
  return timeStr.substring(0, 5);
}

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
    <>
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

      {/* Komende lessen */}
      <div>
        {/* Today schedule */}
        <div className="bg-paper border border-rule rounded-xl overflow-hidden">
          <div className="px-[18px] py-3.5 border-b border-rule flex items-center justify-between">
            <h2 className="text-[13px] font-bold text-ink">
              {t("todayTitle", { count: summary?.upcomingLessons?.length ?? 0 })}
            </h2>
          </div>

          {isLoading && (
            <div className="p-6 space-y-3 animate-pulse">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-12 bg-canvas rounded-lg" />
              ))}
            </div>
          )}

          {!isLoading && (!summary?.upcomingLessons || summary.upcomingLessons.length === 0) && (
            <div className="flex flex-col items-center justify-center py-14 px-6 text-center">
              <div className="w-14 h-14 rounded-full bg-tennis-green/[.08] flex items-center justify-center mb-4">
                <TennisBallEmptyIcon className="w-7 h-7" />
              </div>
              <p className="text-ink font-semibold text-sm mb-1">
                {t("noLessonsTitle")}
              </p>
              <p className="text-ink-3 text-xs max-w-60 leading-relaxed mb-5">
                {t("noLessonsDesc")}
              </p>
              <Link
                href="/dashboard/lessons/new"
                className="inline-flex items-center gap-2 px-4 py-2.5 bg-ink text-white text-xs font-semibold rounded-lg"
              >
                <Plus size={13} />
                {t("planLesson")}
              </Link>
            </div>
          )}

          {!isLoading && summary?.upcomingLessons && summary.upcomingLessons.length > 0 && (
            <div className="divide-y divide-rule">
              {summary.upcomingLessons.map((lesson) => (
                <div key={lesson.id} className="px-[18px] py-3.5 flex items-center justify-between">
                  <div className="flex items-center gap-3.5">
                    <div className="w-10 h-10 rounded-lg bg-tennis-green/10 flex items-center justify-center">
                      <Calendar size={16} className="text-tennis-green" />
                    </div>
                    <div>
                      <p className="text-[12.5px] font-semibold text-ink">
                        {lesson.seriesName}
                      </p>
                      <p className="text-[11px] text-ink-3">
                        {lesson.trainerName} · {lesson.courtName}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <Mono className="text-[12px] font-medium text-ink">
                      {formatDate(lesson.date)}
                    </Mono>
                    <p className="text-[11px] text-ink-3 flex items-center gap-1 justify-end">
                      <Clock size={10} />
                      <Mono>{formatTime(lesson.startTime)} – {formatTime(lesson.endTime)}</Mono>
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </>
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
