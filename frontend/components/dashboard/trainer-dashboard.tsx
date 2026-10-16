"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  parse,
  startOfMonth,
  startOfWeek,
  subMonths,
} from "date-fns";
import { nl } from "date-fns/locale";
import { ChevronLeft, ChevronRight, MapPin, Users } from "lucide-react";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import { TrainerLessonDialog } from "@/components/dashboard/trainer-lesson-dialog";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import { getMyLessons, type MyLessonDto } from "@/lib/api/dashboard";
import { getAuthUser } from "@/lib/auth";
import { useClientValue } from "@/lib/hooks/use-client-value";
import { cn } from "@/lib/utils";

const ISO_DATE = "yyyy-MM-dd";
const MAX_CHIPS_PER_DAY = 2;

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function useLessonTitle() {
  const t = useTranslations("dashboard");
  return (lesson: MyLessonDto) =>
    lesson.seriesName ?? t("trainerStandaloneLesson");
}

// ─── Welkomstband ─────────────────────────────────────────────────────────────

function WelcomeBand({
  firstName,
  todayCount,
  today,
}: {
  firstName: string | undefined;
  todayCount: number | undefined;
  today: Date;
}) {
  const t = useTranslations("dashboard");

  return (
    <div className="bg-paper border border-rule rounded-xl px-5 py-4 mb-3.5">
      <Mono className="text-[10.5px] text-ink-3 uppercase tracking-[0.1em] block">
        {format(today, "EEEE d MMMM", { locale: nl })}
      </Mono>
      <h1 className="text-xl font-bold text-ink tracking-tight mt-1">
        {firstName
          ? t("trainerWelcome", { name: firstName })
          : t("trainerWelcomeNoName")}
      </h1>
      {todayCount !== undefined && (
        <p className="text-[12.5px] text-ink-3 mt-0.5">
          {t("trainerTodaySummary", { count: todayCount })}
        </p>
      )}
    </div>
  );
}

// ─── Lessen van vandaag ───────────────────────────────────────────────────────

