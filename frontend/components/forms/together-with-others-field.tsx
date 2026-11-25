"use client";

import { useId } from "react";
import { useTranslations } from "next-intl";

interface TogetherWithOthersFieldProps {
  /** Het gekozen maximum aantal personen per tijdslot; telt pas mee zodra er een keuze gemaakt is. */
  value: number;
  /** Is de vraag al beantwoord? Zolang dat niet zo is, staat er niets geselecteerd. */
  answered: boolean;
  /** null = liever niet: het maximum volgt dan altijd de eigen groepsgrootte. */
  onChange: (value: number | null) => void;
  /** De eigen groepsgrootte: "Liever niet" betekent een maximum gelijk aan deze waarde. */
  own: number;
  /** Hoogste keuze: wat de reeks toelaat (hoogstens 4). */
  max: number;
  /** Toont de rand in de foutkleur (de vraag is verplicht en nog niet beantwoord). */
  invalid?: boolean;
}

/**
 * "Samen met andere spelers?": liever niet, of toelaten tot een bepaald aantal personen. Een groep van twee
 * ziet bijvoorbeeld "Liever niet", "Tot 3 personen" en "Tot 4 personen". Bewust zonder voorselectie: de speler
 * moet er één kiezen.
 */
export function TogetherWithOthersField({
  value,
  answered,
  onChange,
  own,
  max,
  invalid = false,
}: TogetherWithOthersFieldProps) {
  const t = useTranslations("enrollments");
  const name = useId();
  const options = Array.from({ length: Math.max(0, max - own + 1) }, (_, i) => own + i);

  return (
    <div
      role="radiogroup"
      aria-label={t("together_title")}
      aria-required="true"
      aria-invalid={invalid}
      className={`flex w-full flex-col overflow-hidden rounded-lg border bg-white sm:inline-flex sm:w-auto sm:flex-row ${
        invalid ? "border-red-300" : "border-gray-200"
      }`}
    >
      {options.map((option, index) => {
        const selected = answered && option === value;
        return (
          <label
            key={option}
            className={`cursor-pointer px-3.5 py-2.5 text-sm transition sm:py-2 ${
              index > 0
                ? `border-t sm:border-t-0 sm:border-l ${invalid ? "border-red-300" : "border-gray-200"}`
                : ""
            } ${selected ? "bg-tennis-green/10 font-semibold text-gray-900" : "text-gray-700 hover:bg-gray-50"}`}
          >
            <input
              type="radio"
              name={`together-${name}`}
              value={option}
              checked={selected}
              onChange={() => onChange(option === own ? null : option)}
              className="sr-only"
            />
            {option === own ? t("together_none") : t("together_up_to", { count: option })}
          </label>
        );
      })}
    </div>
  );
}
