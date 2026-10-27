import type { CampDayPartDto, CampPackageSlotDto } from "@/lib/api/camps";

/** Minimale dagvorm die de samenvatting nodig heeft; `CampDayDto` voldoet hieraan. */
export type PackageDay = { date: string; parts: CampDayPartDto[] };

/**
 * Vat de dagdelen van een pakket samen in één regel per dag, in datumvolgorde,
 * bv. "ma 14 juli: Ochtend 09:00-12:00, Namiddag 13:00-16:00".
 * Slots die naar een onbekend dagdeel wijzen worden overgeslagen.
 */
export function summarizePackageSlots(
  days: PackageDay[],
  slots: CampPackageSlotDto[],
): string[] {
  const dates = [...new Set(slots.map((s) => s.date))].sort();
  const lines: string[] = [];

  for (const date of dates) {
    const day = days.find((d) => d.date === date);
    if (!day) continue;

    const parts = slots
      .filter((s) => s.date === date)
      .map((s) => day.parts.find((p) => p.order === s.partOrder))
      .filter((p): p is NonNullable<typeof p> => p !== undefined)
      .sort((a, b) => a.order - b.order)
      .map((p) => `${p.name} ${p.startTime}-${p.endTime}`);
    if (parts.length === 0) continue;

    lines.push(`${formatDayLabel(date)}: ${parts.join(", ")}`);
  }
  return lines;
}

function formatDayLabel(isoDate: string): string {
  return new Date(`${isoDate}T00:00:00`).toLocaleDateString("nl-BE", {
    weekday: "short",
    day: "numeric",
    month: "long",
  });
}
