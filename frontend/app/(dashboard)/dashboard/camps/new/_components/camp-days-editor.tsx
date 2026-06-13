"use client";

import { useTranslations } from "next-intl";
import { Trash2, Plus, Clock } from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import { isAssignableTrainer, type TrainerDto } from "@/lib/api/trainers";
import { clampTime, formatDayHeading, type CampDayDraft } from "../_types";

interface CampDaysEditorProps {
  days: CampDayDraft[];
  onChange: (days: CampDayDraft[]) => void;
  trainers: TrainerDto[];
}

export function CampDaysEditor({ days, onChange, trainers }: CampDaysEditorProps) {
  const t = useTranslations("camps");

  const assignableTrainers = trainers.filter(isAssignableTrainer);

  function updateDay(date: string, updates: Partial<CampDayDraft>) {
    onChange(days.map((d) => (d.date === date ? { ...d, ...updates } : d)));
  }

  function addTrainer(date: string, trainerId: string) {
    if (!trainerId) return;
    onChange(
      days.map((d) => {
        if (d.date !== date) return d;
        if (d.trainers.some((tr) => tr.trainerId === trainerId)) return d;
        return {
          ...d,
          trainers: [
            ...d.trainers,
            { trainerId, startTime: d.startTime, endTime: d.endTime },
          ],
        };
      }),
    );
  }

  function removeTrainer(date: string, trainerId: string) {
    onChange(
      days.map((d) =>
        d.date === date
          ? {
              ...d,
              trainers: d.trainers.filter((tr) => tr.trainerId !== trainerId),
            }
          : d,
      ),
    );
  }

  function updateTrainerTime(
    date: string,
    trainerId: string,
    field: "startTime" | "endTime",
    value: string,
  ) {
    onChange(
      days.map((d) => {
        if (d.date !== date) return d;
        return {
          ...d,
          trainers: d.trainers.map((tr) =>
            tr.trainerId === trainerId
              ? { ...tr, [field]: clampTime(value, d.startTime, d.endTime) }
              : tr,
          ),
        };
      }),
    );
  }

  function trainerName(trainerId: string): string {
    const tr = trainers.find((x) => x.id === trainerId);
    return tr ? `${tr.firstName} ${tr.lastName}` : trainerId;
  }

  const timeInputCls =
    "border border-gray-200 rounded-lg px-2.5 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green";

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
        <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
          <Clock size={13} className="text-tennis-green" />
        </div>
        <div>
          <h2 className="text-sm font-semibold text-gray-900">
            {t("daysTitle")}
          </h2>
          <p className="text-xs text-gray-400 mt-0.5">{t("daysDescription")}</p>
        </div>
      </div>

      <div className="p-6 space-y-3">
        {days.length === 0 && (
          <p className="text-xs text-gray-400 py-2">{t("noDays")}</p>
        )}

        {days.map((day) => {
          const available = assignableTrainers.filter(
            (tr) => !day.trainers.some((d) => d.trainerId === tr.id),
          );
          return (
            <div
              key={day.date}
              className="border border-gray-100 rounded-xl p-4 bg-[#FAFAF8]"
            >
              <div className="text-[13px] font-bold text-tennis-green mb-3">
                {formatDayHeading(day.date)}
              </div>

              {/* Camp hours */}
              <p className="text-[10.5px] uppercase tracking-[0.04em] text-gray-400 mb-1.5">
                {t("dayHours")}
              </p>
              <div className="flex items-center gap-2 mb-3 pb-3 border-b border-dashed border-gray-200">
                <input
                  type="time"
                  value={day.startTime}
                  onChange={(e) =>
                    updateDay(day.date, { startTime: e.target.value })
                  }
                  className={timeInputCls}
                />
                <span className="text-gray-400 text-xs">{t("trainerEndTime")}</span>
                <input
                  type="time"
                  value={day.endTime}
                  onChange={(e) =>
                    updateDay(day.date, { endTime: e.target.value })
                  }
                  className={timeInputCls}
                />
              </div>

              {/* Trainers present */}
              <p className="text-[10.5px] uppercase tracking-[0.04em] text-gray-400 mb-1.5">
                {t("dayTrainers")}
              </p>
              <div className="space-y-2">
                {day.trainers.map((tr) => (
                  <div key={tr.trainerId} className="flex items-center gap-2.5">
                    <span className="flex-1 text-[13px] font-semibold text-gray-900">
                      {trainerName(tr.trainerId)}
                    </span>
                    <input
                      type="time"
                      value={tr.startTime}
                      min={day.startTime}
                      max={day.endTime}
                      onChange={(e) =>
                        updateTrainerTime(
                          day.date,
                          tr.trainerId,
                          "startTime",
                          e.target.value,
                        )
                      }
                      className={timeInputCls + " px-2 py-1"}
                    />
                    <span className="text-gray-400 text-xs">-</span>
                    <input
                      type="time"
                      value={tr.endTime}
                      min={day.startTime}
                      max={day.endTime}
                      onChange={(e) =>
                        updateTrainerTime(
                          day.date,
                          tr.trainerId,
                          "endTime",
                          e.target.value,
                        )
                      }
                      className={timeInputCls + " px-2 py-1"}
                    />
                    <button
                      type="button"
                      onClick={() => removeTrainer(day.date, tr.trainerId)}
                      aria-label={t("removeTrainer")}
                      className="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors"
                    >
                      <Trash2 size={13} />
                    </button>
                  </div>
                ))}
              </div>

              {/* Add trainer */}
              {available.length > 0 ? (
                <div className="mt-3 flex items-center gap-2">
                  <Plus size={12} className="text-tennis-green" />
                  <NativeSelect
                    value=""
                    onChange={(e) => addTrainer(day.date, e.target.value)}
                  >
                    <option value="">{t("addTrainer")}</option>
                    {available.map((tr) => (
                      <option key={tr.id} value={tr.id}>
                        {tr.firstName} {tr.lastName}
                      </option>
                    ))}
                  </NativeSelect>
                </div>
              ) : (
                assignableTrainers.length === 0 && (
                  <p className="text-[11px] text-gray-400 mt-3">
                    {t("noTrainers")}
                  </p>
                )
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
