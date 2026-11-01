"use client";

import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ChevronRight } from "lucide-react";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import {
  CampBasisFields,
  campBasisSchema,
  type CampBasisFormValues,
} from "./camp-basis-fields";
import { useState } from "react";
import { DEFAULT_DAY_PARTS, type CampDayParts, type CampStep1Data } from "../_types";
import {
  CampDefaultPartsFields,
  defaultPartsError,
} from "./camp-default-parts-fields";

interface Step1Props {
  defaultValues: CampStep1Data | null;
  onNext: (data: CampStep1Data) => void;
}

export function Step1Basis({ defaultValues, onNext }: Step1Props) {
  const t = useTranslations("camps");

  const { data: clubs = [], isLoading: clubsLoading } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const {
    register,
    control,
    handleSubmit,
    getValues,
    formState: { errors },
  } = useForm<CampBasisFormValues>({
    resolver: zodResolver(campBasisSchema),
    mode: "onBlur",
    defaultValues: defaultValues ?? {
      name: "",
      description: "",
      tennisClubId: "",
      level: null,
      dayPrice: 0,
      allowHalfDays: false,
      halfDayPrice: 0,
      maxParticipants: null,
      startDate: "",
      endDate: "",
      registrationDeadline: "",
    },
  });

  const [defaultParts, setDefaultParts] = useState<CampDayParts>(
    defaultValues?.defaultParts ?? DEFAULT_DAY_PARTS,
  );
  const partsError = defaultPartsError(defaultParts);

  function submit(values: CampBasisFormValues) {
    if (partsError) return;
    onNext({
      name: values.name,
      description: values.description ?? "",
      tennisClubId: values.tennisClubId,
      level: values.level,
      dayPrice: values.dayPrice,
      halfDayPrice: values.halfDayPrice,
      allowHalfDays: values.allowHalfDays,
      maxParticipants: values.maxParticipants,
      startDate: values.startDate,
      endDate: values.endDate,
      registrationDeadline: values.registrationDeadline,
      defaultParts,
    });
  }

  return (
    <form onSubmit={handleSubmit(submit)}>
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6">
        <CampBasisFields
          register={register}
          control={control}
          errors={errors}
          clubs={clubs}
          clubsLoading={clubsLoading}
          getDates={() => ({
            startDate: getValues("startDate"),
            endDate: getValues("endDate"),
          })}
        />
        <div className="mt-5">
          <CampDefaultPartsFields
            value={defaultParts}
            onChange={setDefaultParts}
            error={partsError}
          />
        </div>
      </div>

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
