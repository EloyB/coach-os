"use client";

import { useTranslations } from "next-intl";
import {
  Controller,
  useWatch,
  type Control,
  type FieldErrors,
  type UseFormRegister,
} from "react-hook-form";
import * as z from "zod";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { DatePicker } from "@/components/ui/date-picker";
import { FieldError } from "@/components/forms/field-error";
import { inputClass } from "@/lib/styles";
import type { TennisClubDto } from "@/lib/api/tennisClubs";

// ─── Shared schema for the camp basis fields ──────────────────────────────────

export const campBasisSchema = z
  .object({
    name: z
      .string()
      .min(1, "Naam is verplicht")
      .max(100, "Naam mag maximaal 100 karakters zijn")
      .refine((v) => !/[<>]|&#|javascript:|on\w+\s*=/i.test(v), {
        message: "Naam mag geen HTML of scripttekens bevatten",
      }),
    description: z.string().max(2000, "Omschrijving is te lang").optional(),
    tennisClubId: z.string().min(1, "Club is verplicht"),
    level: z.number().nullable(),
    price: z
      .number({ message: "Prijs is verplicht" })
      .min(0, "Prijs mag niet negatief zijn")
      .max(100000, "Prijs is onrealistisch hoog"),
    maxParticipants: z
      .number()
      .min(1, "Minimaal 1 deelnemer")
      .max(500, "Maximum is 500 deelnemers")
      .nullable(),
    startDate: z.string().min(1, "Startdatum is verplicht"),
    endDate: z.string().min(1, "Einddatum is verplicht"),
    registrationDeadline: z.string().min(1, "Inschrijfdeadline is verplicht"),
  })
  .refine((d) => !d.startDate || !d.endDate || d.endDate >= d.startDate, {
    message: "Einddatum moet op of na de startdatum zijn",
    path: ["endDate"],
  })
  .refine(
    (d) =>
      !d.registrationDeadline ||
      !d.startDate ||
      d.registrationDeadline <= d.startDate,
    {
      message: "Inschrijfdeadline moet voor of op de startdatum zijn",
      path: ["registrationDeadline"],
    },
  );

export type CampBasisFormValues = z.infer<typeof campBasisSchema>;

function Label({
  children,
  required,
}: {
  children: React.ReactNode;
  required?: boolean;
}) {
  return (
    <label className="block text-sm font-medium text-gray-700 mb-1.5">
      {children}
      {required && <span className="text-red-400 ml-0.5">*</span>}
    </label>
  );
}

interface CampBasisFieldsProps {
  register: UseFormRegister<CampBasisFormValues>;
  control: Control<CampBasisFormValues>;
  errors: FieldErrors<CampBasisFormValues>;
  clubs: TennisClubDto[];
  clubsLoading?: boolean;
  /** Called whenever a date that affects the day list changes. */
  onDateRangeChange?: (startDate: string, endDate: string) => void;
  /** Current form values for the date range, so we can report both on change. */
  getDates: () => { startDate: string; endDate: string };
}

