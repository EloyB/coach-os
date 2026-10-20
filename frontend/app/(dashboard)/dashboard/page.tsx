"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { format, parse } from "date-fns";
import { ArrowRight, BookOpen, Ticket, UserPlus } from "lucide-react";
import { StatStrip } from "@/components/ui/stat-strip";
import { getDashboardSummary } from "@/lib/api/dashboard";
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

  const actions = [
    {
      href: "/dashboard/lessons/new",
      icon: BookOpen,
      title: t("actionNewSeriesTitle"),
      description: t("actionNewSeriesDesc"),
    },
    {
      href: "/dashboard/standalone-lessons/new",
      icon: Ticket,
      title: t("actionNewLessonTitle"),
      description: t("actionNewLessonDesc"),
    },
    {
      href: "/dashboard/trainers?invite=1",
      icon: UserPlus,
      title: t("actionInviteTrainerTitle"),
      description: t("actionInviteTrainerDesc"),
    },
  ];

  return (
    // Op groot scherm scrolt de pagina niet: de agenda krimpt mee met de overgebleven ruimte.
    <div className="flex min-h-full flex-col xl:h-full xl:overflow-hidden">
      <h1 className="sr-only">{t("title")}</h1>

      {/* Welkomstmodal voor nieuwe admins; de stappen zelf staan in de sidenav. */}
      <WelcomeModal shouldShow={isAdmin && (onboarding?.shouldShow ?? false)} />

      {/* Dark hero stat strip */}
      {isLoading ? (
        <div className="bg-ink rounded-xl p-4 mb-3.5 animate-pulse h-[72px]" />
      ) : (
        <StatStrip items={statItems} className="mb-3.5" />
      )}

      {/* Snelle acties */}
      <div className="mb-3.5 grid grid-cols-1 gap-3.5 sm:grid-cols-3">
        {actions.map(({ href, icon: Icon, title, description }) => (
          <Link
            key={href}
            href={href}
            className="group flex items-center gap-3.5 rounded-xl border border-rule bg-paper p-3.5 transition-colors hover:border-ink/40 hover:bg-canvas/60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ink"
          >
            <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-tennis-green/10 text-tennis-green">
              <Icon size={18} aria-hidden />
            </span>
            <span className="min-w-0 flex-1">
              <span className="block text-[13px] font-semibold text-ink">
                {title}
              </span>
              <span className="mt-0.5 block text-[11px] text-ink-3">
                {description}
              </span>
            </span>
            <ArrowRight
              size={15}
              aria-hidden
              className="shrink-0 text-ink-3 transition-transform group-hover:translate-x-0.5"
            />
          </Link>
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
