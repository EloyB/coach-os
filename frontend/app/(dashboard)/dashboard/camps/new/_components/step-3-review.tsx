"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  ChevronLeft,
  Euro,
  CalendarDays,
  Building2,
  GraduationCap,
  Users,
  Clock,
} from "lucide-react";
import { createCamp } from "@/lib/api/camps";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import { getTrainers } from "@/lib/api/trainers";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { formatDateNL } from "@/lib/date-utils";
import {
  buildCampRequest,
  formatDayHeading,
  type CampDayDraft,
  type CampStep1Data,
} from "../_types";

interface Step3Props {
  step1Data: CampStep1Data;
  days: CampDayDraft[];
  onBack: () => void;
}

function Chip({
  icon,
  children,
}: {
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-[#F5F4F1] text-xs text-gray-600">
      {icon}
      {children}
    </span>
  );
}

export function Step3Review({ step1Data, days, onBack }: Step3Props) {
  const t = useTranslations("camps");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [errorMessages, setErrorMessages] = useState<string[]>([]);

  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });
  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const mutation = useMutation({
    mutationFn: () => createCamp(buildCampRequest(step1Data, days)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camps"] });
      router.push("/dashboard/camps");
    },
    onError: (error) => {
      setErrorMessages(getAxiosErrorMessages(error, t("saveError")));
    },
  });

  const clubName =
    clubs.find((c) => c.id === step1Data.tennisClubId)?.name ?? "";
  const levelLabel =
    step1Data.level == null ? t("levelNone") : t(`level${step1Data.level}`);

  function trainerName(trainerId: string): string {
    const tr = trainers.find((x) => x.id === trainerId);
    return tr ? `${tr.firstName} ${tr.lastName}` : trainerId;
  }

  return (
    <div>
      {/* ── Basis summary ── */}
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6">
        <h2 className="text-xl font-bold text-gray-900 leading-tight mb-1.5">
          {step1Data.name || t("fieldName")}
        </h2>
        {step1Data.description && (
          <p className="text-sm text-gray-500 mb-3">{step1Data.description}</p>
        )}
        <div className="flex flex-wrap gap-2">
          <Chip icon={<Euro size={11} className="text-tennis-green" />}>
            {step1Data.price > 0 ? `€${step1Data.price}` : t("priceFree")}
          </Chip>
          <Chip icon={<CalendarDays size={11} className="text-tennis-green" />}>
            {formatDateNL(step1Data.startDate)} -{" "}
            {formatDateNL(step1Data.endDate)}
          </Chip>
          <Chip icon={<GraduationCap size={11} className="text-tennis-green" />}>
            {levelLabel}
          </Chip>
          {clubName && (
            <Chip icon={<Building2 size={11} className="text-tennis-green" />}>
              {clubName}
            </Chip>
          )}
          <Chip icon={<Users size={11} className="text-tennis-green" />}>
            {t("fieldMaxParticipants")}:{" "}
            {step1Data.maxParticipants == null
              ? t("fieldMaxParticipantsHint")
              : step1Data.maxParticipants}
          </Chip>
          <Chip icon={<Clock size={11} className="text-tennis-green" />}>
            {t("publicDeadlineLabel")}:{" "}
            {formatDateNL(step1Data.registrationDeadline)}
          </Chip>
        </div>
      </div>

      {/* ── Days summary ── */}
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden mt-5">
        <div className="px-6 py-4 border-b border-gray-100">
          <h2 className="text-sm font-semibold text-gray-900">
            {t("daysTitle")}
          </h2>
        </div>
        <div className="p-6 space-y-3">
          {days.length === 0 && (
            <p className="text-xs text-gray-400">{t("noDays")}</p>
          )}
          {days.map((day) => (
            <div
              key={day.date}
              className="border border-gray-100 rounded-xl p-4 bg-[#FAFAF8]"
            >
              <div className="flex items-center justify-between mb-2">
                <span className="text-[13px] font-bold text-tennis-green">
                  {formatDayHeading(day.date)}
                </span>
                <span className="text-xs text-gray-500 font-mono">
                  {day.startTime} - {day.endTime}
                </span>
              </div>
              {day.trainers.length === 0 ? (
                <p className="text-xs text-gray-400">{t("dayTrainers")}: -</p>
              ) : (
                <ul className="space-y-1">
                  {day.trainers.map((tr) => (
                    <li
                      key={tr.trainerId}
                      className="flex items-center justify-between text-xs text-gray-700"
                    >
                      <span className="font-medium">
                        {trainerName(tr.trainerId)}
                      </span>
                      <span className="font-mono text-gray-500">
                        {tr.startTime} - {tr.endTime}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ))}
        </div>
      </div>

      {errorMessages.length > 0 && (
        <div className="bg-red-50 border border-red-100 rounded-xl p-4 text-sm text-red-600 space-y-1 mt-5">
          {errorMessages.map((msg, i) => (
            <p key={i}>{msg}</p>
          ))}
        </div>
      )}

      <div className="flex items-center justify-between mt-5">
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          <ChevronLeft size={15} />
          {t("wizardBack")}
        </button>
        <button
          type="button"
          onClick={() => {
            setErrorMessages([]);
            mutation.mutate();
          }}
          disabled={mutation.isPending}
          className="px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60"
        >
          {mutation.isPending ? t("saving") : t("wizardSubmit")}
        </button>
      </div>
    </div>
  );
}
