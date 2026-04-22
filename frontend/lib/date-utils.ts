/**
 * Formats an ISO date string (YYYY-MM-DD) as dd/MM/yyyy for Dutch display.
 */
export function formatDateNL(dateStr: string): string {
  const [year, month, day] = dateStr.split("-");
  return `${day}/${month}/${year}`;
}

/**
 * Formats an ISO date string (YYYY-MM-DD) as "20 apr" using nl-BE locale.
 */
export function formatDateShort(isoDate: string): string {
  const d = new Date(isoDate + "T00:00:00");
  return d.toLocaleDateString("nl-BE", { day: "numeric", month: "short" });
}
