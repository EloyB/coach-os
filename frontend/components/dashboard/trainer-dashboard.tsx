"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
  addMonths,
  differenceInCalendarDays,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  parse,
  parseISO,
  startOfMonth,
  startOfWeek,
  subMonths,
} from "date-fns";
import { nl } from "date-fns/locale";
import { ChevronLeft, ChevronRight, MapPin, Tent, Users } from "lucide-react";
import { useTranslations } from "next-intl";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";
import { TrainerLessonDialog } from "@/components/dashboard/trainer-lesson-dialog";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import {
  getMyCampDays,
  getMyLessons,
  getMyOverview,
  type MyCampDayDto,
  type MyLessonDto,
  type MyOverviewDto,
} from "@/lib/api/dashboard";
import { useClientValue } from "@/lib/hooks/use-client-value";
import { cn } from "@/lib/utils";

const ISO_DATE = "yyyy-MM-dd";
const MAX_CHIPS_PER_DAY = 2;

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function formatHours(hours: number): string {
  return hours.toLocaleString("nl-BE", { maximumFractionDigits: 1 });
}

function useLessonTitle() {
  const t = useTranslations("dashboard");
  return (lesson: MyLessonDto) =>
    lesson.seriesName ?? t("trainerStandaloneLesson");
}

// ─── Lessen en kampdagen op één tijdlijn ─────────────────────────────────────

type AgendaItem =
  | {
      kind: "lesson";
      key: string;
      date: string;
      startTime: string;
      lesson: MyLessonDto;
    }
  | {
      kind: "camp";
      key: string;
      date: string;
      startTime: string;
      camp: MyCampDayDto;
    };

/** Voegt lessen en kampdagen samen, chronologisch (datum, dan starttijd). */
function buildAgenda(
  lessons: MyLessonDto[] | undefined,
  campDays: MyCampDayDto[] | undefined,
): AgendaItem[] {
  const items: AgendaItem[] = [
    ...(lessons ?? []).map((lesson): AgendaItem => ({
      kind: "lesson",
      key: `lesson-${lesson.id}`,
      date: lesson.date,
      startTime: lesson.startTime,
      lesson,
    })),
    ...(campDays ?? []).map((camp, i): AgendaItem => ({
      kind: "camp",
      key: `camp-${camp.campId}-${camp.date}-${i}`,
      date: camp.date,
      startTime: camp.startTime,
      camp,
    })),
  ];
  return items.sort((a, b) =>
    `${a.date} ${a.startTime}`.localeCompare(`${b.date} ${b.startTime}`),
  );
}

// ─── Volgende les ─────────────────────────────────────────────────────────────

