"use client";

import { useEffect, useMemo, useRef, useState } from "react";
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
import {
  ClubOverviewCard,
  ClubOverviewDialog,
  useClubOverview,
} from "@/components/dashboard/club-overview";
import { TrainerLessonDialog } from "@/components/dashboard/trainer-lesson-dialog";
import { TennisBallEmptyIcon } from "@/components/ui/tennis-ball-icon";
import {
  getClubLessons,
  getMyCampDays,
  getMyLessons,
  getMyOverview,
  type MyCampDayDto,
  type MyLessonDto,
  type MyOverviewDto,
} from "@/lib/api/dashboard";
import { useClientValue } from "@/lib/hooks/use-client-value";
import { getOrganizationTrainers } from "@/lib/api/lessonSeries";
import { getAuthUser } from "@/lib/auth";
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

// ─── Welke lessen tonen we? (mijn lessen / hele club) ────────────────────────

export type AgendaScope = "mine" | "club";

const SCOPE_STORAGE_KEY = "coachos.dashboard.agenda-scope";

function readStoredScope(): AgendaScope {
  try {
    return localStorage.getItem(SCOPE_STORAGE_KEY) === "club" ? "club" : "mine";
  } catch {
    return "mine";
  }
}

function storeScope(scope: AgendaScope) {
  try {
    localStorage.setItem(SCOPE_STORAGE_KEY, scope);
  } catch {
    // Opslaan is een gemak, geen vereiste.
  }
}

/** Een les zonder trainer of van de ingelogde gebruiker zelf telt als "mijn les". */
function isOwnLesson(lesson: MyLessonDto, userId: string): boolean {
  return !lesson.trainerId || lesson.trainerId === userId;
}

/**
 * Lessen voor een datumbereik. In clubmodus (hoofdtrainer) komen de lessen van alle trainers in de
 * eigen club(s) erbij; eigen lessen in een andere club blijven zichtbaar.
 */
function useAgendaLessons(from: string, to: string, showClub: boolean) {
  const mine = useQuery({
    queryKey: ["dashboard", "my-lessons", from, to],
    queryFn: () => getMyLessons(from, to),
  });
  const club = useQuery({
    queryKey: ["dashboard", "club-lessons", from, to],
    queryFn: () => getClubLessons(from, to),
    enabled: showClub,
  });

  const lessons = useMemo(() => {
    if (!showClub) return mine.data;
    if (!club.data) return undefined;
    const clubIds = new Set(club.data.map((l) => l.id));
    return [
      ...club.data,
      ...(mine.data ?? []).filter((l) => !clubIds.has(l.id)),
    ];
  }, [showClub, mine.data, club.data]);

  return {
    lessons,
    isLoading: mine.isLoading || (showClub && club.isLoading),
    isError: mine.isError || (showClub && club.isError),
  };
}

// ─── Kleur per trainer ───────────────────────────────────────────────────────

interface LessonStyle {
  chip: string;
  dot: string;
  block: string;
  blockText: string;
}

/** Eigen lessen: altijd groen. */
const OWN_STYLE: LessonStyle = {
  chip: "bg-tennis-green/10 text-tennis-green hover:bg-tennis-green/20",
  dot: "bg-tennis-green",
  block: "bg-tennis-green/10",
  blockText: "text-tennis-green",
};

/**
 * Kleuren voor andere trainers. Bewust zonder groen (eigen lessen) en amber/oranje (kampdagen).
 * Klassen staan voluit uitgeschreven zodat Tailwind ze meeneemt.
 */
