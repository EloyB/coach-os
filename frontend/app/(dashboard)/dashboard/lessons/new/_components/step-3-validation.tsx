"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ChevronLeft, Pencil } from "lucide-react";
import { createLessonSeriesWizard } from "@/lib/api/lessonSeries";
import { getTrainers, isAssignableTrainer } from "@/lib/api/trainers";
import { formatDateShort } from "@/lib/date-utils";
import { toLocalISODate, type Step1Data, type WizardSlot, type WeekOverride } from "../_types";
import { EditWeekDialog } from "./edit-week-dialog";

// ─── Helpers ─────────────────────────────────────────────────────────────────

interface WeekInfo {
  index: number;
  startDate: string;
  endDate: string;
  days: string[]; // 7 ISO dates Mon–Sun
}

function computeWeeks(startDate: string, endDate: string): WeekInfo[] {
  const start = new Date(startDate + "T00:00:00");
  const end = new Date(endDate + "T00:00:00");

  // Find Monday of week containing start (0=Sun in JS)
  const dow = start.getDay();
  const daysBack = dow === 0 ? 6 : dow - 1;
  const firstMonday = new Date(start);
  firstMonday.setDate(firstMonday.getDate() - daysBack);

  const weeks: WeekInfo[] = [];
  const current = new Date(firstMonday);
  let index = 0;

  while (current <= end) {
    const sunday = new Date(current);
    sunday.setDate(sunday.getDate() + 6);

    weeks.push({
      index,
      startDate: toLocalISODate(current),
      endDate: toLocalISODate(sunday),
      days: Array.from({ length: 7 }, (_, i) => {
        const d = new Date(current);
        d.setDate(d.getDate() + i);
        return toLocalISODate(d);
      }),
    });

    current.setDate(current.getDate() + 7);
    index++;
  }

  return weeks;
}

const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

// ─── Component ───────────────────────────────────────────────────────────────

interface Step3Props {
  step1Data: Step1Data;
  weeklyTemplate: WizardSlot[];
  weekOverrides: WeekOverride[];
  onOverridesChange: (overrides: WeekOverride[]) => void;
  onBack: () => void;
}

