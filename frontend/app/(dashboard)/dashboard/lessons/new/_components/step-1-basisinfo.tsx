"use client";

import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { ChevronRight } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { FieldError } from "@/components/forms/field-error";
import { DatePicker } from "@/components/ui/date-picker";
import { inputClass } from "@/lib/styles";
import type { Step1Data } from "../_types";

const schema = z
  .object({
    name: z
      .string()
      .min(1, "Naam is verplicht")
      .max(100, "Naam mag maximaal 100 karakters zijn")
      .refine((v) => !/[<>]|&#|javascript:|on\w+\s*=/i.test(v), {
        message: "Naam mag geen HTML of scripttekens bevatten",
      }),
    price: z
      .number({ message: "Prijs is verplicht" })
      .min(0, "Prijs mag niet negatief zijn")
      .max(100000, "Prijs is onrealistisch hoog"),
    tennisClubId: z.string().min(1, "Tennisclub is verplicht"),
    startDate: z.string().min(1, "Startdatum is verplicht"),
    endDate: z.string().min(1, "Einddatum is verplicht"),
    maxRegistrations: z
      .number({ message: "Maximum inschrijvingen is verplicht" })
      .min(1, "Minimaal 1 inschrijving toestaan")
      .max(500, "Maximum is 500 inschrijvingen"),
    minAge: z
      .number({ message: "Minimumleeftijd is verplicht" })
      .int("Gebruik een heel getal")
      .min(0, "Minimaal 0")
      .max(120, "Maximaal 120"),
    maxAge: z
      .number({ message: "Maximumleeftijd is verplicht" })
      .int("Gebruik een heel getal")
      .min(0, "Minimaal 0")
      .max(120, "Maximaal 120"),
    registrationDeadline: z.string().min(1, "Inschrijfdeadline is verplicht"),
  })
  .refine((d) => !d.startDate || !d.endDate || d.endDate >= d.startDate, {
    message: "Einddatum moet na startdatum zijn",
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
  )
  .refine((d) => d.minAge <= d.maxAge, {
    message: "Minimumleeftijd mag niet groter zijn dan de maximumleeftijd",
    path: ["maxAge"],
  });

type FormValues = z.infer<typeof schema>;

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

interface Step1Props {
  defaultValues: Step1Data | null;
  onNext: (data: Step1Data) => void;
}

export function Step1Basisinfo({ defaultValues, onNext }: Step1Props) {
  const t = useTranslations("lessonWizard");

  const { data: clubs, isLoading: clubsLoading } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    mode: "onBlur",
    defaultValues: defaultValues ?? {
      price: 0,
      maxRegistrations: 0,
      minAge: 3,
      maxAge: 99,
    },
  });

  return (
    <form onSubmit={handleSubmit(onNext)}>
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6 space-y-5">
        {/* Naam */}
        <div>
          <Label required>{t("name")}</Label>
          <input
            {...register("name")}
            type="text"
            maxLength={100}
            placeholder={t("namePlaceholder")}
            className={inputClass}
          />
          <FieldError message={errors.name?.message} />
        </div>

        {/* Prijs */}
        <div>
          <Label required>{t("price")}</Label>
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
          <FieldError message={errors.price?.message} />
        </div>

        {/* Max leerlingen */}
        <div>
          <Label required>{t("maxRegistrations")}</Label>
          <input
            {...register("maxRegistrations", { valueAsNumber: true })}
            type="number"
            min={1}
            max={500}
            className={inputClass}
          />
          <FieldError message={errors.maxRegistrations?.message} />
        </div>

        {/* Leeftijdsgrens */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <Label required>{t("minAge")}</Label>
            <input
              {...register("minAge", { valueAsNumber: true })}
              type="number"
              min={0}
              max={120}
              className={inputClass}
            />
            <FieldError message={errors.minAge?.message} />
          </div>
          <div>
            <Label required>{t("maxAge")}</Label>
            <input
              {...register("maxAge", { valueAsNumber: true })}
              type="number"
              min={0}
              max={120}
              className={inputClass}
            />
            <FieldError message={errors.maxAge?.message} />
          </div>
        </div>

        {/* Tennisclub */}
        <div>
          <Label required>{t("club")}</Label>
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
                  <SelectValue
                    placeholder={
                      clubsLoading ? t("clubLoading") : t("clubPlaceholder")
                    }
                  />
                </SelectTrigger>
                <SelectContent>
                  {clubs?.map((c) => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.name}
                    </SelectItem>
                  ))}
                  {!clubsLoading && !clubs?.length && (
                    <div className="px-3 py-2 text-sm text-gray-400">
                      {t("noClubsFound")}
                    </div>
                  )}
                </SelectContent>
              </Select>
            )}
          />
          <FieldError message={errors.tennisClubId?.message} />
        </div>

        {/* Startdatum + Einddatum */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label required>{t("startDate")}</Label>
            <Controller
              control={control}
              name="startDate"
              render={({ field }) => (
                <DatePicker value={field.value} onChange={field.onChange} />
              )}
            />
            <FieldError message={errors.startDate?.message} />
          </div>
          <div>
            <Label required>{t("endDate")}</Label>
            <Controller
              control={control}
              name="endDate"
              render={({ field }) => (
                <DatePicker value={field.value} onChange={field.onChange} />
              )}
            />
            <FieldError message={errors.endDate?.message} />
          </div>
        </div>

        {/* Inschrijfdeadline */}
        <div>
          <Label required>{t("registrationDeadline")}</Label>
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

      {/* Next */}
      <div className="flex justify-end mt-5">
        <button
          type="submit"
          className="flex items-center gap-2 px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
        >
          {t("next")}
          <ChevronRight size={15} />
        </button>
      </div>
    </form>
  );
}
