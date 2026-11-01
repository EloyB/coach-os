import type { CampDayPartsDto, CampDaySelection } from "@/lib/api/camps";

/** Een kampdag zoals de dagkeuze hem nodig heeft (publiek: met volle dagdelen; beheer: zonder). */
export type PickerDay = CampDayPartsDto & {
  id: string;
  date: string;
  morningFull?: boolean;
  afternoonFull?: boolean;
};

export type DayOption = "none" | "morning" | "afternoon" | "full";

const hasMorning = (day: PickerDay) => day.morningStart !== null;
const hasAfternoon = (day: PickerDay) => day.afternoonStart !== null;

/** Prijsregel van de backend: beide dagdelen = dagprijs, één dagdeel = prijs halve dag. */
export function calcTotal(
  selection: CampDaySelection[],
  dayPrice: number,
  halfDayPrice: number,
): number {
  return selection.reduce(
    (sum, s) => sum + (s.morning && s.afternoon ? dayPrice : halfDayPrice),
    0,
  );
}

/** De keuzes die voor een dag aangeboden worden (zonder "none"). */
export function availableOptions(
  day: PickerDay,
  allowHalfDays: boolean,
): Exclude<DayOption, "none">[] {
  const morningFree = hasMorning(day) && !day.morningFull;
  const afternoonFree = hasAfternoon(day) && !day.afternoonFull;
  const bothExist = hasMorning(day) && hasAfternoon(day);

  if (bothExist) {
    if (!allowHalfDays) return morningFree && afternoonFree ? ["full"] : [];
    const options: Exclude<DayOption, "none">[] = [];
    if (morningFree) options.push("morning");
    if (afternoonFree) options.push("afternoon");
    if (morningFree && afternoonFree) options.push("full");
    return options;
  }
  if (hasMorning(day)) return morningFree ? ["morning"] : [];
  return afternoonFree ? ["afternoon"] : [];
}

export function selectionFor(
  day: PickerDay,
  option: DayOption,
): CampDaySelection | null {
  if (option === "none") return null;
  return {
    campDayId: day.id,
    morning: option === "morning" || option === "full",
    afternoon: option === "afternoon" || option === "full",
  };
}

export function optionOf(selection: CampDaySelection | undefined): DayOption {
  if (!selection) return "none";
  if (selection.morning && selection.afternoon) return "full";
  return selection.morning ? "morning" : "afternoon";
}

/** "Ik kom heel het kamp": elke dag volledig, voor zover vrij (en toegestaan zonder halve dagen). */
export function wholeCampSelection(
  days: PickerDay[],
  allowHalfDays: boolean,
): CampDaySelection[] {
  const result: CampDaySelection[] = [];
  for (const day of days) {
    const morning = hasMorning(day) && !day.morningFull;
    const afternoon = hasAfternoon(day) && !day.afternoonFull;
    if (!morning && !afternoon) continue;
    const wholeDayFree =
      morning === hasMorning(day) && afternoon === hasAfternoon(day);
    if (!allowHalfDays && !wholeDayFree) continue;
    result.push({ campDayId: day.id, morning, afternoon });
  }
  return result;
}

export function isWholeCamp(
  selection: CampDaySelection[],
  days: PickerDay[],
  allowHalfDays: boolean,
): boolean {
  const whole = wholeCampSelection(days, allowHalfDays);
  return (
    whole.length > 0 &&
    whole.length === selection.length &&
    whole.every((w) =>
      selection.some(
        (s) =>
          s.campDayId === w.campDayId &&
          s.morning === w.morning &&
          s.afternoon === w.afternoon,
      ),
    )
  );
}

/** "21/12 hele dag", "22/12 voormiddag", op datum gesorteerd; onbekende dagen worden overgeslagen. */
export function describeSelection(
  selection: CampDaySelection[],
  days: Pick<PickerDay, "id" | "date">[],
  labels: { morning: string; afternoon: string; wholeDay: string },
): string[] {
  const dateOf = new Map(days.map((d) => [d.id, d.date]));
  return selection
    .filter((s) => dateOf.has(s.campDayId))
    .sort((a, b) => dateOf.get(a.campDayId)!.localeCompare(dateOf.get(b.campDayId)!))
    .map((s) => {
      const [, month, day] = dateOf.get(s.campDayId)!.split("-");
      const part =
        s.morning && s.afternoon
          ? labels.wholeDay
          : s.morning
            ? labels.morning
            : labels.afternoon;
      return `${day}/${month} ${part.toLowerCase()}`;
    });
}

/** "32,50": bedrag met twee decimalen en een komma, voor in vertaalde zinnen. */
export function formatPrice(amount: number): string {
  return amount.toFixed(2).replace(".", ",");
}
