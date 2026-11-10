import type { TrainerAvailabilityDto } from "@/lib/api/trainerAvailabilities";

/** Standaardbereik van de tijdas in het weekoverzicht. */
const DEFAULT_START_HOUR = 7;
const DEFAULT_END_HOUR = 23;
const SNAP_MINUTES = 15;

export interface AxisRange {
  startHour: number;
  endHour: number;
}

export interface TimeSpan {
  startTime: string;
  endTime: string;
}

export function toMinutes(time: string): number {
  const [hours, minutes] = time.split(":").map(Number);
  return hours * 60 + minutes;
}

export function toTime(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
}

export function snapToQuarter(minutes: number): number {
  return Math.round(minutes / SNAP_MINUTES) * SNAP_MINUTES;
}

/**
 * As van 07:00 tot 23:00, breder naar het dichtstbijzijnde hele uur wanneer bestaande
 * tijdvakken (bijvoorbeeld door de admin ingevuld) daarbuiten vallen.
 */
export function computeAxisRange(spans: TimeSpan[]): AxisRange {
  let startHour = DEFAULT_START_HOUR;
  let endHour = DEFAULT_END_HOUR;
  for (const span of spans) {
    startHour = Math.min(startHour, Math.floor(toMinutes(span.startTime) / 60));
    endHour = Math.max(endHour, Math.ceil(toMinutes(span.endTime) / 60));
  }
  return { startHour, endHour };
}

function axisMinutes(range: AxisRange): number {
  return (range.endHour - range.startHour) * 60;
}

export function blockPosition(
  startTime: string,
  endTime: string,
  range: AxisRange,
): { topPercent: number; heightPercent: number } {
  const total = axisMinutes(range);
  const start = toMinutes(startTime) - range.startHour * 60;
  const length = toMinutes(endTime) - toMinutes(startTime);
  return {
    topPercent: (start / total) * 100,
    heightPercent: (length / total) * 100,
  };
}

/** Zet een hoogtepositie in de kolom om naar minuten sinds middernacht, gesnapt en binnen de as. */
export function minutesFromOffset(
  offsetY: number,
  heightPx: number,
  range: AxisRange,
): number {
  const raw = range.startHour * 60 + (offsetY / heightPx) * axisMinutes(range);
  const snapped = snapToQuarter(raw);
  return Math.min(Math.max(snapped, range.startHour * 60), range.endHour * 60);
}

/** Halfopen: een tijdvak dat eindigt waar het volgende begint, overlapt niet. */
export function overlaps(a: TimeSpan, b: TimeSpan): boolean {
  return toMinutes(a.startTime) < toMinutes(b.endTime)
    && toMinutes(a.endTime) > toMinutes(b.startTime);
}

/** Alle zeven weekdagen (0 = maandag) aanwezig, per dag gesorteerd op starttijd. */
export function groupByDay(
  availabilities: TrainerAvailabilityDto[],
): Record<number, TrainerAvailabilityDto[]> {
  const grouped: Record<number, TrainerAvailabilityDto[]> = {};
  for (let day = 0; day < 7; day++) grouped[day] = [];
  for (const availability of availabilities) {
    grouped[availability.dayOfWeek]?.push(availability);
  }
  for (const day of Object.keys(grouped)) {
    grouped[Number(day)].sort(
      (a, b) => toMinutes(a.startTime) - toMinutes(b.startTime),
    );
  }
  return grouped;
}

/** 0 = "elke club" (groen); clubs krijgen 1, 2, ... in de volgorde van de clublijst. */
export function clubColorIndex(clubId: string | null, clubIds: string[]): number {
  if (clubId === null) return 0;
  const index = clubIds.indexOf(clubId);
  return index === -1 ? 0 : index + 1;
}
