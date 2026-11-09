"use client";

import { useTranslations } from "next-intl";
import type { CampDayParts } from "../_types";

const TIME_INPUT_CLS =
  "border border-gray-200 rounded-lg px-2 py-1.5 text-sm bg-white appearance-none min-w-0 focus:outline-none focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green";

/** Foutcode van de standaarduren, of null als ze kloppen (voormiddag voor namiddag, einde na start). */
export function defaultPartsError(parts: CampDayParts): "order" | null {
  const { morningStart, morningEnd, afternoonStart, afternoonEnd } = parts;
  if (
    !morningStart ||
    !morningEnd ||
    !afternoonStart ||
    !afternoonEnd ||
    morningEnd <= morningStart ||
    afternoonEnd <= afternoonStart ||
    afternoonStart < morningEnd
  )
    return "order";
  return null;
}

interface CampDefaultPartsFieldsProps {
  value: CampDayParts;
  onChange: (value: CampDayParts) => void;
  error: "order" | null;
}

/** Standaarduren van de voormiddag en namiddag; nieuwe kampdagen starten hiermee. */
export function CampDefaultPartsFields({
  value,
  onChange,
  error,
}: CampDefaultPartsFieldsProps) {
  const t = useTranslations("camps");

  function field(
    label: string,
    startKey: "morningStart" | "afternoonStart",
    endKey: "morningEnd" | "afternoonEnd",
  ) {
    return (
      <div>
        <p className="mb-1.5 text-sm font-medium text-gray-700">{label}</p>
        <div className="flex items-center gap-1.5">
          <input
            type="time"
            value={value[startKey] ?? ""}
            onChange={(e) => onChange({ ...value, [startKey]: e.target.value })}
            className={TIME_INPUT_CLS}
          />
          <span className="text-xs text-gray-400">{t("trainerEndTime")}</span>
          <input
            type="time"
            value={value[endKey] ?? ""}
            onChange={(e) => onChange({ ...value, [endKey]: e.target.value })}
            className={TIME_INPUT_CLS}
          />
        </div>
      </div>
    );
  }

  return (
    <div>
      <p className="text-sm font-medium text-gray-700">
        {t("defaultPartsTitle")}
      </p>
      <p className="text-[11px] text-gray-400 mb-3">{t("defaultPartsHint")}</p>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {field(t("morning"), "morningStart", "morningEnd")}
        {field(t("afternoon"), "afternoonStart", "afternoonEnd")}
      </div>
      {error && (
        <p className="mt-2 text-xs font-medium text-red-600">
          {t("defaultPartsOrder")}
        </p>
      )}
    </div>
  );
}
