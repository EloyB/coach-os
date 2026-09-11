/**
 * Sentinel voor "de parallelle banen op hetzelfde uur zijn het onderling oneens".
 * Nieuwe inschrijvingen schrijven dezelfde voorkeur naar elke parallelle baan,
 * maar legacy-data (of een andere schrijver) kan per baan verschillen.
 */
export const MIXED_PREF = "mixed" as const;

/**
 * Vouwt de voorkeuren van parallelle banen (zelfde dag+uur) samen tot één waarde.
 *
 * - `null` als geen enkele baan een voorkeur heeft.
 * - de gedeelde waarde als alle (aanwezige) banen dezelfde voorkeur hebben.
 * - `MIXED_PREF` als de banen onderling verschillen — zodat een conflict
 *   zichtbaar blijft i.p.v. stilzwijgend de eerste waarde te tonen.
 *
 * Ontbrekende waarden (null/undefined) worden genegeerd.
 */
export function collapseParallelPrefs<T>(
  values: readonly (T | null | undefined)[]
): T | typeof MIXED_PREF | null {
  const present = values.filter((v): v is T => v != null);
  if (present.length === 0) return null;
  for (const v of present) {
    if (v !== present[0]) return MIXED_PREF;
  }
  return present[0];
}
