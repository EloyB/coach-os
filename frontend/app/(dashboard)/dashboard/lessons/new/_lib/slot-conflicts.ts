import type { WizardSlot } from "../_types";

/**
 * Een botsing tussen weekindeling-slots op hetzelfde moment. Mirrort de backend-guard in
 * LessonSerieService.CreateAsync: slots worden gegroepeerd op (dag, starttijd, genormaliseerde baannaam).
 * Parallelle lessen (2 trainers/velden) worden onderscheiden via de baannaam; een lege of enkel-witruimte
 * baannaam telt als "geen baan" en botst dus met elke andere baanloze les op hetzelfde moment.
 */
export interface SlotConflict {
  dayOfWeek: number;
  startTime: string;
  /** Genormaliseerde baannaam; "" = geen baan opgegeven. */
  courtName: string;
  count: number;
  /** True wanneer de botsende slots geen baannaam hebben (op te lossen door baannamen toe te voegen). */
  missingCourt: boolean;
}

function normalizeCourt(court: string | null): string {
  return (court ?? "").trim();
}

/**
 * Vindt alle groepen slots die dezelfde dag + starttijd + (genormaliseerde) baannaam delen.
 * Retourneert één conflict per botsende groep.
 */
export function findSlotConflicts(slots: WizardSlot[]): SlotConflict[] {
  const groups = new Map<string, { slot: WizardSlot; court: string }[]>();

  for (const slot of slots) {
    const court = normalizeCourt(slot.courtName);
    const key = `${slot.dayOfWeek}|${slot.startTime}|${court.toLowerCase()}`;
    const group = groups.get(key) ?? [];
    group.push({ slot, court });
    groups.set(key, group);
  }

  const conflicts: SlotConflict[] = [];
  for (const group of groups.values()) {
    if (group.length > 1) {
      const court = group[0].court;
      conflicts.push({
        dayOfWeek: group[0].slot.dayOfWeek,
        startTime: group[0].slot.startTime,
        courtName: court,
        count: group.length,
        missingCourt: court === "",
      });
    }
  }

  return conflicts;
}