export function Step3Validation({
  step1Data,
  weeklyTemplate,
  weekOverrides,
  onOverridesChange,
  onBack,
}: Step3Props) {
  const t = useTranslations("lessonWizard");
  const router = useRouter();
  const [editingWeekIndex, setEditingWeekIndex] = useState<number | null>(null);

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });
  const assignableTrainers = trainers.filter(isAssignableTrainer);

  const weeks = computeWeeks(step1Data.startDate, step1Data.endDate);

  const queryClient = useQueryClient();
  const mutation = useMutation({
    mutationFn: createLessonSeriesWizard,
    onSuccess: () => {
      // Eerste lesreeks voltooit de "series" onboarding-stap.
      queryClient.invalidateQueries({ queryKey: ["onboarding"] });
      router.push("/dashboard/lessons");
    },
  });

  function getSlotsForWeek(week: WeekInfo): WizardSlot[] {
    const override = weekOverrides.find((o) => o.weekIndex === week.index);
    return override ? override.slots : weeklyTemplate;
  }

  function handleWeekSave(weekIndex: number, slots: WizardSlot[]) {
    const existingIndex = weekOverrides.findIndex(
      (o) => o.weekIndex === weekIndex
    );
    if (existingIndex >= 0) {
      const updated = [...weekOverrides];
      updated[existingIndex] = {
        weekIndex,
        weekStartDate: weeks[weekIndex].startDate,
        slots,
      };
      onOverridesChange(updated);
    } else {
      onOverridesChange([
        ...weekOverrides,
        { weekIndex, weekStartDate: weeks[weekIndex].startDate, slots },
      ]);
    }
  }

  /** Expand template + overrides into a flat list of lessons with actual dates */
  function buildLessonsList() {
    const lessons: { date: string; startTime: string; endTime: string; trainerId?: string; courtName?: string; maxStudents: number; level?: number }[] = [];

    for (const week of weeks) {
      const slots = getSlotsForWeek(week);
      for (const slot of slots) {
        const dayDate = week.days[slot.dayOfWeek];
        // Only include days within the series range
        if (dayDate < step1Data.startDate || dayDate > step1Data.endDate) continue;

        lessons.push({
          date: dayDate,
          startTime: slot.startTime,
          endTime: slot.endTime,
          trainerId: slot.trainerId ?? undefined,
          courtName: slot.courtName ?? undefined,
          maxStudents: slot.maxStudents,
          level: slot.level ?? undefined,
        });
      }
    }

    return lessons;
  }

  function handleSubmit() {
    mutation.mutate({
      name: step1Data.name,
      price: step1Data.price,
      maxRegistrations: step1Data.maxRegistrations,
      minAge: step1Data.minAge,
      maxAge: step1Data.maxAge,
      tennisClubId: step1Data.tennisClubId,
      startDate: step1Data.startDate,
      endDate: step1Data.endDate,
      registrationDeadline: step1Data.registrationDeadline,
      allowSoloEnrollment: step1Data.allowSoloEnrollment,
      allowGroupEnrollment: step1Data.allowGroupEnrollment,
      acceptOnlinePayment: step1Data.acceptOnlinePayment,
      acceptManualPayment: step1Data.acceptManualPayment,
      weeklyTemplate: weeklyTemplate.map((s) => ({
        dayOfWeek: s.dayOfWeek,
        startTime: s.startTime,
        endTime: s.endTime,
        trainerId: s.trainerId ?? undefined,
        courtName: s.courtName ?? undefined,
        maxStudents: s.maxStudents,
      })),
      lessons: buildLessonsList(),
    });
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="bg-white rounded-xl shadow-sm shadow-gray-100 p-6">
        <h2 className="font-semibold text-gray-900 mb-1">{t("step3Title")}</h2>
        <p className="text-sm text-gray-400">{t("step3Desc")}</p>
      </div>

      {/* Error banner */}
      {mutation.isError && (
        <div className="bg-red-50 border border-red-100 rounded-lg p-4 text-sm text-red-600">
          {t("errorDesc")}
        </div>
      )}

      {/* Week cards */}
      {weeks.map((week) => {
        const slots = getSlotsForWeek(week);
        const hasOverride = weekOverrides.some((o) => o.weekIndex === week.index);

        return (
          <div
            key={week.index}
            className="bg-white rounded-xl shadow-sm shadow-gray-100 p-5"
          >
            {/* Week header */}
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <span className="text-sm font-semibold text-gray-900">
                  {t("weekLabel")} {week.index + 1}
                </span>
                <span className="text-xs text-gray-400">
                  {formatDateShort(week.startDate)} – {formatDateShort(week.endDate)}
                </span>
                {hasOverride && (
                  <span className="inline-flex items-center px-1.5 py-0.5 bg-amber-50 text-amber-600 text-[10px] font-medium rounded">
                    {t("overridden")}
                  </span>
                )}
              </div>
              <button
                type="button"
                onClick={() => setEditingWeekIndex(week.index)}
                className="flex items-center gap-1.5 text-xs text-gray-400 hover:text-tennis-green transition-colors px-2 py-1 rounded-md hover:bg-tennis-green/5"
              >
                <Pencil size={12} />
                {t("adjustWeek")}
              </button>
            </div>

            {/* Day columns */}
            <div className="overflow-x-auto">
              <div className="grid grid-cols-7 gap-2 min-w-[560px]">
                {week.days.map((dayDate, dayIndex) => {
                  const daySlots = slots
                    .filter((s) => s.dayOfWeek === dayIndex)
                    .sort((a, b) => a.startTime.localeCompare(b.startTime));

                  const isInRange =
                    dayDate >= step1Data.startDate &&
                    dayDate <= step1Data.endDate;

                  return (
                    <div key={dayIndex} className="flex flex-col gap-1.5">
                      {/* Day header */}
                      <div
                        className={`text-center py-1.5 rounded-lg ${
                          isInRange ? "bg-gray-50" : "bg-gray-50/40"
                        }`}
                      >
                        <p
                          className={`text-[10px] font-semibold ${
                            isInRange ? "text-gray-500" : "text-gray-300"
                          }`}
                        >
                          {DAY_NAMES_SHORT[dayIndex]}
                        </p>
                        <p
                          className={`text-[10px] ${
                            isInRange ? "text-gray-400" : "text-gray-200"
                          }`}
                        >
                          {formatDateShort(dayDate)}
                        </p>
                      </div>

                      {/* Slots */}
                      {isInRange &&
                        daySlots.map((slot) => (
                          <div
                            key={slot.id}
                            className="bg-tennis-green/5 border border-tennis-green/15 rounded p-1.5"
                          >
                            <p className="text-[10px] font-semibold text-gray-800">
                              {slot.startTime}–{slot.endTime}
                            </p>
                            <p className="text-[10px] text-gray-500 truncate">
                              {slot.courtName}
                            </p>
                            <p className="text-[10px] text-gray-400 truncate">
                              {slot.trainerName}
                            </p>
                          </div>
                        ))}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        );
      })}

      {/* Navigation */}
      <div className="flex items-center justify-between pt-2">
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
          onClick={handleSubmit}
          disabled={mutation.isPending}
          className="flex items-center gap-2 px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {mutation.isPending ? (
            <>
              <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              {t("creating")}
            </>
          ) : (
            t("createSeries")
          )}
        </button>
      </div>

      {/* Edit week dialog */}
      {editingWeekIndex !== null && (
        <EditWeekDialog
          weekIndex={editingWeekIndex}
          weekStartDate={weeks[editingWeekIndex].startDate}
          weekEndDate={weeks[editingWeekIndex].endDate}
          currentSlots={getSlotsForWeek(weeks[editingWeekIndex])}
          trainers={assignableTrainers}
          seriesStartDate={step1Data.startDate}
          seriesEndDate={step1Data.endDate}
          onSave={(slots) => handleWeekSave(editingWeekIndex, slots)}
          onClose={() => setEditingWeekIndex(null)}
        />
      )}
    </div>
  );
}
