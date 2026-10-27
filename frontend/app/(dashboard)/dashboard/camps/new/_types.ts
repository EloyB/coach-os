import type {
  CampDetailDto,
  CampPackageDto,
  CreateCampRequest,
  SaveCampPackageRequest,
} from "@/lib/api/camps";

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

/** Prijsoptie in het formulier; `discount` (korting in EUR) is NaN zolang het veld leeg is. */
export type CampPriceDraft = {
  id?: string;
  label: string;
  description: string;
  discount: number;
};

/** Pakket in het formulier; `price` is NaN zolang het veld leeg is. */
export type CampPackageDraft = {
  id?: string;
  name: string;
  description: string;
  price: number;
  slots: { date: string; partOrder: number }[];
};

/** Vast dagdeel van een kampdag in het formulier. */
export type CampDayPartDraft = {
  name: string;
  startTime: string; // HH:mm
  endTime: string; // HH:mm
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
  /** Maximaal 2 dagdelen; leeg = de dag is één blok. Met dagdelen volgen de dagtijden de dagdelen. */
  parts: CampDayPartDraft[];
};

/** Maximaal aantal dagdelen per kampdag (spiegelt de backendvalidatie). */
export const MAX_PARTS_PER_DAY = 2;

/** Kampuren van een dag met dagdelen: de omhullende van de dagdelen. */
export function deriveDayHours(
  parts: CampDayPartDraft[],
  fallback: { startTime: string; endTime: string },
): { startTime: string; endTime: string } {
  if (parts.length === 0) return fallback;
  return {
    startTime: parts.reduce((m, p) => (p.startTime < m ? p.startTime : m), parts[0]!.startTime),
    endTime: parts.reduce((m, p) => (p.endTime > m ? p.endTime : m), parts[0]!.endTime),
  };
}

export type PartsError = "emptyName" | "endBeforeStart" | "overlap";

/** Eerste fout in de dagdelen van een dag, of null als ze geldig zijn. */
export function findPartsError(parts: CampDayPartDraft[]): PartsError | null {
  if (parts.some((p) => !p.name.trim())) return "emptyName";
  if (parts.some((p) => p.endTime <= p.startTime)) return "endBeforeStart";
  const sorted = [...parts].sort((a, b) => a.startTime.localeCompare(b.startTime));
  for (let i = 1; i < sorted.length; i++) {
    if (sorted[i]!.startTime < sorted[i - 1]!.endTime) return "overlap";
  }
  return null;
}

// ─── Day builders / mergers ───────────────────────────────────────────────────

export type DateRange = { start: string; end: string };

/**
 * Build the day list for [start, end]. Existing days (matched by date) keep their times + trainers.
 * Dates that were already inside `previousRange` but have no day are days the admin deliberately
 * dropped: they stay dropped. Only dates that are new to the range get a default day.
 */
export function buildDays(
  start: string,
  end: string,
  existing: CampDayDraft[],
  previousRange?: DateRange | null,
): CampDayDraft[] {
  if (!start || !end || end < start) return [];
  const byDate = new Map(existing.map((d) => [d.date, d]));
  const result: CampDayDraft[] = [];
  const cur = new Date(start + "T00:00:00");
  const last = new Date(end + "T00:00:00");
  while (cur <= last) {
    const iso = `${cur.getFullYear()}-${String(cur.getMonth() + 1).padStart(2, "0")}-${String(cur.getDate()).padStart(2, "0")}`;
    const kept = byDate.get(iso);
    const wasDropped =
      previousRange != null &&
      iso >= previousRange.start &&
      iso <= previousRange.end;
    if (kept) result.push(kept);
    else if (!wasDropped)
      result.push({
        date: iso,
        startTime: "09:00",
        endTime: "16:00",
        trainers: [],
        parts: [],
      });
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
    discount: p.discount,
  }));
}

/** Haalt slots weg die niet (meer) naar een dagdeel van de dagen wijzen, bv. na een gewijzigde kampperiode. */
export function prunePackages(
  packages: CampPackageDraft[],
  days: CampDayDraft[],
): CampPackageDraft[] {
  const existing = new Set(
    days.flatMap((d) => d.parts.map((_, order) => `${d.date}#${order}`)),
  );
  return packages.map((p) => ({
    ...p,
    slots: p.slots.filter((s) => existing.has(`${s.date}#${s.partOrder}`)),
  }));
}

export type PackagesBlock = "dayWithoutParts" | "packageWithoutSlots";

/** Waarom de pakketten (nog) niet bewaard kunnen worden, of null als alles klopt. */
export function findPackagesBlock(
  packages: CampPackageDraft[],
  days: CampDayDraft[],
): PackagesBlock | null {
  if (packages.length === 0) return null;
  if (days.some((d) => d.parts.length === 0)) return "dayWithoutParts";
  if (packages.some((p) => p.slots.length === 0)) return "packageWithoutSlots";
  return null;
}

/** Map the packages of an existing camp into form drafts. */
export function detailToPackages(detail: CampDetailDto): CampPackageDraft[] {
  return detail.packages.map((p: CampPackageDto) => ({
    id: p.id,
    name: p.name,
    description: p.description ?? "",
    price: p.price,
    slots: p.slots.map((s) => ({ date: s.date, partOrder: s.partOrder })),
  }));
}

/** Build the package payload; weglaten van `packages` laat bestaande pakketten ongemoeid. */
export function buildPackageRequests(
  packages: CampPackageDraft[],
): SaveCampPackageRequest[] {
  return packages.map((p) => ({
    id: p.id,
    name: p.name.trim(),
    description: p.description.trim() || undefined,
    price: p.price,
    slots: p.slots,
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
    parts: d.parts.map((p) => ({
      name: p.name,
      startTime: p.startTime,
      endTime: p.endTime,
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
  packages?: CampPackageDraft[],
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
    packages: packages ? buildPackageRequests(packages) : undefined,
    prices: step1.prices.map((p) => ({
      id: p.id,
      label: p.label.trim(),
      description: p.description.trim() || undefined,
      discount: p.discount,
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
      parts: d.parts.length > 0 ? d.parts : undefined,
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
  return (
    Math.max(0, toMinutes(shift.endTime) - toMinutes(shift.startTime)) / 60
  );
}