const OTHER_STYLES: LessonStyle[] = [
  {
    chip: "bg-violet-100 text-violet-800 hover:bg-violet-200",
    dot: "bg-violet-500",
    block: "bg-violet-100",
    blockText: "text-violet-700",
  },
  {
    chip: "bg-blue-100 text-blue-800 hover:bg-blue-200",
    dot: "bg-blue-500",
    block: "bg-blue-100",
    blockText: "text-blue-700",
  },
  {
    chip: "bg-pink-100 text-pink-800 hover:bg-pink-200",
    dot: "bg-pink-500",
    block: "bg-pink-100",
    blockText: "text-pink-700",
  },
  {
    chip: "bg-teal-100 text-teal-800 hover:bg-teal-200",
    dot: "bg-teal-500",
    block: "bg-teal-100",
    blockText: "text-teal-700",
  },
  {
    chip: "bg-red-100 text-red-800 hover:bg-red-200",
    dot: "bg-red-500",
    block: "bg-red-100",
    blockText: "text-red-700",
  },
  {
    chip: "bg-indigo-100 text-indigo-800 hover:bg-indigo-200",
    dot: "bg-indigo-500",
    block: "bg-indigo-100",
    blockText: "text-indigo-700",
  },
  {
    chip: "bg-cyan-100 text-cyan-800 hover:bg-cyan-200",
    dot: "bg-cyan-500",
    block: "bg-cyan-100",
    blockText: "text-cyan-700",
  },
];

function hashString(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = ((hash << 5) - hash + value.charCodeAt(i)) | 0;
  }
  return Math.abs(hash);
}

type StyleOf = (lesson: MyLessonDto) => LessonStyle;

/**
 * Geeft elke andere trainer een vaste kleur. De kleur volgt de positie van de trainer in de
 * (op id gesorteerde) lijst van trainers van de organisatie: stabiel tussen maanden en
 * gegarandeerd verschillend zolang er niet meer trainers zijn dan kleuren.
 */
function useLessonStyles(userId: string, enabled: boolean): StyleOf {
  const { data: members } = useQuery({
    queryKey: ["lessonseries", "members"],
    queryFn: getOrganizationTrainers,
    enabled,
    staleTime: 5 * 60_000,
  });

  return useMemo(() => {
    const index = new Map(
      (members ?? [])
        .map((m) => m.id)
        .filter((id) => id !== userId)
        .sort()
        .map((id, i) => [id, i] as const),
    );
    return (lesson: MyLessonDto): LessonStyle => {
      if (isOwnLesson(lesson, userId)) return OWN_STYLE;
      const id = lesson.trainerId ?? "";
      const position = index.get(id) ?? hashString(id);
      return OTHER_STYLES[position % OTHER_STYLES.length];
    };
  }, [members, userId]);
}

