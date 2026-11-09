"use client";

import { useTranslations } from "next-intl";
import type { CampDaySelection } from "@/lib/api/camps";
import {
  availableOptions,
  calcTotal,
  isWholeCamp,
  optionOf,
  selectionFor,
  wholeCampSelection,
  type DayOption,
  type PickerDay,
} from "@/lib/camp-day-selection";
import { formatDateShort } from "@/lib/date-utils";

interface CampDayPickerProps {
  days: PickerDay[];
  allowHalfDays: boolean;
  dayPrice: number;
  halfDayPrice: number;
  value: CampDaySelection[];
  onChange: (value: CampDaySelection[]) => void;
  /** Toon het live totaalbedrag onderaan (standaard aan). */
  showTotal?: boolean;
  error?: string | null;
}

function formatAmount(amount: number): string {
  return `€${amount.toFixed(2).replace(".", ",")}`;
}

/** Kies per kampdag voormiddag, namiddag of hele dag, met de snelkeuze "Ik kom heel het kamp". */
export function CampDayPicker({
  days,
  allowHalfDays,
  dayPrice,
  halfDayPrice,
  value,
  onChange,
  showTotal = true,
  error,
}: CampDayPickerProps) {
  const t = useTranslations("camps");
  const sorted = [...days].sort((a, b) => a.date.localeCompare(b.date));
  const whole = isWholeCamp(value, sorted, allowHalfDays);

  function setDay(day: PickerDay, option: DayOption) {
    const next = value.filter((s) => s.campDayId !== day.id);
    const chosen = selectionFor(day, option);
    onChange(chosen ? [...next, chosen] : next);
  }

  function toggleWhole(checked: boolean) {
    onChange(checked ? wholeCampSelection(sorted, allowHalfDays) : []);
  }

  function label(day: PickerDay, option: Exclude<DayOption, "none">): string {
    if (option === "full") return t("wholeDay");
    const times =
      option === "morning"
        ? `${day.morningStart}–${day.morningEnd}`
        : `${day.afternoonStart}–${day.afternoonEnd}`;
    return `${t(option)} (${times})`;
  }

  const total = calcTotal(value, dayPrice, halfDayPrice);

  return (
    <div className="space-y-4">
      <label className="flex items-center gap-2 rounded-lg border border-tennis-green/30 bg-tennis-green/5 px-3 py-2.5 text-sm font-medium text-gray-900 cursor-pointer">
        <input
          type="checkbox"
          checked={whole}
          onChange={(e) => toggleWhole(e.target.checked)}
          className="h-4 w-4 rounded border-gray-300 accent-tennis-green"
        />
        {t("pickerWholeCamp")}
      </label>

      <div className="space-y-3">
        {sorted.map((day) => {
          const options = availableOptions(day, allowHalfDays);
          const current = optionOf(value.find((s) => s.campDayId === day.id));
          const fullOfParts =
            options.length === 0 && (day.morningFull || day.afternoonFull);
          return (
            <fieldset
              key={day.id}
              className="rounded-lg border border-gray-100 px-3 py-2.5"
            >
              <legend className="px-1 text-sm font-semibold text-gray-900 capitalize">
                {formatDateShort(day.date)}
              </legend>
              <div className="flex flex-wrap gap-x-5 gap-y-1.5">
                <label className="flex items-center gap-2 text-sm text-gray-500 cursor-pointer">
                  <input
                    type="radio"
                    name={`day_${day.id}`}
                    checked={current === "none"}
                    onChange={() => setDay(day, "none")}
                    className="h-4 w-4 accent-tennis-green"
                  />
                  {t("pickerNotComing")}
                </label>
                {options.map((option) => (
                  <label
                    key={option}
                    className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer"
                  >
                    <input
                      type="radio"
                      name={`day_${day.id}`}
                      checked={current === option}
                      onChange={() => setDay(day, option)}
                      className="h-4 w-4 accent-tennis-green"
                    />
                    {label(day, option)}
                  </label>
                ))}
                {day.morningFull && (
                  <span className="text-xs font-medium text-red-600">
                    {t("morning")} {t("pickerFull")}
                  </span>
                )}
                {day.afternoonFull && (
                  <span className="text-xs font-medium text-red-600">
                    {t("afternoon")} {t("pickerFull")}
                  </span>
                )}
                {fullOfParts && (
                  <span className="sr-only">{t("pickerFull")}</span>
                )}
              </div>
            </fieldset>
          );
        })}
      </div>

      {error && <p className="text-xs text-red-500">{error}</p>}

      {showTotal && (
        <div className="flex items-center justify-between rounded-lg bg-gray-50 px-3 py-2.5 text-sm">
          <span className="text-gray-600">{t("pickerTotal")}</span>
          <span className="font-semibold text-gray-900">
            {total > 0 ? formatAmount(total) : t("priceFree")}
          </span>
        </div>
      )}
    </div>
  );
}
