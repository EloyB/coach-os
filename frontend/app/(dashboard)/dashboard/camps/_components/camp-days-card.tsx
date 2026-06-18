"use client";

import { useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { CheckCircle2 } from "lucide-react";
import { CampDaysEditor } from "../new/_components/camp-days-editor";
import {
  buildCampRequest,
  detailToDays,
  toDateOnly,
  type CampDayDraft,
} from "../new/_types";
import { getTrainers, isAssignableTrainer } from "@/lib/api/trainers";
import { updateCamp, type CampDetailDto } from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";

interface CampDaysCardProps {
  camp: CampDetailDto;
}

export function CampDaysCard({ camp }: CampDaysCardProps) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();

  const initialDays = useMemo(() => detailToDays(camp), [camp]);
  const [days, setDays] = useState<CampDayDraft[]>(initialDays);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const [savedOk, setSavedOk] = useState(false);

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });
  const assignableTrainers = trainers.filter(isAssignableTrainer);

  const isDirty = JSON.stringify(days) !== JSON.stringify(initialDays);

  const mutation = useMutation({
    mutationFn: () =>
      updateCamp(camp.id, {
        ...buildCampRequest(
          {
            name: camp.name,
            description: camp.description ?? "",
            tennisClubId: camp.tennisClubId,
            level: camp.level,
            price: camp.price,
            maxParticipants: camp.maxParticipants,
            startDate: toDateOnly(camp.startDate),
            endDate: toDateOnly(camp.endDate),
            registrationDeadline: toDateOnly(camp.registrationDeadline),
          },
          days,
        ),
        isActive: camp.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camp", camp.id] });
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      setSavedOk(true);
      setTimeout(() => setSavedOk(false), 2500);
    },
    onError: (error) => {
      setErrorMessages(getAxiosErrorMessages(error, t("saveError")));
    },
  });

  function handleSave() {
    setErrorMessages([]);
    setSavedOk(false);
    mutation.mutate();
  }

  function handleReset() {
    setErrorMessages([]);
    setSavedOk(false);
    setDays(detailToDays(camp));
  }

  return (
    <div className="space-y-3">
      <CampDaysEditor
        days={days}
        onChange={(next) => {
          setSavedOk(false);
          setDays(next);
        }}
        trainers={assignableTrainers}
      />

      {errorMessages.length > 0 && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-sm text-red-600 space-y-1">
          {errorMessages.map((msg, i) => (
            <p key={i}>{msg}</p>
          ))}
        </div>
      )}

      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={handleSave}
          disabled={mutation.isPending || !isDirty}
          className="px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60"
        >
          {mutation.isPending ? t("saving") : t("save")}
        </button>
        {isDirty && (
          <button
            type="button"
            onClick={handleReset}
            disabled={mutation.isPending}
            className="px-4 py-2.5 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            {t("cancel")}
          </button>
        )}
        {savedOk && !isDirty && (
          <span className="inline-flex items-center gap-1.5 text-xs font-medium text-tennis-green">
            <CheckCircle2 size={14} />
            {t("daysSaved")}
          </span>
        )}
      </div>
    </div>
  );
}