function AgendaScopeToggle({
  scope,
  clubCount,
  onChange,
}: {
  scope: AgendaScope;
  clubCount: number;
  onChange: (scope: AgendaScope) => void;
}) {
  const t = useTranslations("dashboard");
  const option = (value: AgendaScope, label: string) => (
    <button
      type="button"
      role="radio"
      aria-checked={scope === value}
      onClick={() => onChange(value)}
      className={cn(
        "rounded-md px-3 py-1 text-[11.5px] font-semibold transition-colors",
        scope === value
          ? "bg-ink text-white"
          : "text-ink-3 hover:text-ink hover:bg-canvas",
      )}
    >
      {label}
    </button>
  );

  return (
    <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
      <SlashLabel>{t("trainerScopeLabel")}</SlashLabel>
      <div className="flex items-center gap-4">
        <div
          role="radiogroup"
          aria-label={t("trainerScopeAria")}
          className="inline-flex gap-0.5 rounded-lg border border-rule bg-paper p-0.5"
        >
          {option("mine", t("trainerScopeMine"))}
          {option("club", t("trainerScopeClub", { count: clubCount }))}
        </div>
      </div>
    </div>
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

function DayCard({
  day,
  isToday,
  items,
  isLoading,
  userId,
  styleOf,
  onSelect,
  onBackToToday,
}: {
  /** De geselecteerde dag (standaard vandaag). */
  day: Date;
  isToday: boolean;
  items: AgendaItem[];
  isLoading: boolean;
  userId: string;
  styleOf: StyleOf;
  onSelect: (lessonId: string) => void;
  onBackToToday: () => void;
}) {
  const t = useTranslations("dashboard");
  const titleOf = useLessonTitle();

  return (
    <div className="bg-paper border border-rule rounded-xl overflow-hidden xl:flex xl:min-h-0 xl:flex-col">
      <div className="px-[18px] py-3.5 border-b border-rule flex items-center justify-between gap-3">
        <h2 className="text-[13px] font-bold text-ink">
          {isToday
            ? t("trainerTodayTitle")
            : t("trainerDayTitle", {
                date: capitalize(format(day, "EEEE d MMMM", { locale: nl })),
              })}
        </h2>
        <div className="flex shrink-0 items-center gap-2.5">
          {!isToday && (
            <button
              type="button"
              onClick={onBackToToday}
              className="rounded-md border border-rule px-2.5 h-7 text-[11px] font-medium text-ink-3 hover:text-ink hover:bg-canvas transition-colors"
            >
              {t("trainerBackToToday")}
            </button>
          )}
          {items.length > 0 && (
            <Mono className="text-[11px] text-ink-3">{items.length}</Mono>
          )}
        </div>
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
            {isToday ? t("trainerTodayEmptyTitle") : t("trainerDayEmptyTitle")}
          </p>
          <p className="text-ink-3 text-xs max-w-60 leading-relaxed">
            {isToday ? t("trainerTodayEmptyDesc") : t("trainerDayEmptyDesc")}
          </p>
        </div>
      )}

      {!isLoading && items.length > 0 && (
        <ul className="divide-y divide-rule xl:min-h-0 xl:overflow-y-auto">
          {items.map((item) =>
            item.kind === "lesson" ? (
              <li key={item.key}>
                <button
                  type="button"
                  onClick={() => onSelect(item.lesson.id)}
                  className="flex w-full items-start gap-3.5 px-[18px] py-3.5 text-left hover:bg-canvas/60 transition-colors"
                >
                  <div
                    className={cn(
                      "shrink-0 w-[62px] rounded-lg py-1.5 text-center",
                      styleOf(item.lesson).block,
                    )}
                  >
                    <Mono
                      className={cn(
                        "block text-[12.5px] font-bold",
                        styleOf(item.lesson).blockText,
                      )}
                    >
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
                      {!isOwnLesson(item.lesson, userId) &&
                        item.lesson.trainerName && (
                          <span>
                            {t("trainerWithTrainer", {
                              name: item.lesson.trainerName,
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
  showClub,
  userId,
  styleOf,
  selectedKey,
  onSelectDay,
  onSelect,
}: {
  today: Date;
  showClub: boolean;
  userId: string;
  styleOf: StyleOf;
  /** De geselecteerde dag (yyyy-MM-dd): die toont de kaart naast de kalender. */
  selectedKey: string;
  onSelectDay: (dayKey: string) => void;
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

  const { lessons, isLoading, isError } = useAgendaLessons(from, to, showClub);

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
    <div className="flex h-full flex-col overflow-hidden rounded-xl border border-rule bg-paper xl:min-h-0">
      <div className="px-[18px] py-3.5 border-b border-rule flex items-center justify-between gap-3">
        <h2 className="text-[13px] font-bold text-ink">
          {capitalize(format(month, "LLLL yyyy", { locale: nl }))}
        </h2>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => {
              setMonth(startOfMonth(today));
              onSelectDay(format(today, ISO_DATE));
            }}
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
          "grid flex-1 grid-cols-[repeat(7,minmax(0,1fr))] auto-rows-fr xl:min-h-0 xl:auto-rows-[minmax(0,1fr)]",
          isLoading && "opacity-60",
        )}
      >
        {days.map((day, i) => {
          const key = format(day, ISO_DATE);
          const dayItems = itemsByDay.get(key) ?? [];
          const inMonth = isSameMonth(day, month);
          const isToday = isSameDay(day, today);
          const isSelected = key === selectedKey;
          const visible = dayItems.slice(0, MAX_CHIPS_PER_DAY);
          const extra = dayItems.length - visible.length;
          const dayLabel = format(day, "EEEE d MMMM", { locale: nl });

          return (
            // Een klik op de dag selecteert hem; de nummerknop is het toetsenbord-/schermlezerpad.
            <div
              key={key}
              onClick={() => onSelectDay(key)}
              className={cn(
                "min-h-[64px] sm:min-h-[92px] xl:min-h-0 xl:overflow-hidden p-1.5 border-rule cursor-pointer transition-colors",
                i % 7 !== 0 && "border-l",
                i >= 7 && "border-t",
                !inMonth && "bg-canvas/40",
                isSelected
                  ? "bg-tennis-green/[0.06] ring-2 ring-inset ring-tennis-green/50"
                  : "hover:bg-canvas/60",
              )}
            >
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  onSelectDay(key);
                }}
                aria-label={t("trainerCalendarSelectDay", { date: dayLabel })}
                aria-pressed={isSelected}
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
              </button>

              {/* Desktop: chips per les of kampdag */}
              <div className="hidden sm:block mt-1 space-y-0.5">
                {visible.map((item) =>
                  item.kind === "lesson" ? (
                    <button
                      key={item.key}
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        onSelect(item.lesson.id);
                      }}
                      title={`${item.lesson.startTime} – ${item.lesson.endTime} · ${titleOf(item.lesson)}${
                        item.lesson.trainerName &&
                        !isOwnLesson(item.lesson, userId)
                          ? ` · ${item.lesson.trainerName}`
                          : ""
                      }`}
                      className={cn(
                        "block w-full truncate rounded px-1.5 py-0.5 text-left text-[10.5px] leading-tight transition-colors",
                        styleOf(item.lesson).chip,
                      )}
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
                      onClick={(e) => e.stopPropagation()}
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
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      onSelectDay(key);
                    }}
                    className="px-1.5 text-[10px] font-medium text-ink-3 hover:text-tennis-green hover:underline"
                  >
                    {t("trainerCalendarMore", { count: extra })}
                  </button>
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
                          ? styleOf(item.lesson).dot
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

      {showClub && (
        <CalendarLegend
          lessons={lessons ?? []}
          hasCamp={(campDays ?? []).length > 0}
          userId={userId}
          styleOf={styleOf}
        />
      )}
    </div>
  );
}

/** Legenda onder de agenda: eigen lessen, elke andere trainer in beeld, en kampdagen. */
function CalendarLegend({
  lessons,
  hasCamp,
  userId,
  styleOf,
}: {
  lessons: MyLessonDto[];
  hasCamp: boolean;
  userId: string;
  styleOf: StyleOf;
}) {
  const t = useTranslations("dashboard");

  const others = new Map<string, MyLessonDto>();
  for (const lesson of lessons) {
    if (
      !isOwnLesson(lesson, userId) &&
      lesson.trainerId &&
      !others.has(lesson.trainerId)
    ) {
      others.set(lesson.trainerId, lesson);
    }
  }
  const sortedOthers = [...others.values()].sort((a, b) =>
    (a.trainerName ?? "").localeCompare(b.trainerName ?? ""),
  );

  return (
    <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 border-t border-rule px-[18px] py-2.5 text-[11px] text-ink-3">
      <span className="inline-flex items-center gap-1.5">
        <span className={cn("h-2.5 w-2.5 rounded-full", OWN_STYLE.dot)} />
        {t("trainerLegendMine")}
      </span>
      {sortedOthers.map((lesson) => (
        <span
          key={lesson.trainerId}
          className="inline-flex items-center gap-1.5"
        >
          <span
            className={cn("h-2.5 w-2.5 rounded-full", styleOf(lesson).dot)}
          />
          {lesson.trainerName}
        </span>
      ))}
      {hasCamp && (
        <span className="inline-flex items-center gap-1.5">
          <span className="h-2.5 w-2.5 rounded-full bg-amber-500" />
          {t("trainerCampLabel")}
        </span>
      )}
    </div>
  );
}

// ─── Dashboard ────────────────────────────────────────────────────────────────

export type TrainerDashboardVariant = "trainer" | "head-trainer";

export function TrainerDashboard({
  variant = "trainer",
}: {
  variant?: TrainerDashboardVariant;
}) {
  // De datum (als yyyy-MM-dd) bestaat enkel client-side.
  const todayKey = useClientValue(() => format(new Date(), ISO_DATE));

  if (!todayKey) return null;
  return (
    <TrainerDashboardContent
      today={parse(todayKey, ISO_DATE, new Date())}
      variant={variant}
    />
  );
}

function TrainerDashboardContent({
  today,
  variant,
}: {
  today: Date;
  variant: TrainerDashboardVariant;
}) {
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

  // Alleen een hoofdtrainer kiest tussen "mijn lessen" en "hele club"; de keuze wordt onthouden.
  const isHeadTrainer = variant === "head-trainer";
  const userId = useClientValue(() => getAuthUser()?.userId ?? "") ?? "";
  const clubCount =
    useClientValue(() => getAuthUser()?.headTrainerClubIds?.length ?? 0) ?? 0;
  const [scope, setScope] = useState<AgendaScope>(() =>
    isHeadTrainer ? readStoredScope() : "mine",
  );
  const showClub = isHeadTrainer && scope === "club";
  const styleOf = useLessonStyles(userId, showClub);
  function changeScope(next: AgendaScope) {
    setScope(next);
    storeScope(next);
  }

  // De geselecteerde dag (standaard vandaag) bepaalt wat de kaart naast de kalender toont.
  const [selectedKey, setSelectedKey] = useState(todayKey);
  const selectedDay = parse(selectedKey, ISO_DATE, today);
  const isTodaySelected = selectedKey === todayKey;
  const dayCardRef = useRef<HTMLDivElement>(null);

  function selectDay(dayKey: string) {
    setSelectedKey(dayKey);
    // Onder de brede layout staat de kaart boven de kalender (gestapeld): breng ze dan in beeld.
    if (!window.matchMedia("(min-width: 1280px)").matches) {
      dayCardRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    }
  }

  const { lessons: todayLessons, isLoading } = useAgendaLessons(
    selectedKey,
    selectedKey,
    showClub,
  );

  const { data: todayCampDays, isLoading: campsLoading } = useQuery({
    queryKey: ["dashboard", "my-camp-days", selectedKey, selectedKey],
    queryFn: () => getMyCampDays(selectedKey, selectedKey),
  });

  const todayItems = buildAgenda(todayLessons, todayCampDays);
  const todayLoading = isLoading || campsLoading;

  const [selectedLessonId, setSelectedLessonId] = useState<string | null>(null);
  const [clubOverviewOpen, setClubOverviewOpen] = useState(false);
  const { data: clubOverview, isError: clubOverviewError } =
    useClubOverview(isHeadTrainer);

  return (
    // Op groot scherm scrolt de pagina niet: alles past in de hoogte van de container en de
    // kalender krimpt mee. Op kleinere schermen groeit de pagina gewoon mee.
    <div
      className="flex min-h-full flex-col xl:h-full xl:overflow-hidden"
      data-variant={variant}
    >
      <h1 className="sr-only">{t("title")}</h1>

      <div
        className={cn(
          "mb-[18px] grid grid-cols-1 gap-[18px] md:grid-cols-2",
          isHeadTrainer ? "xl:grid-cols-4" : "xl:grid-cols-3",
        )}
      >
        <NextLessonCard
          overview={overview}
          now={now}
          onSelect={setSelectedLessonId}
        />
        <HoursCard overview={overview} />
        {isHeadTrainer && (
          <ClubOverviewCard
            overview={clubOverview}
            onOpen={() => setClubOverviewOpen(true)}
          />
        )}
      </div>

      {isHeadTrainer && (
        <AgendaScopeToggle
          scope={scope}
          clubCount={clubCount}
          onChange={changeScope}
        />
      )}

      <div className="grid flex-1 grid-cols-1 gap-[18px] xl:min-h-0 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.8fr)] xl:grid-rows-[minmax(0,1fr)]">
        <div
          ref={dayCardRef}
          className="self-start scroll-mt-4 xl:flex xl:max-h-full xl:min-h-0 xl:flex-col"
        >
          <DayCard
            day={selectedDay}
            isToday={isTodaySelected}
            items={todayItems}
            isLoading={todayLoading}
            userId={userId}
            styleOf={styleOf}
            onSelect={setSelectedLessonId}
            onBackToToday={() => selectDay(todayKey)}
          />
        </div>
        <MonthCalendar
          today={today}
          showClub={showClub}
          userId={userId}
          styleOf={styleOf}
          selectedKey={selectedKey}
          onSelectDay={selectDay}
          onSelect={setSelectedLessonId}
        />
      </div>

      {isHeadTrainer && (
        <ClubOverviewDialog
          overview={clubOverview}
          isError={clubOverviewError}
          open={clubOverviewOpen}
          onClose={() => setClubOverviewOpen(false)}
        />
      )}
      <TrainerLessonDialog
        lessonId={selectedLessonId}
        onClose={() => setSelectedLessonId(null)}
      />
    </div>
  );
}
