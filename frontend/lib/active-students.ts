export type ActiveStudentsLevel = "normal" | "warning" | "exceeded";

export interface ActiveStudentsUsage {
  /** Alleen gezet bij een geldige limiet (> 0); anders geen voortgangsbalk. */
  limit: number | null;
  level: ActiveStudentsLevel;
  /** Breedte van de voortgangsbalk in procenten (0-100). */
  percentage: number;
}

const WARNING_RATIO = 0.8;

/**
 * Bepaalt kleurniveau en balkbreedte van de teller. Een limiet van 0, negatief of
 * niet-numeriek geldt als "geen limiet". Puur informatief: er wordt niets geblokkeerd.
 */
export function getActiveStudentsUsage(
  count: number,
  limit: number | null | undefined,
): ActiveStudentsUsage {
  if (limit == null || !Number.isFinite(limit) || limit <= 0) {
    return { limit: null, level: "normal", percentage: 0 };
  }
  const ratio = Math.max(count, 0) / limit;
  const level: ActiveStudentsLevel =
    ratio >= 1 ? "exceeded" : ratio >= WARNING_RATIO ? "warning" : "normal";
  return { limit, level, percentage: Math.min(ratio, 1) * 100 };
}

/** Query-key per organisatie (de API leest de organisatie uit het token). */
export function activeStudentsQueryKey(organizationId: string | null | undefined) {
  return ["activeStudents", organizationId ?? null] as const;
}
