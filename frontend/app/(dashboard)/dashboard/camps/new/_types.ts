import type { CampDetailDto, CreateCampRequest } from "@/lib/api/camps";

// ─── Wizard / shared draft types ──────────────────────────────────────────────

export type CampStep1Data = {
  name: string;
  description: string;
  tennisClubId: string;
  level: number | null;
  price: number;
  maxParticipants: number | null;
  startDate: string; // yyyy-MM-dd
  endDate: string; // yyyy-MM-dd
  registrationDeadline: string; // yyyy-MM-dd
  prices: CampPriceDraft[];
};

/** Prijsoptie in het formulier; `amount` is NaN zolang het veld leeg is. */
export type CampPriceDraft = {
  id?: string;
  label: string;
  description: string;
  amount: number;
};

export type CampTrainerDraft = {
  trainerId: string;
  startTime: string; // HH:mm
  endTime: string; // HH:mm
  /** Server-resolved trainer name (from camp detail), used as display fallback. */
  trainerName?: string;
};

export type CampDayDraft = {
  date: string; // yyyy-MM-dd
  startTime: string; // HH:mm
  endTime: string;
  trainers: CampTrainerDraft[];
};

// ─── Day builders / mergers ───────────────────────────────────────────────────

/**
 * Build one CampDayDraft per date in [start, end]. Existing days (matched by
 * date) keep their times + trainers so adjusting the range never wipes data.
 */
export function buildDays(
  start: string,
  end: string,
  existing: CampDayDraft[],
): CampDayDraft[] {
  if (!start || !end || end < start) return [];
  const byDate = new Map(existing.map((d) => [d.date, d]));
  const result: CampDayDraft[] = [];
  const cur = new Date(start + "T00:00:00");
  const last = new Date(end + "T00:00:00");
  while (cur <= last) {
    const iso = `${cur.getFullYear()}-${String(cur.getMonth() + 1).padStart(2, "0")}-${String(cur.getDate()).padStart(2, "0")}`;
    result.push(
      byDate.get(iso) ?? {
        date: iso,
        startTime: "09:00",
        endTime: "16:00",
        trainers: [],
      },
    );
    cur.setDate(cur.getDate() + 1);
  }
  return result;
}

/** Map the price options of an existing camp into form drafts. */
export function detailToPrices(detail: CampDetailDto): CampPriceDraft[] {
  return detail.prices.map((p) => ({
    id: p.id,
    label: p.label,
    description: p.description ?? "",
    amount: p.amount,
  }));
}

/** Map an existing camp detail into editable day drafts. */
export function detailToDays(detail: CampDetailDto): CampDayDraft[] {
  return detail.days.map((d) => ({
    date: d.date,
    startTime: d.startTime,
    endTime: d.endTime,
    trainers: d.trainers.map((tr) => ({
      trainerId: tr.trainerId,
      startTime: tr.startTime,
      endTime: tr.endTime,
      trainerName: tr.trainerName,
    })),
  }));
}

/** Convert an ISO timestamp (or yyyy-MM-dd) into a yyyy-MM-dd DatePicker value. */
export function toDateOnly(value: string | null | undefined): string {
  if (!value) return "";
  return value.split("T")[0] ?? "";
}

/** Clamp a HH:mm value into [min, max] (lexicographic compare works for HH:mm). */
export function clampTime(value: string, min: string, max: string): string {
  if (value < min) return min;
  if (value > max) return max;
  return value;
}

/** Build the API request payload from wizard step-1 data + day drafts. */
export function buildCampRequest(
  step1: CampStep1Data,
  days: CampDayDraft[],
): CreateCampRequest {
  return {
    name: step1.name.trim(),
    description: step1.description.trim() || undefined,
    tennisClubId: step1.tennisClubId,
    level: step1.level,
    price: step1.price,
    startDate: step1.startDate,
    endDate: step1.endDate,
    // Backend expects an ISO timestamp; the deadline is a date (UTC start of day).
    // Append "Z" so it is parsed as UTC midnight instead of local time (which
    // would shift the date by the UTC offset and land on the wrong day in CET).
    registrationDeadline: step1.registrationDeadline + "T00:00:00Z",
    maxParticipants: step1.maxParticipants,
    prices: step1.prices.map((p) => ({
      id: p.id,
      label: p.label.trim(),
      description: p.description.trim() || undefined,
      amount: p.amount,
    })),
    days: days.map((d) => ({
      date: d.date,
      startTime: d.startTime,
      endTime: d.endTime,
      trainers: d.trainers.map((tr) => ({
        trainerId: tr.trainerId,
        startTime: tr.startTime,
        endTime: tr.endTime,
      })),
    })),
  };
}

export function formatDayHeading(iso: string): string {
  const date = new Date(iso + "T00:00:00");
  return new Intl.DateTimeFormat("nl-BE", {
    weekday: "long",
    day: "numeric",
    month: "long",
  }).format(date);
}

// ─── Shiften van een trainer op een dag ──────────────────────────────────────

/** Eén shift van een trainer: een tijdvenster binnen een kampdag. */
export type CampShift = Pick<CampTrainerDraft, "startTime" | "endTime">;

/** Twee shiften overlappen als de ene begint voor de andere eindigt; aansluitende shiften zijn oké. */
export function shiftsOverlap(a: CampShift, b: CampShift): boolean {
  return a.startTime < b.endTime && b.startTime < a.endTime;
}

/**
 * Indexen (in `day.trainers`) van shiften die overlappen met een andere shift van dezelfde trainer
 * op die dag.
 */
export function findOverlappingShiftIndexes(day: CampDayDraft): Set<number> {
  const result = new Set<number>();
  day.trainers.forEach((a, i) => {
    day.trainers.forEach((b, j) => {
      if (i < j && a.trainerId === b.trainerId && shiftsOverlap(a, b)) {
        result.add(i);
        result.add(j);
      }
    });
  });
  return result;
}

export function hasOverlappingShifts(days: CampDayDraft[]): boolean {
  return days.some((d) => findOverlappingShiftIndexes(d).size > 0);
}

/** Duur van een shift in uren (HH:mm-strings). */
export function shiftHours(shift: CampShift): number {
  const toMinutes = (time: string) => {
    const [h, m] = time.split(":").map(Number);
    return (h ?? 0) * 60 + (m ?? 0);
  };
  return Math.max(0, toMinutes(shift.endTime) - toMinutes(shift.startTime)) / 60;
}
