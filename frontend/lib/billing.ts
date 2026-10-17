/** Pure helpers voor de abonnementspagina. Zelfde regels als de backend (enkel Belgische btw). */

export const VAT_RATE = 0.21;
export const OVERAGE_MARGIN = 0.1;

export type BillingIntervalName = "Monthly" | "Yearly";

export interface PlanOption {
  plan: number;
  name: string;
  monthlyPrice: number;
  yearlyPrice: number;
  studentLimit: number;
}

/** Bedrag incl. 21% btw, afgerond op centen. */
export function grossAmount(net: number): number {
  return Math.round(net * (1 + VAT_RATE) * 100) / 100;
}

/** Kleinste plan waarvan de limiet het aantal leerlingen dekt; anders het grootste. */
export function suggestPlan(plans: PlanOption[], activeStudents: number): PlanOption {
  const sorted = [...plans].sort((a, b) => a.studentLimit - b.studentLimit);
  return sorted.find((p) => p.studentLimit >= activeStudents) ?? sorted[sorted.length - 1];
}

/** True als de club meer leerlingen heeft dan de limiet plus de marge van 10%. */
export function exceedsLimit(plan: PlanOption, activeStudents: number): boolean {
  return activeStudents > plan.studentLimit * (1 + OVERAGE_MARGIN);
}

/** Overschrijving kan enkel met een jaarabonnement. */
export function canPayByBankTransfer(interval: BillingIntervalName): boolean {
  return interval === "Yearly";
}

export function planPrice(plan: PlanOption, interval: BillingIntervalName): number {
  return interval === "Yearly" ? plan.yearlyPrice : plan.monthlyPrice;
}

/** Belgisch ondernemingsnummer: 10 cijfers, beginnend met 0 of 1, met of zonder "BE" (zelfde regel als de backend). */
export function isValidBelgianVat(value: string): boolean {
  const withoutPrefix = value.trim().replace(/^BE/i, "");
  if (/[^\d .]/.test(withoutPrefix)) return false;
  const digits = withoutPrefix.replace(/\D/g, "");
  return digits.length === 10 && (digits[0] === "0" || digits[0] === "1");
}

const euro = new Intl.NumberFormat("nl-BE", { style: "currency", currency: "EUR" });

export function formatEuro(amount: number): string {
  return euro.format(amount);
}