function TodayCard({
  lessons,
  isLoading,
  onSelect,
}: {
  lessons: MyLessonDto[] | undefined;
  isLoading: boolean;
  onSelect: (lessonId: string) => void;
}) {
  const t = useTranslations("dashboard");
  const titleOf = useLessonTitle();

  return (
    <div className="bg-paper border border-rule rounded-xl overflow-hidden">
      <div className="px-[18px] py-3.5 border-b border-rule flex items-center justify-between">
        <h2 className="text-[13px] font-bold text-ink">
          {t("trainerTodayTitle")}
        </h2>
        {lessons && lessons.length > 0 && (
          <Mono className="text-[11px] text-ink-3">{lessons.length}</Mono>
        )}
      </div>

      {isLoading && (
        <div className="p-4 space-y-3 animate-pulse">
          {[1, 2].map((i) => (
            <div key={i} className="h-16 bg-canvas rounded-lg" />
          ))}
        </div>
      )}

      {!isLoading && lessons && lessons.length === 0 && (
        <div className="flex flex-col items-center justify-center py-12 px-6 text-center">
          <div className="w-14 h-14 rounded-full bg-tennis-green/[.08] flex items-center justify-center mb-4">
            <TennisBallEmptyIcon className="w-7 h-7" />
          </div>
          <p className="text-ink font-semibold text-sm mb-1">
            {t("trainerTodayEmptyTitle")}
          </p>
          <p className="text-ink-3 text-xs max-w-60 leading-relaxed">
            {t("trainerTodayEmptyDesc")}
          </p>
        </div>
      )}

      {!isLoading && lessons && lessons.length > 0 && (
        <ul className="divide-y divide-rule">
          {lessons.map((lesson) => (
            <li key={lesson.id}>
              <button
                type="button"
                onClick={() => onSelect(lesson.id)}
                className="flex w-full items-start gap-3.5 px-[18px] py-3.5 text-left hover:bg-canvas/60 transition-colors"
              >
                <div className="shrink-0 w-[62px] rounded-lg bg-tennis-green/10 py-1.5 text-center">
                  <Mono className="block text-[12.5px] font-bold text-tennis-green">
                    {lesson.startTime}
                  </Mono>
                  <Mono className="block text-[10px] text-ink-3">
                    {lesson.endTime}
                  </Mono>
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-[13px] font-semibold text-ink truncate">
                    {titleOf(lesson)}
                  </p>
                  <p className="text-[11px] text-ink-3 mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5">
                    {(lesson.courtName || lesson.tennisClubName) && (
                      <span className="inline-flex items-center gap-1">
                        <MapPin size={10} />
                        {[lesson.courtName, lesson.tennisClubName]
                          .filter(Boolean)
                          .join(" · ")}
                      </span>
                    )}
                    {lesson.enrolledCount !== null && (
                      <span className="inline-flex items-center gap-1">
                        <Users size={10} />
                        {t("trainerParticipants", {
                          count: lesson.enrolledCount,
                        })}
                      </span>
                    )}
                  </p>
                </div>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

// ─── Maandkalender ────────────────────────────────────────────────────────────

function MonthCalendar({
  today,
  onSelect,
}: {
  today: Date;
  onSelect: (lessonId: string) => void;
}) {
  const t = useTranslations("dashboard");
  const titleOf = useLessonTitle();
  const [month, setMonth] = useState(() => startOfMonth(today));

  // Volledige weken (ma–zo) zodat de randdagen van vorige/volgende maand ook gevuld zijn.
  const gridStart = startOfWeek(startOfMonth(month), { weekStartsOn: 1 });
  const gridEnd = endOfWeek(endOfMonth(month), { weekStartsOn: 1 });
  const days = eachDayOfInterval({ start: gridStart, end: gridEnd });
  const from = format(gridStart, ISO_DATE);
  const to = format(gridEnd, ISO_DATE);

  const {
    data: lessons,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["dashboard", "my-lessons", from, to],
    queryFn: () => getMyLessons(from, to),
  });

  const lessonsByDay = new Map<string, MyLessonDto[]>();
  for (const lesson of lessons ?? []) {
    const list = lessonsByDay.get(lesson.date) ?? [];
    list.push(lesson);
    lessonsByDay.set(lesson.date, list);
  }

  const weekdayLabels = days
    .slice(0, 7)
    .map((d) => capitalize(format(d, "EEEEEE", { locale: nl })));

  return (
    <div className="bg-paper border border-rule rounded-xl overflow-hidden">
      <div className="px-[18px] py-3.5 border-b border-rule flex items-center justify-between gap-3">
        <h2 className="text-[13px] font-bold text-ink">
          {capitalize(format(month, "LLLL yyyy", { locale: nl }))}
        </h2>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => setMonth(startOfMonth(today))}
            className="px-2.5 h-7 rounded-md border border-rule text-[11px] font-medium text-ink-3 hover:text-ink hover:bg-canvas transition-colors"
          >
            {t("trainerCalendarToday")}
          </button>
          <button
            type="button"
            onClick={() => setMonth((m) => subMonths(m, 1))}
            aria-label={t("trainerCalendarPrev")}
            className="h-7 w-7 grid place-items-center rounded-md border border-rule text-ink-3 hover:text-ink hover:bg-canvas transition-colors"
          >
            <ChevronLeft size={14} />
          </button>
          <button
            type="button"
            onClick={() => setMonth((m) => addMonths(m, 1))}
            aria-label={t("trainerCalendarNext")}
            className="h-7 w-7 grid place-items-center rounded-md border border-rule text-ink-3 hover:text-ink hover:bg-canvas transition-colors"
          >
            <ChevronRight size={14} />
          </button>
        </div>
      </div>

      {isError && (
        <p className="px-[18px] py-3 text-xs text-red-600">
          {t("trainerCalendarLoadError")}
        </p>
      )}

      <div className="grid grid-cols-7 border-b border-rule bg-canvas/50">
        {weekdayLabels.map((label) => (
          <div
            key={label}
            className="px-2 py-1.5 text-[10.5px] font-semibold uppercase tracking-[0.06em] text-ink-3 text-center sm:text-left"
          >
            {label}
          </div>
        ))}
      </div>

      <div className={cn("grid grid-cols-7", isLoading && "opacity-60")}>
        {days.map((day, i) => {
          const key = format(day, ISO_DATE);
          const dayLessons = lessonsByDay.get(key) ?? [];
          const inMonth = isSameMonth(day, month);
          const isToday = isSameDay(day, today);
          const visible = dayLessons.slice(0, MAX_CHIPS_PER_DAY);
          const extra = dayLessons.length - visible.length;

          return (
            <div
              key={key}
              className={cn(
                "min-h-[64px] sm:min-h-[92px] p-1.5 border-rule",
                i % 7 !== 0 && "border-l",
                i >= 7 && "border-t",
                !inMonth && "bg-canvas/40",
              )}
            >
              <span
                className={cn(
                  "inline-grid place-items-center h-6 min-w-6 rounded-full px-1 text-[11.5px] font-mono",
                  isToday
                    ? "bg-tennis-green text-tennis-lime font-bold"
                    : inMonth
                      ? "text-ink"
                      : "text-ink-3/60",
                )}
              >
                {format(day, "d")}
              </span>

              {/* Desktop: chips per les */}
              <div className="hidden sm:block mt-1 space-y-0.5">
                {visible.map((lesson) => (
                  <button
                    key={lesson.id}
                    type="button"
                    onClick={() => onSelect(lesson.id)}
                    title={`${lesson.startTime} – ${lesson.endTime} · ${titleOf(lesson)}`}
                    className="block w-full truncate rounded bg-tennis-green/10 px-1.5 py-0.5 text-left text-[10.5px] leading-tight text-tennis-green hover:bg-tennis-green/20 transition-colors"
                  >
                    <span className="font-mono font-semibold">
                      {lesson.startTime}
                    </span>{" "}
                    {titleOf(lesson)}
                  </button>
                ))}
                {extra > 0 && (
                  <p className="px-1.5 text-[10px] text-ink-3">
                    {t("trainerCalendarMore", { count: extra })}
                  </p>
                )}
              </div>

              {/* Mobiel: puntjes, de details staan in de kaart "vandaag" en de lijsten */}
              {dayLessons.length > 0 && (
                <div className="sm:hidden mt-1 flex flex-wrap gap-0.5">
                  {dayLessons.slice(0, 4).map((lesson) => (
                    <span
                      key={lesson.id}
                      aria-hidden
                      className="h-1.5 w-1.5 rounded-full bg-tennis-green"
                    />
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ─── Dashboard ────────────────────────────────────────────────────────────────

export function TrainerDashboard() {
  // Datum (als yyyy-MM-dd) en voornaam bestaan enkel client-side.
  const todayKey = useClientValue(() => format(new Date(), ISO_DATE));
  const firstName = useClientValue(() => getAuthUser()?.firstName ?? "");

  if (!todayKey) return null;
  return (
    <TrainerDashboardContent
      today={parse(todayKey, ISO_DATE, new Date())}
      firstName={firstName || undefined}
    />
  );
}

function TrainerDashboardContent({
  today,
  firstName,
}: {
  today: Date;
  firstName: string | undefined;
}) {
  const todayKey = format(today, ISO_DATE);

  const { data: todayLessons, isLoading } = useQuery({
    queryKey: ["dashboard", "my-lessons", todayKey, todayKey],
    queryFn: () => getMyLessons(todayKey, todayKey),
  });

  const [selectedLessonId, setSelectedLessonId] = useState<string | null>(null);

  return (
    <>
      <WelcomeBand
        firstName={firstName}
        todayCount={todayLessons?.length}
        today={today}
      />

      <div className="grid grid-cols-1 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.8fr)] gap-[18px] items-start">
        <TodayCard
          lessons={todayLessons}
          isLoading={isLoading}
          onSelect={setSelectedLessonId}
        />
        <MonthCalendar today={today} onSelect={setSelectedLessonId} />
      </div>

      <TrainerLessonDialog
        lessonId={selectedLessonId}
        onClose={() => setSelectedLessonId(null)}
      />
    </>
  );
}
