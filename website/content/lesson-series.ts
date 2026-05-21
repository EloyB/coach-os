export type Sport = "tennis" | "padel";

export type LessonLevel = "Beginner" | "Intermediate" | "Gevorderd";

export interface ScheduleSlot {
  /** ISO weekday 1=Mon … 7=Sun. */
  dayOfWeek: number;
  /** "HH:mm". */
  startTime: string;
  endTime: string;
}

export interface PublicLessonSeries {
  id: string;
  slug: string;
  /** NOTE: not yet in DB — needs `Sport` on TennisClub or LessonSerie. */
  sport: Sport;
  name: string;
  /** Nullable — LessonSerie.Level is optional. */
  level: LessonLevel | null;
  clubName: string;
  /** NOTE: not yet in DB — TennisClub only has a single Address string. */
  clubCity: string;
  /** EUR, whole-series price. */
  pricePerSeriesEur: number;
  /** ISO date, derived from LessonSerie.StartDate. */
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  /** Derived from LessonSerie.Lessons.Count. */
  lessonCount: number;
  /** Derived from LessonSerie.WeeklyTemplate[]. */
  schedule: ScheduleSlot[];
  /** TotalCapacity (= sum of weekly MaxStudents * weeks) or MaxRegistrations. */
  capacity: number;
  spotsLeft: number;
}

export const LESSON_SERIES_HEADING =
  "Lessenreeksen bij clubs die met CoachOS werken";

export const LESSON_SERIES_SUB =
  "Bekijk de lessen die clubs in jouw buurt publiceren. Inschrijven gaat anoniem via een publieke link — geen account nodig.";

export const LESSON_SERIES_DISCLAIMER =
  "Demovoorbeelden — de clubs en lessen op deze pagina zijn fictief. Echte lessenreeksen verschijnen hier zodra clubs hun planning publiceren.";

export const LESSON_SERIES: PublicLessonSeries[] = [
  {
    id: "1",
    slug: "beginners-volwassenen-tc-lichtenburg",
    sport: "tennis",
    name: "Beginnerslessen voor volwassenen",
    level: "Beginner",
    clubName: "TC Lichtenburg",
    clubCity: "Antwerpen",
    pricePerSeriesEur: 195,
    startDate: "2026-09-07",
    endDate: "2026-11-16",
    registrationDeadline: "2026-08-25",
    lessonCount: 10,
    schedule: [
      { dayOfWeek: 1, startTime: "19:00", endTime: "20:00" },
      { dayOfWeek: 1, startTime: "20:00", endTime: "21:00" },
      { dayOfWeek: 3, startTime: "19:00", endTime: "20:00" },
      { dayOfWeek: 5, startTime: "18:00", endTime: "19:00" },
    ],
    capacity: 8,
    spotsLeft: 3,
  },
  {
    id: "2",
    slug: "padel-foundation-niveau-1-brussels-center",
    sport: "padel",
    name: "Padel Foundation",
    level: "Beginner",
    clubName: "Padel Brussels Center",
    clubCity: "Brussel",
    pricePerSeriesEur: 220,
    startDate: "2026-09-12",
    endDate: "2026-10-31",
    registrationDeadline: "2026-09-01",
    lessonCount: 8,
    schedule: [
      { dayOfWeek: 6, startTime: "10:00", endTime: "11:15" },
      { dayOfWeek: 6, startTime: "11:15", endTime: "12:30" },
    ],
    capacity: 12,
    spotsLeft: 5,
  },
  {
    id: "3",
    slug: "kids-tennis-6-tot-9-tc-eikenhof",
    sport: "tennis",
    name: "Kids Tennis (6 — 9 jaar)",
    level: "Beginner",
    clubName: "TC Eikenhof",
    clubCity: "Brasschaat",
    pricePerSeriesEur: 165,
    startDate: "2026-09-04",
    endDate: "2026-11-13",
    registrationDeadline: "2026-08-20",
    lessonCount: 10,
    schedule: [{ dayOfWeek: 5, startTime: "16:30", endTime: "17:20" }],
    capacity: 6,
    spotsLeft: 2,
  },
  {
    id: "4",
    slug: "gevorderden-weekprogramma-tc-ravels",
    sport: "tennis",
    name: "Gevorderden weekprogramma",
    level: "Gevorderd",
    clubName: "TC Ravels",
    clubCity: "Mechelen",
    pricePerSeriesEur: 360,
    startDate: "2026-09-08",
    endDate: "2026-12-01",
    registrationDeadline: "2026-08-28",
    lessonCount: 24,
    schedule: [
      { dayOfWeek: 2, startTime: "20:00", endTime: "21:30" },
      { dayOfWeek: 4, startTime: "20:00", endTime: "21:30" },
    ],
    capacity: 6,
    spotsLeft: 1,
  },
  {
    id: "5",
    slug: "padel-improvers-antwerpen-zuid",
    sport: "padel",
    name: "Padel Improvers",
    level: "Intermediate",
    clubName: "Padelclub Antwerpen Zuid",
    clubCity: "Antwerpen",
    pricePerSeriesEur: 275,
    startDate: "2026-09-15",
    endDate: "2026-11-24",
    registrationDeadline: "2026-09-05",
    lessonCount: 10,
    schedule: [
      { dayOfWeek: 2, startTime: "19:30", endTime: "20:45" },
      { dayOfWeek: 4, startTime: "20:00", endTime: "21:15" },
    ],
    capacity: 12,
    spotsLeft: 7,
  },
  {
    id: "6",
    slug: "competitie-tennis-royal-la-hulpe",
    sport: "tennis",
    name: "Interclubvoorbereiding",
    level: "Gevorderd",
    clubName: "Royal La Hulpe Tennis Club",
    clubCity: "La Hulpe",
    pricePerSeriesEur: 320,
    startDate: "2026-04-06",
    endDate: "2026-05-25",
    registrationDeadline: "2026-03-25",
    lessonCount: 8,
    schedule: [{ dayOfWeek: 4, startTime: "19:00", endTime: "20:30" }],
    capacity: 4,
    spotsLeft: 0,
  },
  {
    id: "7",
    slug: "damesgroep-beginners-tc-de-plataan",
    sport: "tennis",
    name: "Damesgroep Beginners",
    level: "Beginner",
    clubName: "TC De Plataan",
    clubCity: "Gent",
    pricePerSeriesEur: 175,
    startDate: "2026-09-09",
    endDate: "2026-11-18",
    registrationDeadline: "2026-08-26",
    lessonCount: 10,
    schedule: [{ dayOfWeek: 3, startTime: "09:30", endTime: "10:30" }],
    capacity: 8,
    spotsLeft: 4,
  },
  {
    id: "8",
    slug: "padel-bedrijfsteams-padel-park-leuven",
    sport: "padel",
    name: "Padel voor bedrijfsteams",
    level: null,
    clubName: "Padel Park",
    clubCity: "Leuven",
    pricePerSeriesEur: 540,
    startDate: "2026-10-01",
    endDate: "2026-11-12",
    registrationDeadline: "2026-09-20",
    lessonCount: 6,
    schedule: [
      { dayOfWeek: 4, startTime: "18:00", endTime: "19:30" },
      { dayOfWeek: 5, startTime: "17:30", endTime: "19:00" },
    ],
    capacity: 16,
    spotsLeft: 12,
  },
];

