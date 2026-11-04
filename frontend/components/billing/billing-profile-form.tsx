"use client";

import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import * as z from "zod";
import { BadgeCheck } from "lucide-react";
import type { BillingProfile, SaveBillingProfileRequest } from "@/lib/api/billing";
import { FieldError } from "@/components/forms/field-error";
import { isValidBelgianVat } from "@/lib/billing";
import { btnPrimary, inputClass } from "@/lib/styles";

interface BillingProfileFormProps {
  profile: BillingProfile | null;
  defaultName: string;
  pending: boolean;
  onSave: (request: SaveBillingProfileRequest) => void;
}

/** Facturatiegegevens. Land staat vast op België (CoachOS is voorlopig enkel Belgisch). */
export function BillingProfileForm({ profile, defaultName, pending, onSave }: BillingProfileFormProps) {
  const t = useTranslations("billing");

  const schema = useMemo(
    () =>
      z.object({
        name: z.string().trim().min(1, t("profileRequired")).max(200),
        street: z.string().trim().min(1, t("profileRequired")).max(200),
        postalCode: z.string().trim().regex(/^\d{4}$/, t("profilePostalCodeInvalid")),
        city: z.string().trim().min(1, t("profileRequired")).max(100),
        invoiceEmail: z.email(t("profileEmailInvalid")),
        vatNumber: z
          .string()
          .trim()
          .refine((v) => v === "" || isValidBelgianVat(v), t("profileVatInvalid")),
      }),
    [t],
  );
  type FormValues = z.infer<typeof schema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: profile?.name ?? defaultName,
      street: profile?.street ?? "",
      postalCode: profile?.postalCode ?? "",
      city: profile?.city ?? "",
      invoiceEmail: profile?.invoiceEmail ?? "",
      vatNumber: profile?.vatNumber ?? "",
    },
  });

  function submit(values: FormValues) {
    onSave({
      ...values,
      country: "BE",
      vatNumber: values.vatNumber === "" ? null : values.vatNumber,
    });
  }

  return (
    <form onSubmit={handleSubmit(submit)} className="p-5 grid gap-3 sm:grid-cols-2">
      <label className="sm:col-span-2 text-xs font-semibold text-gray-700">
        {t("profileName")}
        <input {...register("name")} className={`${inputClass} mt-1`} autoComplete="organization" />
        <FieldError message={errors.name?.message} />
      </label>
      <label className="sm:col-span-2 text-xs font-semibold text-gray-700">
        {t("profileStreet")}
        <input {...register("street")} className={`${inputClass} mt-1`} autoComplete="street-address" />
        <FieldError message={errors.street?.message} />
      </label>
      <label className="text-xs font-semibold text-gray-700">
        {t("profilePostalCode")}
        <input {...register("postalCode")} className={`${inputClass} mt-1`} inputMode="numeric" autoComplete="postal-code" />
        <FieldError message={errors.postalCode?.message} />
      </label>
      <label className="text-xs font-semibold text-gray-700">
        {t("profileCity")}
        <input {...register("city")} className={`${inputClass} mt-1`} autoComplete="address-level2" />
        <FieldError message={errors.city?.message} />
      </label>
      <label className="text-xs font-semibold text-gray-700">
        {t("profileCountry")}
        <input value={t("profileCountryBelgium")} disabled className={`${inputClass} mt-1 bg-gray-50 text-gray-500`} />
      </label>
      <label className="text-xs font-semibold text-gray-700">
        {t("profileInvoiceEmail")}
        <input {...register("invoiceEmail")} type="email" className={`${inputClass} mt-1`} autoComplete="email" />
        <FieldError message={errors.invoiceEmail?.message} />
      </label>
      <label className="sm:col-span-2 text-xs font-semibold text-gray-700">
        {t("profileVatNumber")}
        <input {...register("vatNumber")} placeholder={t("profileVatPlaceholder")} className={`${inputClass} mt-1`} />
        <FieldError message={errors.vatNumber?.message} />
        {profile?.vatNumber && (
          <span className={`mt-1 flex items-center gap-1 text-[11px] font-normal ${profile.vatVerified ? "text-tennis-green" : "text-gray-400"}`}>
            {profile.vatVerified && <BadgeCheck size={12} />}
            {profile.vatVerified ? t("profileVatVerified") : t("profileVatUnverified")}
          </span>
        )}
      </label>
      <div className="sm:col-span-2">
        <button type="submit" disabled={pending} className={btnPrimary}>
          {t("profileSave")}
        </button>
      </div>
    </form>
  );
}