function NextLessonCard({
  overview,
  now,
  onSelect,
}: {
  overview: MyOverviewDto | undefined;
  now: Date;
  onSelect: (lessonId: string) => void;
}) {
  const t = useTranslations("dashboard");
  const titleOf = useLessonTitle();
  const lesson = overview?.nextLesson;

  // Zwart zoals de statistiekenband op het admin dashboard, zodat de volgende les meteen opvalt.
  const shell = "bg-ink text-white rounded-xl p-[18px] md:col-span-2";
  const mutedLabel = "text-[#a8a195]";

  if (!overview) {
    return <div className={cn(shell, "h-[132px] animate-pulse")} />;
  }

  if (!lesson) {
    return (
      <div className={shell}>
        <SlashLabel className={mutedLabel}>
          {t("trainerNextLessonLabel")}
        </SlashLabel>
        <p className="mt-2 text-sm font-bold text-white">
          {t("trainerNextLessonEmptyTitle")}
        </p>
        <p className={cn("mt-0.5 text-xs", mutedLabel)}>
          {t("trainerNextLessonEmptyDesc")}
        </p>
      </div>
    );
  }

  const start = parse(
    `${lesson.date} ${lesson.startTime}`,
    `${ISO_DATE} HH:mm`,
    now,
  );
  const end = parse(
    `${lesson.date} ${lesson.endTime}`,
    `${ISO_DATE} HH:mm`,
    now,
  );
  const dayDiff = differenceInCalendarDays(parseISO(lesson.date), now);

  const whenLabel =
    dayDiff === 0
      ? t("trainerNextToday")
      : dayDiff === 1
        ? t("trainerNextTomorrow")
        : capitalize(
            format(parseISO(lesson.date), "EEEE d MMMM", { locale: nl }),
          );

  const minutesUntil = Math.round((start.getTime() - now.getTime()) / 60_000);
  let countdown: string;
  if (now >= start && now < end) {
    countdown = t("trainerNextInProgress");
  } else if (dayDiff >= 2) {
    countdown = t("trainerNextInDays", { count: dayDiff });
  } else if (minutesUntil < 60) {
    countdown = t("trainerNextInMinutes", {
      minutes: Math.max(minutesUntil, 1),
    });
  } else {
    const hours = Math.floor(minutesUntil / 60);
    const minutes = minutesUntil % 60;
    countdown =
      minutes === 0
        ? t("trainerNextInHoursOnly", { hours })
        : t("trainerNextInHours", { hours, minutes });
  }

  return (
    <button
      type="button"
      onClick={() => onSelect(lesson.id)}
      className={cn(
        shell,
        "flex flex-col justify-start text-left hover:bg-ink/90 transition-colors",
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <SlashLabel className={mutedLabel}>
          {t("trainerNextLessonLabel")}
        </SlashLabel>
        <span className="shrink-0 rounded-full bg-tennis-lime px-2 py-0.5 text-[10.5px] font-bold text-ink">
          {countdown}
        </span>
      </div>
      <p className="mt-2 text-[17px] font-bold tracking-tight text-white truncate">
        {titleOf(lesson)}
      </p>
      <p className="mt-0.5 text-[12.5px] text-white/80">
        {whenLabel} ·{" "}
        <Mono className="font-medium text-tennis-lime">
          {lesson.startTime} – {lesson.endTime}
        </Mono>
      </p>
      <p className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-[11.5px] text-[#a8a195]">
        {(lesson.courtName || lesson.tennisClubName) && (
          <span className="inline-flex items-center gap-1">
            <MapPin size={11} />
            {[lesson.courtName, lesson.tennisClubName]
              .filter(Boolean)
              .join(" · ")}
          </span>
        )}
        {lesson.enrolledCount !== null && (
          <span className="inline-flex items-center gap-1">
            <Users size={11} />
            {t("trainerParticipants", { count: lesson.enrolledCount })}
          </span>
        )}
      </p>
    </button>
  );
}

// ─── Uren ─────────────────────────────────────────────────────────────────────

function HoursCard({ overview }: { overview: MyOverviewDto | undefined }) {
  const t = useTranslations("dashboard");
  const shell = "bg-paper border border-rule rounded-xl p-[18px]";

  if (!overview)
    return <div className={cn(shell, "h-[132px] animate-pulse")} />;

  const {
    hoursThisWeek,
    hoursThisMonth,
    weeklyCapacityHours: capacity,
  } = overview;
  const pct =
    capacity > 0
      ? Math.min(100, Math.round((hoursThisWeek / capacity) * 100))
      : 0;
  const over = capacity > 0 && hoursThisWeek > capacity;

  return (
    <div className={shell}>
      <p className="text-[10.5px] font-medium uppercase tracking-[0.05em] text-ink-3">
        {t("trainerHoursTitle")}
      </p>
      <p className="mt-1 text-[26px] font-bold leading-none tracking-tight text-ink tabular-nums">
        {formatHours(hoursThisWeek)}
        <span className="ml-0.5 text-[15px] font-semibold text-ink-3">
          {t("trainerHoursUnit")}
        </span>
      </p>

      {capacity > 0 && (
        <div className="mt-3">
          <div className="h-[5px] overflow-hidden rounded-full bg-gray-100">
            <div
              className={cn(
                "h-full rounded-full transition-all",
                over ? "bg-amber-500" : "bg-tennis-green",
              )}
              style={{ width: `${pct}%` }}
            />
          </div>
          <p
            className={cn(
              "mt-1 text-[10.5px]",
              over ? "font-medium text-amber-600" : "text-ink-3",
            )}
          >
            {over
              ? t("trainerHoursOverCapacity", { capacity })
              : t("trainerHoursOfCapacity", { capacity })}
          </p>
        </div>
      )}

      <p className="mt-2.5 border-t border-dashed border-rule pt-2 text-[11px] text-ink-3">
        {t("trainerHoursMonth", { hours: formatHours(hoursThisMonth) })}
      </p>
    </div>
  );
}

// ─── Lessen van vandaag ───────────────────────────────────────────────────────

function TodayCard({
  items,
  isLoading,
  onSelect,
}: {
  items: AgendaItem[];
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
        {items.length > 0 && (
          <Mono className="text-[11px] text-ink-3">{items.length}</Mono>
        )}
      </div>

      {isLoading && (
        <div className="p-4 space-y-3 animate-pulse">
          {[1, 2].map((i) => (
            <div key={i} className="h-16 bg-canvas rounded-lg" />
          ))}
        </div>
      )}

      {!isLoading && items.length === 0 && (
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

      {!isLoading && items.length > 0 && (
        <ul className="divide-y divide-rule">
          {items.map((item) =>
            item.kind === "lesson" ? (
              <li key={item.key}>
                <button
                  type="button"
                  onClick={() => onSelect(item.lesson.id)}
                  className="flex w-full items-start gap-3.5 px-[18px] py-3.5 text-left hover:bg-canvas/60 transition-colors"
                >
                  <div className="shrink-0 w-[62px] rounded-lg bg-tennis-green/10 py-1.5 text-center">
                    <Mono className="block text-[12.5px] font-bold text-tennis-green">
                      {item.lesson.startTime}
                    </Mono>
                    <Mono className="block text-[10px] text-ink-3">
                      {item.lesson.endTime}
                    </Mono>
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-[13px] font-semibold text-ink truncate">
                      {titleOf(item.lesson)}
                    </p>
                    <p className="text-[11px] text-ink-3 mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5">
                      {(item.lesson.courtName ||
                        item.lesson.tennisClubName) && (
                        <span className="inline-flex items-center gap-1">
                          <MapPin size={10} />
                          {[item.lesson.courtName, item.lesson.tennisClubName]
                            .filter(Boolean)
                            .join(" · ")}
                        </span>
                      )}
                      {item.lesson.enrolledCount !== null && (
                        <span className="inline-flex items-center gap-1">
                          <Users size={10} />
                          {t("trainerParticipants", {
                            count: item.lesson.enrolledCount,
                          })}
                        </span>
                      )}
                    </p>
                  </div>
                </button>
              </li>
            ) : (
              <li key={item.key}>
                <Link
                  href={`/dashboard/camps/${item.camp.campId}`}
                  className="flex w-full items-start gap-3.5 px-[18px] py-3.5 text-left hover:bg-canvas/60 transition-colors"
                >
                  <div className="shrink-0 w-[62px] rounded-lg bg-amber-50 py-1.5 text-center">
                    <Mono className="block text-[12.5px] font-bold text-amber-700">
                      {item.camp.startTime}
                    </Mono>
                    <Mono className="block text-[10px] text-ink-3">
                      {item.camp.endTime}
                    </Mono>
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-[13px] font-semibold text-ink truncate">
                      {item.camp.campName}
                    </p>
                    <p className="text-[11px] text-ink-3 mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5">
                      <span className="inline-flex items-center gap-1 text-amber-700">
                        <Tent size={10} />
                        {t("trainerCampLabel")}
                      </span>
                      {item.camp.tennisClubName && (
                        <span className="inline-flex items-center gap-1">
                          <MapPin size={10} />
                          {item.camp.tennisClubName}
                        </span>
                      )}
                    </p>
                  </div>
                </Link>
              </li>
            ),
          )}
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

  const { data: campDays } = useQuery({
    queryKey: ["dashboard", "my-camp-days", from, to],
    queryFn: () => getMyCampDays(from, to),
  });

  const itemsByDay = new Map<string, AgendaItem[]>();
  for (const item of buildAgenda(lessons, campDays)) {
    const list = itemsByDay.get(item.date) ?? [];
    list.push(item);
    itemsByDay.set(item.date, list);
  }

  const weekdayLabels = days
    .slice(0, 7)
    .map((d) => capitalize(format(d, "EEEEEE", { locale: nl })));

  return (
    <div className="flex h-full flex-col overflow-hidden rounded-xl border border-rule bg-paper">
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

      <div className="grid grid-cols-[repeat(7,minmax(0,1fr))] border-b border-rule bg-canvas/50">
        {weekdayLabels.map((label) => (
          <div
            key={label}
            className="px-2 py-1.5 text-[10.5px] font-semibold uppercase tracking-[0.06em] text-ink-3 text-center sm:text-left"
          >
            {label}
          </div>
        ))}
      </div>

      <div
        className={cn(
          "grid flex-1 grid-cols-[repeat(7,minmax(0,1fr))] auto-rows-fr",
          isLoading && "opacity-60",
        )}
      >
        {days.map((day, i) => {
          const key = format(day, ISO_DATE);
          const dayItems = itemsByDay.get(key) ?? [];
          const inMonth = isSameMonth(day, month);
          const isToday = isSameDay(day, today);
          const visible = dayItems.slice(0, MAX_CHIPS_PER_DAY);
          const extra = dayItems.length - visible.length;

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

              {/* Desktop: chips per les of kampdag */}
              <div className="hidden sm:block mt-1 space-y-0.5">
                {visible.map((item) =>
                  item.kind === "lesson" ? (
                    <button
                      key={item.key}
                      type="button"
                      onClick={() => onSelect(item.lesson.id)}
                      title={`${item.lesson.startTime} – ${item.lesson.endTime} · ${titleOf(item.lesson)}`}
                      className="block w-full truncate rounded bg-tennis-green/10 px-1.5 py-0.5 text-left text-[10.5px] leading-tight text-tennis-green hover:bg-tennis-green/20 transition-colors"
                    >
                      <span className="font-mono font-semibold">
                        {item.lesson.startTime}
                      </span>{" "}
                      {titleOf(item.lesson)}
                    </button>
                  ) : (
                    <Link
                      key={item.key}
                      href={`/dashboard/camps/${item.camp.campId}`}
                      title={`${item.camp.startTime} – ${item.camp.endTime} · ${item.camp.campName}`}
                      className="block w-full truncate rounded bg-amber-50 px-1.5 py-0.5 text-left text-[10.5px] leading-tight text-amber-800 hover:bg-amber-100 transition-colors"
                    >
                      <span className="font-mono font-semibold">
                        {item.camp.startTime}
                      </span>{" "}
                      {item.camp.campName}
                    </Link>
                  ),
                )}
                {extra > 0 && (
                  <p className="px-1.5 text-[10px] text-ink-3">
                    {t("trainerCalendarMore", { count: extra })}
                  </p>
                )}
              </div>

              {/* Mobiel: puntjes, de details staan in de kaart "vandaag" en de lijsten */}
              {dayItems.length > 0 && (
                <div className="sm:hidden mt-1 flex flex-wrap gap-0.5">
                  {dayItems.slice(0, 4).map((item) => (
                    <span
                      key={item.key}
                      aria-hidden
                      className={cn(
                        "h-1.5 w-1.5 rounded-full",
                        item.kind === "lesson"
                          ? "bg-tennis-green"
                          : "bg-amber-500",
                      )}
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
  // De datum (als yyyy-MM-dd) bestaat enkel client-side.
  const todayKey = useClientValue(() => format(new Date(), ISO_DATE));

  if (!todayKey) return null;
  return (
    <TrainerDashboardContent today={parse(todayKey, ISO_DATE, new Date())} />
  );
}

function TrainerDashboardContent({ today }: { today: Date }) {
  const t = useTranslations("dashboard");
  const todayKey = format(today, ISO_DATE);

  // Tikt elke minuut zodat afteltijden ("over 2u 15m") bijblijven.
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 60_000);
    return () => clearInterval(id);
  }, []);

  const { data: overview } = useQuery({
    queryKey: ["dashboard", "my-overview"],
    queryFn: getMyOverview,
  });

  const { data: todayLessons, isLoading } = useQuery({
    queryKey: ["dashboard", "my-lessons", todayKey, todayKey],
    queryFn: () => getMyLessons(todayKey, todayKey),
  });

  const { data: todayCampDays, isLoading: campsLoading } = useQuery({
    queryKey: ["dashboard", "my-camp-days", todayKey, todayKey],
    queryFn: () => getMyCampDays(todayKey, todayKey),
  });

  const todayItems = buildAgenda(todayLessons, todayCampDays);
  const todayLoading = isLoading || campsLoading;

  const [selectedLessonId, setSelectedLessonId] = useState<string | null>(null);

  return (
    // min-h-full: de kalender neemt de volledige beschikbare hoogte van de pagina in.
    <div className="flex min-h-full flex-col">
      <h1 className="sr-only">{t("title")}</h1>

      <div className="mb-[18px] grid grid-cols-1 gap-[18px] md:grid-cols-2 xl:grid-cols-3">
        <NextLessonCard
          overview={overview}
          now={now}
          onSelect={setSelectedLessonId}
        />
        <HoursCard overview={overview} />
      </div>

      <div className="grid flex-1 grid-cols-1 gap-[18px] xl:grid-cols-[minmax(0,1fr)_minmax(0,1.8fr)]">
        <div className="self-start">
          <TodayCard
            items={todayItems}
            isLoading={todayLoading}
            onSelect={setSelectedLessonId}
          />
        </div>
        <MonthCalendar today={today} onSelect={setSelectedLessonId} />
      </div>

      <TrainerLessonDialog
        lessonId={selectedLessonId}
        onClose={() => setSelectedLessonId(null)}
      />
    </div>
  );
}
