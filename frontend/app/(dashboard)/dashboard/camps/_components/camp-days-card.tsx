"use client";

import { useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { CheckCircle2 } from "lucide-react";
import { CampDaysEditor } from "../new/_components/camp-days-editor";
import {
  buildCampRequest,
  detailToDays,
  hasOverlappingShifts,
  toDateOnly,
  detailToPrices,
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
  const hasOverlap = hasOverlappingShifts(days);

  const mutation = useMutation({
    mutationFn: (daysToSave: CampDayDraft[]) =>
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
            prices: detailToPrices(camp),
          },
          daysToSave,
        ),
        prices: undefined,
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
    mutation.mutate(days);
  }

  /** Opslaan vanuit de dag-dialog: geeft foutmeldingen terug (leeg = gelukt). */
  async function saveDays(next: CampDayDraft[]): Promise<string[]> {
    setErrorMessages([]);
    setSavedOk(false);
    try {
      await mutation.mutateAsync(next);
      setDays(next);
      return [];
    } catch (error) {
      return getAxiosErrorMessages(error, t("saveError"));
    }
  }

  function handleReset() {
    setErrorMessages([]);
    setSavedOk(false);
    setDays(detailToDays(camp));
  }

  const status = hasOverlap
    ? { text: t("shiftsOverlapBlock"), tone: "text-red-600" }
    : isDirty
      ? { text: t("daysUnsaved"), tone: "text-amber-700" }
      : { text: t("daysAllSaved"), tone: "text-gray-400" };

  const footer = (
    <div className="sticky bottom-0 z-10 border-t border-gray-100 bg-white/95 px-6 py-3 backdrop-blur">
      {errorMessages.length > 0 && (
        <div className="mb-2 space-y-0.5 text-xs text-red-600">
          {errorMessages.map((msg, i) => (
            <p key={i}>{msg}</p>
          ))}
        </div>
      )}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p
          role="status"
          className={`inline-flex items-center gap-1.5 text-xs font-medium ${
            savedOk && !isDirty ? "text-tennis-green" : status.tone
          }`}
        >
          {savedOk && !isDirty ? (
            <>
              <CheckCircle2 size={14} />
              {t("daysSaved")}
            </>
          ) : (
            status.text
          )}
        </p>
        {isDirty && (
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={handleReset}
              disabled={mutation.isPending}
              className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 transition-colors hover:bg-gray-50"
            >
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={handleSave}
              disabled={mutation.isPending || hasOverlap}
              className="rounded-lg bg-tennis-green px-5 py-2 text-sm font-semibold text-white transition-colors hover:bg-tennis-green/90 disabled:opacity-60"
            >
              {mutation.isPending ? t("saving") : t("save")}
            </button>
          </div>
        )}
      </div>
    </div>
  );

  return (
    <CampDaysEditor
      days={days}
      onChange={(next) => {
        setSavedOk(false);
        setDays(next);
      }}
      trainers={assignableTrainers}
      onSaveDays={saveDays}
      footer={footer}
    />
  );
}
