"use client";

import { useTranslations } from "next-intl";
import { Input } from "@/components/ui/input";
import type { CampEnrollmentFormDto } from "@/lib/api/camps";
import { CampFormFieldInputs } from "./camp-form-field-inputs";
import { CampPriceSelect } from "./camp-price-select";

export interface ParticipantValues {
  name: string;
  email: string;
  phone: string;
}

interface CampParticipantFieldsProps {
  values: ParticipantValues;
  onChange: (patch: Partial<ParticipantValues>) => void;
  form: CampEnrollmentFormDto | null | undefined;
  answers: Record<string, string>;
  onAnswerChange: (fieldId: string, value: string) => void;
  idPrefix: string;
  /** Kamp waarvan de prijsopties aangeboden worden; samen met `onPriceChange` verplicht om de keuze te tonen. */
  campId: string;
  priceId: string | null;
  onPriceChange: (priceId: string | null) => void;
}

/**
 * Gedeelde velden van de beheerder-dialogs voor kamp-deelnemers (lid toevoegen,
 * aanpassen, manueel inschrijven): naam, e-mail, telefoon + de custom formuliervelden.
 */
export function CampParticipantFields({
  values,
  onChange,
  form,
  answers,
  onAnswerChange,
  idPrefix,
  campId,
  priceId,
  onPriceChange,
}: CampParticipantFieldsProps) {
  const t = useTranslations("campEnrollmentsTable");

  return (
    <>
      <label className="block text-sm font-medium">
        {t("memberName")}
        <Input
          required
          value={values.name}
          onChange={(e) => onChange({ name: e.target.value })}
          className="mt-1"
        />
      </label>
      <label className="block text-sm font-medium">
        {t("memberEmail")}
        <Input
          required
          type="email"
          value={values.email}
          onChange={(e) => onChange({ email: e.target.value })}
          className="mt-1"
        />
      </label>
      <label className="block text-sm font-medium">
        {t("memberPhone")}
        <Input
          value={values.phone}
          onChange={(e) => onChange({ phone: e.target.value })}
          className="mt-1"
        />
      </label>
      <CampPriceSelect campId={campId} value={priceId} onChange={onPriceChange} />
      {form && (
        <CampFormFieldInputs
          fields={form.fields}
          values={answers}
          onChange={onAnswerChange}
          idPrefix={idPrefix}
        />
      )}
    </>
  );
}
