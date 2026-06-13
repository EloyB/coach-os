"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  CampBasisFields,
  campBasisSchema,
  type CampBasisFormValues,
} from "../new/_components/camp-basis-fields";
import { CampDaysEditor } from "../new/_components/camp-days-editor";
import {
  buildCampRequest,
  buildDays,
  detailToDays,
  toDateOnly,
  type CampDayDraft,
} from "../new/_types";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { getTrainers } from "@/lib/api/trainers";
import { updateCamp, type CampDetailDto } from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";

interface CampEditFormProps {
  camp: CampDetailDto;
  onCancel: () => void;
  onSaved: () => void;
}

export function CampEditForm({ camp, onCancel, onSaved }: CampEditFormProps) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [days, setDays] = useState<CampDayDraft[]>(detailToDays(camp));

  const { data: clubs = [], isLoading: clubsLoading } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });
  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
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
    defaultValues: {
      name: camp.name,
      description: camp.description ?? "",
      tennisClubId: camp.tennisClubId,
      level: camp.level,
      price: camp.price,
      maxParticipants: camp.maxParticipants,
      startDate: camp.startDate,
      endDate: camp.endDate,
      registrationDeadline: toDateOnly(camp.registrationDeadline),
    },
  });

  const mutation = useMutation({
    mutationFn: (values: CampBasisFormValues) =>
      updateCamp(camp.id, {
        ...buildCampRequest(
          {
            name: values.name,
            description: values.description ?? "",
            tennisClubId: values.tennisClubId,
            level: values.level,
            price: values.price,
            maxParticipants: values.maxParticipants,
            startDate: values.startDate,
            endDate: values.endDate,
            registrationDeadline: values.registrationDeadline,
          },
          days,
        ),
        isActive: camp.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      queryClient.invalidateQueries({ queryKey: ["camp", camp.id] });
      onSaved();
    },
    onError: (error) => {
      setErrorMessages(getAxiosErrorMessages(error, t("saveError")));
    },
  });

  function regenerateDays(startDate: string, endDate: string) {
    setDays((prev) => buildDays(startDate, endDate, prev));
  }

  return (
    <form
      onSubmit={handleSubmit((values) => {
        setErrorMessages([]);
        mutation.mutate(values);
      })}
      className="mt-5 space-y-5"
    >
      <CampBasisFields
        register={register}
        control={control}
        errors={errors}
        clubs={clubs}
        clubsLoading={clubsLoading}
        onDateRangeChange={regenerateDays}
        getDates={() => ({
          startDate: getValues("startDate"),
          endDate: getValues("endDate"),
        })}
      />

      <CampDaysEditor days={days} onChange={setDays} trainers={trainers} />

      {errorMessages.length > 0 && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-sm text-red-600 space-y-1">
          {errorMessages.map((msg, i) => (
            <p key={i}>{msg}</p>
          ))}
        </div>
      )}

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={mutation.isPending}
          className="px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60"
        >
          {mutation.isPending ? t("saving") : t("save")}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2.5 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          {t("deleteCancel")}
        </button>
      </div>
    </form>
  );
}