export function CampBasisFields({
  register,
  control,
  errors,
  clubs,
  clubsLoading = false,
  onDateRangeChange,
  getDates,
}: CampBasisFieldsProps) {
  const t = useTranslations("camps");
  // Reactive start-date value so the end-date picker disables earlier dates as
  // soon as a start date is picked (getValues is not reactive on its own).
  const watchedStartDate = useWatch({ control, name: "startDate" });

  return (
    <div className="space-y-5">
      {/* Naam */}
      <div>
        <Label required>{t("fieldName")}</Label>
        <input
          {...register("name")}
          type="text"
          maxLength={100}
          placeholder={t("fieldNamePlaceholder")}
          className={inputClass}
        />
        <FieldError message={errors.name?.message} />
      </div>

      {/* Omschrijving */}
      <div>
        <Label>{t("fieldDescription")}</Label>
        <textarea
          {...register("description")}
          rows={3}
          placeholder={t("fieldDescriptionPlaceholder")}
          className={inputClass + " resize-y"}
        />
        <FieldError message={errors.description?.message} />
      </div>

      {/* Club + Niveau */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label required>{t("fieldClub")}</Label>
          <Controller
            control={control}
            name="tennisClubId"
            render={({ field }) => (
              <Select
                onValueChange={field.onChange}
                value={field.value ?? ""}
                disabled={clubsLoading}
              >
                <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                  <SelectValue placeholder={t("selectClub")} />
                </SelectTrigger>
                <SelectContent>
                  {clubs.map((c) => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.name}
                    </SelectItem>
                  ))}
                  {!clubsLoading && clubs.length === 0 && (
                    <div className="px-3 py-2 text-sm text-gray-400">
                      {t("noClubs")}
                    </div>
                  )}
                </SelectContent>
              </Select>
            )}
          />
          <FieldError message={errors.tennisClubId?.message} />
        </div>

        <div>
          <Label>{t("fieldLevel")}</Label>
          <Controller
            control={control}
            name="level"
            render={({ field }) => (
              <Select
                onValueChange={(v) =>
                  field.onChange(v === "none" ? null : Number(v))
                }
                value={field.value == null ? "none" : String(field.value)}
              >
                <SelectTrigger className="border border-gray-200 rounded-lg h-9 text-sm focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                  <SelectValue placeholder={t("selectLevel")} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">{t("levelNone")}</SelectItem>
                  <SelectItem value="1">{t("level1")}</SelectItem>
                  <SelectItem value="2">{t("level2")}</SelectItem>
                  <SelectItem value="3">{t("level3")}</SelectItem>
                </SelectContent>
              </Select>
            )}
          />
          <FieldError message={errors.level?.message} />
        </div>
      </div>

      {/* Prijs + Max deelnemers */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label required>{t("fieldPrice")}</Label>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-gray-400 pointer-events-none">
              €
            </span>
            <input
              {...register("price", { valueAsNumber: true })}
              type="number"
              min={0}
              step={0.01}
              className={inputClass + " pl-7"}
            />
          </div>
          <p className="text-[11px] text-gray-400 mt-1">{t("fieldPriceHint")}</p>
          <FieldError message={errors.price?.message} />
        </div>

        <div>
          <Label>{t("fieldMaxParticipants")}</Label>
          <input
            {...register("maxParticipants", {
              setValueAs: (v) =>
                v === "" || v === null || v === undefined ? null : Number(v),
            })}
            type="number"
            min={1}
            max={500}
            className={inputClass}
          />
          <p className="text-[11px] text-gray-400 mt-1">
            {t("fieldMaxParticipantsHint")}
          </p>
          <FieldError message={errors.maxParticipants?.message} />
        </div>
      </div>

      {/* Startdatum + Einddatum */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label required>{t("fieldStartDate")}</Label>
          <Controller
            control={control}
            name="startDate"
            render={({ field }) => (
              <DatePicker
                value={field.value}
                onChange={(v) => {
                  field.onChange(v);
                  onDateRangeChange?.(v, getDates().endDate);
                }}
              />
            )}
          />
          <FieldError message={errors.startDate?.message} />
        </div>
        <div>
          <Label required>{t("fieldEndDate")}</Label>
          <Controller
            control={control}
            name="endDate"
            render={({ field }) => (
              <DatePicker
                value={field.value}
                minDate={watchedStartDate || undefined}
                onChange={(v) => {
                  field.onChange(v);
                  onDateRangeChange?.(getDates().startDate, v);
                }}
              />
            )}
          />
          <FieldError message={errors.endDate?.message} />
        </div>
      </div>

      {/* Inschrijfdeadline */}
      <div>
        <Label required>{t("fieldRegistrationDeadline")}</Label>
        <Controller
          control={control}
          name="registrationDeadline"
          render={({ field }) => (
            <DatePicker value={field.value} onChange={field.onChange} />
          )}
        />
        <FieldError message={errors.registrationDeadline?.message} />
      </div>
    </div>
  );
}
