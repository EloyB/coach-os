"use client";

import { useId } from "react";
import { useTranslations } from "next-intl";

interface MaxGroupSizeFieldProps {
  /** Gekozen maximum aantal personen per tijdslot. */
  value: number;
  onChange: (value: number) => void;
  /** Laagste keuze: de eigen groepsgrootte (een groep van 2 kan geen maximum van 1 kiezen). */
  min: number;
  /** Hoogste keuze: wat de reeks toelaat (hoogstens 4). */
  max: number;
  /** Maakt de radiogroep uniek wanneer er meerdere op één pagina staan. */
  name?: string;
}

/**
 * Keuze van het maximum aantal personen waarmee iemand (of een groep) in één tijdslot wil zitten:
 * solo, duo, trio of groep van 4, begrensd door de eigen groepsgrootte en wat de reeks toelaat.
 */
export function MaxGroupSizeField({ value, onChange, min, max, name }: MaxGroupSizeFieldProps) {
  const t = useTranslations("maxGroupSize");
  const generatedName = useId();
  const groupName = name ?? `max-group-size-${generatedName}`;
  const options = Array.from({ length: Math.max(0, max - min + 1) }, (_, i) => min + i);

  return (
    <div role="radiogroup" aria-label={t("label")} className="flex flex-wrap gap-2">
      {options.map((option) => {
        const selected = option === value;
        return (
          <label
            key={option}
            className={`cursor-pointer rounded-lg border px-3 py-2 text-sm transition ${
              selected
                ? "border-tennis-green bg-tennis-green/10 text-gray-900"
                : "border-gray-200 bg-white text-gray-700 hover:border-tennis-green/40"
            }`}
          >
            <input
              type="radio"
              name={groupName}
              value={option}
              checked={selected}
              onChange={() => onChange(option)}
              className="sr-only"
            />
            <span className="font-medium">{t(`option${option}` as "option1")}</span>
            <span className="ml-1.5 text-xs text-gray-500">{t(`hint${option}` as "hint1")}</span>
          </label>
        );
      })}
    </div>
  );
}
