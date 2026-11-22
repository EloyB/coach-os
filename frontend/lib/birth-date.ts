export type BirthDateError = "required" | "invalid" | "future" | "implausible";

const MAXIMUM_AGE_IN_YEARS = 120;

/** Spiegelt de backendregels voor een geboortedatum (yyyy-MM-dd uit een date-input). */
export function getBirthDateError(value: string, today = new Date()): BirthDateError | null {
  if (!value.trim()) return "required";

  const birthDate = new Date(`${value}T00:00:00`);
  if (Number.isNaN(birthDate.getTime())) return "invalid";

  const startOfToday = new Date(today);
  startOfToday.setHours(0, 0, 0, 0);
  if (birthDate > startOfToday) return "future";

  const oldestPlausibleBirthDate = new Date(startOfToday);
  oldestPlausibleBirthDate.setFullYear(startOfToday.getFullYear() - MAXIMUM_AGE_IN_YEARS);
  if (birthDate < oldestPlausibleBirthDate) return "implausible";

  return null;
}

export function formatTodayAsIsoDate(today = new Date()): string {
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${today.getFullYear()}-${month}-${day}`;
}
