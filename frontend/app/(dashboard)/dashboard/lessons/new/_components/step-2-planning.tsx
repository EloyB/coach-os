"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ChevronLeft, ChevronRight } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { LESSON_LEVELS } from "@/lib/api/lessonSeries";
import { getTrainers, isAssignableTrainer } from "@/lib/api/trainers";
import { getTrainerAvailabilities } from "@/lib/api/trainerAvailabilities";
import { inputClass } from "@/lib/styles";
import type { WizardSlot } from "../_types";
import { CalendarWeekView, type SlotDefaults } from "./calendar-week-view";

interface Step2Props {
  slots: WizardSlot[];
  tennisClubId: string;
  onNext: (slots: WizardSlot[]) => void;
  onBack: () => void;
}

export function Step2Planning({
  slots: initialSlots,
  tennisClubId,
  onNext,
  onBack,
}: Step2Props) {
  const t = useTranslations("lessonWizard");
  const [slots, setSlots] = useState<WizardSlot[]>(initialSlots);

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });
  const assignableTrainers = trainers.filter(isAssignableTrainer);

  const { data: availabilities = [] } = useQuery({
    queryKey: ["trainerAvailabilities"],
    queryFn: getTrainerAvailabilities,
  });

  const clubTrainerIds = new Set(
    availabilities
      // null club = beschikbaar bij eender welke club -> matcht deze club ook.
      .filter((a) => a.tennisClubId === null || a.tennisClubId === tennisClubId)
      .map((a) => a.trainerId)
  );
  const sortedAssignableTrainers = [...assignableTrainers].sort(
    (a, b) => Number(clubTrainerIds.has(b.id)) - Number(clubTrainerIds.has(a.id))
  );

  const [defaults, setDefaults] = useState<SlotDefaults>({
    trainerId: null,
    trainerName: null,
    courtName: null,
    maxStudents: 4,
    level: null,
  });

  function handleTrainerChange(trainerId: string) {
    if (trainerId === "none") {
      setDefaults((d) => ({ ...d, trainerId: null, trainerName: null }));
    } else {
      const trainer = trainers.find((tr) => tr.id === trainerId);
      setDefaults((d) => ({
        ...d,
        trainerId,
        trainerName: trainer
          ? `${trainer.firstName} ${trainer.lastName}`
          : null,
      }));
    }
  }

  function handleLevelChange(value: string) {
    setDefaults((d) => ({
      ...d,
      level: value === "none" ? null : parseInt(value),
    }));
  }

  return (
    <div className="space-y-5">
      {/* Header */}
      <div>
        <h2 className="font-semibold text-gray-900 mb-1">{t("step2Title")}</h2>
        <p className="text-sm text-gray-400">{t("step2Desc")}</p>
      </div>

      {/* Calendar + defaults sidebar */}
      <div className="flex gap-5 items-start">
        {/* Calendar */}
        <div className="flex-1 min-w-0">
          <CalendarWeekView
            slots={slots}
            onChange={setSlots}
            defaults={defaults}
            tennisClubId={tennisClubId}
          />
        </div>

        {/* Defaults card */}
        <div className="w-56 shrink-0 sticky top-8">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 space-y-4">
            <div>
              <h3 className="text-sm font-semibold text-gray-900 mb-0.5">
                {t("defaultsTitle")}
              </h3>
              <p className="text-[11px] text-gray-400 leading-snug">
                {t("defaultsDesc")}
              </p>
            </div>

            {/* Trainer */}
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                {t("trainer")}
              </label>
              <Select
                onValueChange={handleTrainerChange}
                value={defaults.trainerId ?? "none"}
              >
                <SelectTrigger className="border border-gray-200 rounded-lg h-8 text-xs focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                  <SelectValue placeholder={t("trainerPlaceholder")} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">
                    <span className="text-gray-400">{t("nonePicker")}</span>
                  </SelectItem>
                  {sortedAssignableTrainers.map((tr) => (
                    <SelectItem key={tr.id} value={tr.id}>
                      <span className="inline-flex items-center gap-1.5">
                        {clubTrainerIds.has(tr.id) && (
                          <span
                            className="h-1.5 w-1.5 shrink-0 rounded-full bg-tennis-green"
                            title={t("availableBadge")}
                            aria-label={t("availableBadge")}
                          />
                        )}
                        {tr.firstName} {tr.lastName}
                      </span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Court name */}
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                {t("courtName")}
              </label>
              <input
                type="text"
                value={defaults.courtName ?? ""}
                onChange={(e) =>
                  setDefaults((d) => ({
                    ...d,
                    courtName: e.target.value || null,
                  }))
                }
                placeholder={t("courtNamePlaceholder")}
                className={inputClass + " !h-8 !text-xs"}
              />
            </div>

            {/* Max students */}
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                {t("maxStudents")}
              </label>
              <input
                type="number"
                min={1}
                value={defaults.maxStudents}
                onChange={(e) =>
                  setDefaults((d) => ({
                    ...d,
                    maxStudents: parseInt(e.target.value) || 1,
                  }))
                }
                className={inputClass + " !h-8 !text-xs"}
              />
            </div>

            {/* Level */}
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">
                {t("level")}
              </label>
              <Select
                onValueChange={handleLevelChange}
                value={defaults.level !== null ? String(defaults.level) : "none"}
              >
                <SelectTrigger className="border border-gray-200 rounded-lg h-8 text-xs focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green">
                  <SelectValue placeholder={t("levelNone")} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">
                    <span className="text-gray-400">{t("levelNone")}</span>
                  </SelectItem>
                  {Object.entries(LESSON_LEVELS).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex items-center justify-between">
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-2 px-5 py-2.5 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          <ChevronLeft size={15} />
          {t("previous")}
        </button>
        <button
          type="button"
          onClick={() => onNext(slots)}
          className="flex items-center gap-2 px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
        >
          {t("next")}
          <ChevronRight size={15} />
        </button>
      </div>
    </div>
  );
}
