"use client";

import { Input } from "@/components/ui/input";
import type { CampFormFieldDto } from "@/lib/api/camps";

const FIELD_TYPE_MULTIPLE_CHOICE = 2;
const FIELD_TYPE_YES_NO = 3;

interface CampFormFieldInputsProps {
  fields: CampFormFieldDto[];
  values: Record<string, string>;
  onChange: (fieldId: string, value: string) => void;
  /** Voorvoegsel om radio-groepnamen uniek te houden wanneer er meerdere formulieren open staan. */
  idPrefix: string;
}

/**
 * Rendert de custom formuliervelden van een kamp (tekst, keuzelijst, ja/nee).
 * Herbruikbaar voor elke beheerder-actie die antwoorden moet verzamelen
 * (lid toevoegen aan groep, handmatig inschrijven).
 */
export function CampFormFieldInputs({
  fields,
  values,
  onChange,
  idPrefix,
}: CampFormFieldInputsProps) {
  const ordered = [...fields].sort((a, b) => a.order - b.order);

  return (
    <>
      {ordered.map((field) => {
        const value = values[field.id] ?? "";
        const label = (
          <span className="block text-sm font-medium">
            {field.label}
            {field.isRequired && <span className="ml-1 text-red-500">*</span>}
          </span>
        );

        if (field.type === FIELD_TYPE_MULTIPLE_CHOICE && field.options) {
          return (
            <fieldset key={field.id} className="space-y-1.5">
              <legend>{label}</legend>
              <div className="space-y-1.5">
                {field.options.map((opt) => (
                  <label
                    key={opt}
                    className="flex cursor-pointer items-center gap-2"
                  >
                    <input
                      type="radio"
                      name={`${idPrefix}_${field.id}`}
                      value={opt}
                      checked={value === opt}
                      required={field.isRequired}
                      onChange={() => onChange(field.id, opt)}
                      className="h-4 w-4 text-tennis-green focus:ring-tennis-green"
                    />
                    <span className="text-sm text-gray-700">{opt}</span>
                  </label>
                ))}
              </div>
            </fieldset>
          );
        }

        if (field.type === FIELD_TYPE_YES_NO) {
          return (
            <fieldset key={field.id} className="space-y-1.5">
              <legend>{label}</legend>
              <div className="flex items-center gap-4">
                {["Ja", "Nee"].map((opt) => (
                  <label
                    key={opt}
                    className="flex cursor-pointer items-center gap-2"
                  >
                    <input
                      type="radio"
                      name={`${idPrefix}_${field.id}`}
                      value={opt}
                      checked={value === opt}
                      required={field.isRequired}
                      onChange={() => onChange(field.id, opt)}
                      className="h-4 w-4 text-tennis-green focus:ring-tennis-green"
                    />
                    <span className="text-sm text-gray-700">{opt}</span>
                  </label>
                ))}
              </div>
            </fieldset>
          );
        }

        return (
          <label key={field.id} className="block">
            {label}
            <Input
              value={value}
              required={field.isRequired}
              onChange={(e) => onChange(field.id, e.target.value)}
              className="mt-1"
            />
          </label>
        );
      })}
    </>
  );
}