const WEEKDAYS_NL = [
  "",
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

const WEEKDAYS_NL_SHORT = ["", "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

export function formatWeekday(dayOfWeek: number, short = false): string {
  const list = short ? WEEKDAYS_NL_SHORT : WEEKDAYS_NL;
  return list[dayOfWeek] ?? "";
}

export interface ScheduleSummary {
  /** Unique weekdays (sorted Mon→Sun) covered by the template. */
  weekdays: number[];
  /** Earliest start across all slots, "HH:mm". */
  earliestStart: string;
  /** Latest end across all slots, "HH:mm". */
  latestEnd: string;
  /** True when the template offers more than one slot — exact slot will be assigned. */
  hasMultipleSlots: boolean;
}

export function summarizeSchedule(slots: ScheduleSlot[]): ScheduleSummary {
  const weekdays = Array.from(new Set(slots.map((s) => s.dayOfWeek))).sort(
    (a, b) => a - b,
  );
  const earliestStart = slots
    .map((s) => s.startTime)
    .sort()[0] ?? "";
  const latestEnd = slots
    .map((s) => s.endTime)
    .sort()
    .at(-1) ?? "";
  return {
    weekdays,
    earliestStart,
    latestEnd,
    hasMultipleSlots: slots.length > 1,
  };
}

export function formatWeekdayList(weekdays: number[]): string {
  if (weekdays.length === 0) return "";
  if (weekdays.length === 1) return formatWeekday(weekdays[0]);
  if (weekdays.length === 2) {
    return `${formatWeekday(weekdays[0])} of ${formatWeekday(weekdays[1])}`;
  }
  const names = weekdays.map((d) => formatWeekday(d));
  const last = names.pop();
  return `${names.join(", ")} of ${last}`;
}

export function formatLessonDateRange(startIso: string, endIso: string): string {
  const start = new Date(startIso);
  const end = new Date(endIso);
  const fmt = new Intl.DateTimeFormat("nl-BE", {
    day: "numeric",
    month: "short",
  });
  const sameYear = start.getFullYear() === end.getFullYear();
  const startStr = fmt.format(start);
  const endStr = `${fmt.format(end)} ${end.getFullYear()}`;
  return sameYear
    ? `${startStr} – ${endStr}`
    : `${startStr} ${start.getFullYear()} – ${endStr}`;
}

export function formatDeadlineShort(iso: string): string {
  return new Intl.DateTimeFormat("nl-BE", {
    day: "numeric",
    month: "short",
  }).format(new Date(iso));
}
