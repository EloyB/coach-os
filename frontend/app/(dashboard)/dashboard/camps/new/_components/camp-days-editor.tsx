"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Trash2, Plus, Clock, Pencil, Check } from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { isAssignableTrainer, type TrainerDto } from "@/lib/api/trainers";
import { clampTime, formatDayHeading, type CampDayDraft } from "../_types";

interface CampDaysEditorProps {
  days: CampDayDraft[];
  onChange: (days: CampDayDraft[]) => void;
  trainers: TrainerDto[];
}

export function CampDaysEditor({ days, onChange, trainers }: CampDaysEditorProps) {
  const t = useTranslations("camps");
  // Welke trainer-kaart staat in uren-bewerkmodus (key = `${date}:${trainerId}`).
  const [editingTimes, setEditingTimes] = useState<string | null>(null);
  // Dialog om een trainer aan een dag toe te voegen (null = gesloten).
  const [addDialog, setAddDialog] = useState<{
    date: string;
    trainerId: string;
    start: string;
    end: string;
  } | null>(null);

  const assignableTrainers = trainers.filter(isAssignableTrainer);

  function updateDay(date: string, updates: Partial<CampDayDraft>) {
    onChange(days.map((d) => (d.date === date ? { ...d, ...updates } : d)));
  }

  function addTrainer(
    date: string,
    trainerId: string,
    startTime?: string,
    endTime?: string,
  ) {
    if (!trainerId) return;
    onChange(
      days.map((d) => {
        if (d.date !== date) return d;
        if (d.trainers.some((tr) => tr.trainerId === trainerId)) return d;
        return {
          ...d,
          trainers: [
            ...d.trainers,
            {
              trainerId,
              startTime: startTime ?? d.startTime,
              endTime: endTime ?? d.endTime,
            },
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

  function trainerName(draft: { trainerId: string; trainerName?: string }): string {
    const tr = trainers.find((x) => x.id === draft.trainerId);
    if (tr) return `${tr.firstName} ${tr.lastName}`;
    // Fall back to the server-resolved name carried in from the camp detail,
    // so names render even when the trainers list is unavailable.
    return draft.trainerName ?? draft.trainerId;
  }

  const timeInputCls =
    "border border-gray-200 rounded-lg px-2.5 py-1.5 text-sm bg-white appearance-none min-w-0 focus:outline-none focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green";

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
        <div className="w-6 h-6 shrink-0 rounded-md bg-tennis-green/10 flex items-center justify-center">
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
              <div className="mb-3 flex items-center justify-between gap-2">
                <div className="text-[13px] font-bold text-tennis-green">
                  {formatDayHeading(day.date)}
                </div>
                <button
                  type="button"
                  onClick={() =>
                    setAddDialog({
                      date: day.date,
                      trainerId: "",
                      start: day.startTime,
                      end: day.endTime,
                    })
                  }
                  disabled={available.length === 0}
                  aria-label={t("addTrainer")}
                  title={t("addTrainer")}
                  className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-tennis-green text-white transition-colors hover:bg-tennis-green/90 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  <Plus size={16} />
                </button>
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
                {day.trainers.map((tr) => {
                  const key = `${day.date}:${tr.trainerId}`;
                  const isEditing = editingTimes === key;
                  return (
                    <div
                      key={tr.trainerId}
                      className="rounded-lg border border-gray-200 bg-white p-3"
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0 flex-1">
                          <p className="text-[13px] font-semibold text-gray-900">
                            {trainerName(tr)}
                          </p>
                          {isEditing ? (
                            <div className="mt-2 flex items-center gap-2">
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
                            </div>
                          ) : (
                            <p className="mt-0.5 text-xs text-gray-500 tabular-nums">
                              {tr.startTime} – {tr.endTime}
                            </p>
                          )}
                        </div>
                        <div className="flex shrink-0 items-center gap-1">
                          <button
                            type="button"
                            onClick={() => setEditingTimes(isEditing ? null : key)}
                            aria-label={
                              isEditing
                                ? t("trainerTimesDone")
                                : t("editTrainerTimes")
                            }
                            className="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-tennis-green hover:bg-tennis-green/10 transition-colors"
                          >
                            {isEditing ? (
                              <Check size={14} />
                            ) : (
                              <Pencil size={13} />
                            )}
                          </button>
                          <button
                            type="button"
                            onClick={() => removeTrainer(day.date, tr.trainerId)}
                            aria-label={t("removeTrainer")}
                            className="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors"
                          >
                            <Trash2 size={13} />
                          </button>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>

              {assignableTrainers.length === 0 && (
                <p className="text-[11px] text-gray-400 mt-3">
                  {t("noTrainers")}
                </p>
              )}
            </div>
          );
        })}
      </div>

      {addDialog &&
        (() => {
          const day = days.find((d) => d.date === addDialog.date);
          const available = day
            ? assignableTrainers.filter(
                (tr) => !day.trainers.some((x) => x.trainerId === tr.id),
              )
            : [];
          return (
            <Dialog open onOpenChange={(o) => !o && setAddDialog(null)}>
              <DialogContent
                className="sm:max-w-md"
                onOpenAutoFocus={(e) => e.preventDefault()}
              >
                <DialogHeader>
                  <DialogTitle>{t("addTrainer")}</DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  <div>
                    <label className="mb-1 block text-xs font-medium text-gray-600">
                      {t("addTrainerTrainerLabel")}
                    </label>
                    <NativeSelect
                      value={addDialog.trainerId}
                      onChange={(e) =>
                        setAddDialog((d) =>
                          d ? { ...d, trainerId: e.target.value } : d,
                        )
                      }
                    >
                      <option value="">{t("selectTrainer")}</option>
                      {available.map((tr) => (
                        <option key={tr.id} value={tr.id}>
                          {tr.firstName} {tr.lastName}
                        </option>
                      ))}
                    </NativeSelect>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="min-w-0">
                      <label className="mb-1 block text-xs font-medium text-gray-600">
                        {t("dayStartTime")}
                      </label>
                      <input
                        type="time"
                        value={addDialog.start}
                        onChange={(e) =>
                          setAddDialog((d) =>
                            d ? { ...d, start: e.target.value } : d,
                          )
                        }
                        className={timeInputCls + " w-full"}
                      />
                    </div>
                    <div className="min-w-0">
                      <label className="mb-1 block text-xs font-medium text-gray-600">
                        {t("dayEndTime")}
                      </label>
                      <input
                        type="time"
                        value={addDialog.end}
                        onChange={(e) =>
                          setAddDialog((d) =>
                            d ? { ...d, end: e.target.value } : d,
                          )
                        }
                        className={timeInputCls + " w-full"}
                      />
                    </div>
                  </div>
                </div>
                <DialogFooter>
                  <button
                    type="button"
                    onClick={() => setAddDialog(null)}
                    className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
                  >
                    {t("cancel")}
                  </button>
                  <button
                    type="button"
                    disabled={!addDialog.trainerId}
                    onClick={() => {
                      addTrainer(
                        addDialog.date,
                        addDialog.trainerId,
                        addDialog.start,
                        addDialog.end,
                      );
                      setAddDialog(null);
                    }}
                    className="rounded-lg bg-tennis-green px-4 py-2 text-sm font-semibold text-white hover:bg-tennis-green/90 disabled:opacity-50"
                  >
                    {t("addTrainerSubmit")}
                  </button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          );
        })()}
    </div>
  );
}
