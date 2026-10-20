"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Check, Clock, Pencil, Plus, Trash2, X } from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { isAssignableTrainer, type TrainerDto } from "@/lib/api/trainers";
import {
  clampTime,
  findOverlappingShiftIndexes,
  formatDayHeading,
  shiftHours,
  shiftsOverlap,
  type CampDayDraft,
} from "../_types";

interface CampDaysEditorProps {
  days: CampDayDraft[];
  onChange: (days: CampDayDraft[]) => void;
  trainers: TrainerDto[];
}

/** Dialog-state om een trainer (of een extra shift van een trainer) aan een dag toe te voegen. */
interface ShiftDialogState {
  date: string;
  trainerId: string;
  /** True bij "+ shift" bij een bestaande trainer: de trainer ligt dan vast. */
  trainerLocked: boolean;
  start: string;
  end: string;
}

interface TrainerGroup {
  trainerId: string;
  trainerName?: string;
  /** Shiften met hun index in `day.trainers`, op begintijd gesorteerd. */
  shifts: { index: number; startTime: string; endTime: string }[];
}

function groupByTrainer(day: CampDayDraft): TrainerGroup[] {
  const groups = new Map<string, TrainerGroup>();
  day.trainers.forEach((tr, index) => {
    const group = groups.get(tr.trainerId) ?? {
      trainerId: tr.trainerId,
      trainerName: tr.trainerName,
      shifts: [],
    };
    group.shifts.push({ index, startTime: tr.startTime, endTime: tr.endTime });
    groups.set(tr.trainerId, group);
  });
  return [...groups.values()].map((g) => ({
    ...g,
    shifts: [...g.shifts].sort((a, b) => a.startTime.localeCompare(b.startTime)),
  }));
}

function formatHours(hours: number): string {
  return hours.toLocaleString("nl-BE", { maximumFractionDigits: 1 });
}

const TIME_INPUT_CLS =
  "border border-gray-200 rounded-lg px-2 py-1 text-sm bg-white appearance-none min-w-0 focus:outline-none focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green";

export function CampDaysEditor({ days, onChange, trainers }: CampDaysEditorProps) {
  const t = useTranslations("camps");
  // Welke shift staat in uren-bewerkmodus (key = `${date}:${index in day.trainers}`).
  const [editingShift, setEditingShift] = useState<string | null>(null);
  const [dialog, setDialog] = useState<ShiftDialogState | null>(null);

  const assignableTrainers = trainers.filter(isAssignableTrainer);

  function updateDay(date: string, updates: Partial<CampDayDraft>) {
    onChange(days.map((d) => (d.date === date ? { ...d, ...updates } : d)));
  }

  function addShift(date: string, trainerId: string, start: string, end: string) {
    if (!trainerId) return;
    onChange(
      days.map((d) =>
        d.date === date
          ? {
              ...d,
              trainers: [...d.trainers, { trainerId, startTime: start, endTime: end }],
            }
          : d,
      ),
    );
  }

  function removeShift(date: string, index: number) {
    setEditingShift(null);
    onChange(
      days.map((d) =>
        d.date === date
          ? { ...d, trainers: d.trainers.filter((_, i) => i !== index) }
          : d,
      ),
    );
  }

  function removeTrainer(date: string, trainerId: string) {
    setEditingShift(null);
    onChange(
      days.map((d) =>
        d.date === date
          ? { ...d, trainers: d.trainers.filter((tr) => tr.trainerId !== trainerId) }
          : d,
      ),
    );
  }

  function updateShiftTime(
    date: string,
    index: number,
    field: "startTime" | "endTime",
    value: string,
  ) {
    onChange(
      days.map((d) =>
        d.date === date
          ? {
              ...d,
              trainers: d.trainers.map((tr, i) =>
                i === index
                  ? { ...tr, [field]: clampTime(value, d.startTime, d.endTime) }
                  : tr,
              ),
            }
          : d,
      ),
    );
  }

  function trainerName(group: { trainerId: string; trainerName?: string }): string {
    const tr = trainers.find((x) => x.id === group.trainerId);
    if (tr) return `${tr.firstName} ${tr.lastName}`;
    // Valt terug op de door de server opgeloste naam, zodat namen ook zonder trainerslijst tonen.
    return group.trainerName ?? group.trainerId;
  }

  function openNewTrainerDialog(day: CampDayDraft) {
    setDialog({
      date: day.date,
      trainerId: "",
      trainerLocked: false,
      start: day.startTime,
      end: day.endTime,
    });
  }

  function openNewShiftDialog(day: CampDayDraft, group: TrainerGroup) {
    // Stel de tijd na de laatste shift voor, zolang daar nog ruimte voor is.
    const lastEnd = group.shifts.reduce(
      (max, s) => (s.endTime > max ? s.endTime : max),
      day.startTime,
    );
    const hasRoom = lastEnd < day.endTime;
    setDialog({
      date: day.date,
      trainerId: group.trainerId,
      trainerLocked: true,
      start: hasRoom ? lastEnd : day.startTime,
      end: day.endTime,
    });
  }

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
          const groups = groupByTrainer(day);
          const overlapping = findOverlappingShiftIndexes(day);
          const availableForNew = assignableTrainers.filter(
            (tr) => !day.trainers.some((x) => x.trainerId === tr.id),
          );

          return (
            <div
              key={day.date}
              className="grid gap-4 rounded-xl border border-gray-100 bg-[#FAFAF8] p-4 md:grid-cols-[13rem_minmax(0,1fr)]"
            >
              {/* Datum + kampuren */}
              <div>
                <div className="text-[13px] font-bold text-tennis-green first-letter:uppercase">
                  {formatDayHeading(day.date)}
                </div>
                <p className="mt-3 mb-1.5 text-[10.5px] uppercase tracking-[0.04em] text-gray-400">
                  {t("dayHours")}
                </p>
                <div className="flex items-center gap-1.5">
                  <input
                    type="time"
                    value={day.startTime}
                    onChange={(e) =>
                      updateDay(day.date, { startTime: e.target.value })
                    }
                    className={TIME_INPUT_CLS}
                  />
                  <span className="text-xs text-gray-400">{t("trainerEndTime")}</span>
                  <input
                    type="time"
                    value={day.endTime}
                    onChange={(e) =>
                      updateDay(day.date, { endTime: e.target.value })
                    }
                    className={TIME_INPUT_CLS}
                  />
                </div>
              </div>

              {/* Trainers en hun shiften */}
              <div className="min-w-0 md:border-l md:border-dashed md:border-gray-200 md:pl-4">
                <div className="mb-2 flex items-center justify-between gap-2">
                  <p className="text-[10.5px] uppercase tracking-[0.04em] text-gray-400">
                    {t("dayTrainers")}
                  </p>
                  <button
                    type="button"
                    onClick={() => openNewTrainerDialog(day)}
                    disabled={availableForNew.length === 0}
                    className="inline-flex items-center gap-1 rounded-md border border-gray-200 bg-white px-2 py-1 text-[11px] font-medium text-gray-600 transition-colors hover:bg-gray-50 hover:text-tennis-green disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    <Plus size={12} />
                    {t("addTrainer")}
                  </button>
                </div>

                {groups.length === 0 ? (
                  <p className="py-1 text-[11px] text-gray-400">
                    {t("dayNoTrainersPlanned")}
                  </p>
                ) : (
                  <div className="space-y-2">
                    {groups.map((group) => {
                      const totalHours = group.shifts.reduce(
                        (sum, s) => sum + shiftHours(s),
                        0,
                      );
                      const hasOverlap = group.shifts.some((s) =>
                        overlapping.has(s.index),
                      );
                      return (
                        <div
                          key={group.trainerId}
                          className="rounded-lg border border-gray-200 bg-white p-3"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <div className="min-w-0">
                              <span className="text-[13px] font-semibold text-gray-900">
                                {trainerName(group)}
                              </span>
                              <span className="ml-2 text-[11px] text-gray-400">
                                {t("shiftSummary", {
                                  count: group.shifts.length,
                                  hours: formatHours(totalHours),
                                })}
                              </span>
                            </div>
                            <div className="flex shrink-0 items-center gap-1">
                              <button
                                type="button"
                                onClick={() => openNewShiftDialog(day, group)}
                                className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-[11px] font-medium text-gray-500 transition-colors hover:bg-tennis-green/10 hover:text-tennis-green"
                              >
                                <Plus size={12} />
                                {t("addShift")}
                              </button>
                              <button
                                type="button"
                                onClick={() => removeTrainer(day.date, group.trainerId)}
                                aria-label={t("removeTrainer")}
                                title={t("removeTrainer")}
                                className="flex h-7 w-7 items-center justify-center rounded-lg text-gray-400 transition-colors hover:bg-red-50 hover:text-red-500"
                              >
                                <Trash2 size={13} />
                              </button>
                            </div>
                          </div>

                          <div className="mt-2 flex flex-wrap gap-2">
                            {group.shifts.map((shift) => {
                              const key = `${day.date}:${shift.index}`;
                              const isEditing = editingShift === key;
                              const isOverlapping = overlapping.has(shift.index);
                              return (
                                <div
                                  key={shift.index}
                                  className={
                                    "inline-flex items-center gap-1 rounded-lg border px-2 py-1 text-xs tabular-nums " +
                                    (isOverlapping
                                      ? "border-red-300 bg-red-50 text-red-700"
                                      : "border-tennis-green/20 bg-tennis-green/5 text-gray-700")
                                  }
                                >
                                  {isEditing ? (
                                    <>
                                      <input
                                        type="time"
                                        value={shift.startTime}
                                        min={day.startTime}
                                        max={day.endTime}
                                        onChange={(e) =>
                                          updateShiftTime(
                                            day.date,
                                            shift.index,
                                            "startTime",
                                            e.target.value,
                                          )
                                        }
                                        className={TIME_INPUT_CLS}
                                      />
                                      <span className="text-gray-400">-</span>
                                      <input
                                        type="time"
                                        value={shift.endTime}
                                        min={day.startTime}
                                        max={day.endTime}
                                        onChange={(e) =>
                                          updateShiftTime(
                                            day.date,
                                            shift.index,
                                            "endTime",
                                            e.target.value,
                                          )
                                        }
                                        className={TIME_INPUT_CLS}
                                      />
                                    </>
                                  ) : (
                                    <span className="px-1">
                                      {shift.startTime} – {shift.endTime}
                                    </span>
                                  )}
                                  <button
                                    type="button"
                                    onClick={() =>
                                      setEditingShift(isEditing ? null : key)
                                    }
                                    aria-label={
                                      isEditing
                                        ? t("trainerTimesDone")
                                        : t("editTrainerTimes")
                                    }
                                    className="flex h-6 w-6 items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-white hover:text-tennis-green"
                                  >
                                    {isEditing ? (
                                      <Check size={13} />
                                    ) : (
                                      <Pencil size={12} />
                                    )}
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() => removeShift(day.date, shift.index)}
                                    aria-label={t("removeShift")}
                                    title={t("removeShift")}
                                    className="flex h-6 w-6 items-center justify-center rounded-md text-gray-400 transition-colors hover:bg-white hover:text-red-500"
                                  >
                                    <X size={13} />
                                  </button>
                                </div>
                              );
                            })}
                          </div>

                          {hasOverlap && (
                            <p className="mt-2 text-[11px] font-medium text-red-600">
                              {t("shiftOverlap")}
                            </p>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}

                {assignableTrainers.length === 0 && (
                  <p className="mt-3 text-[11px] text-gray-400">
                    {t("noTrainers")}
                  </p>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {dialog &&
        (() => {
          const day = days.find((d) => d.date === dialog.date);
          const available = day
            ? assignableTrainers.filter(
                (tr) => !day.trainers.some((x) => x.trainerId === tr.id),
              )
            : [];
          const lockedTrainer = trainers.find((x) => x.id === dialog.trainerId);
          const lockedName = lockedTrainer
            ? `${lockedTrainer.firstName} ${lockedTrainer.lastName}`
            : (day?.trainers.find((x) => x.trainerId === dialog.trainerId)
                ?.trainerName ?? dialog.trainerId);

          // Eenvoudige controles zodat de gebruiker meteen ziet waarom een shift niet kan.
          let error: string | null = null;
          if (day) {
            if (dialog.end <= dialog.start) error = t("shiftEndBeforeStart");
            else if (dialog.start < day.startTime || dialog.end > day.endTime)
              error = t("shiftOutsideCampHours");
            else if (
              day.trainers.some(
                (x) =>
                  x.trainerId === dialog.trainerId &&
                  shiftsOverlap(x, { startTime: dialog.start, endTime: dialog.end }),
              )
            )
              error = t("shiftOverlapsOther");
          }

          return (
            <Dialog open onOpenChange={(o) => !o && setDialog(null)}>
              <DialogContent
                className="sm:max-w-md"
                onOpenAutoFocus={(e) => e.preventDefault()}
              >
                <DialogHeader>
                  <DialogTitle>
                    {dialog.trainerLocked
                      ? t("addShiftTitle", { name: lockedName })
                      : t("addTrainer")}
                  </DialogTitle>
                </DialogHeader>
                <div className="space-y-4">
                  {!dialog.trainerLocked && (
                    <div>
                      <label className="mb-1 block text-xs font-medium text-gray-600">
                        {t("addTrainerTrainerLabel")}
                      </label>
                      <NativeSelect
                        value={dialog.trainerId}
                        onChange={(e) =>
                          setDialog((d) =>
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
                  )}
                  <div className="grid grid-cols-2 gap-3">
                    <div className="min-w-0">
                      <label className="mb-1 block text-xs font-medium text-gray-600">
                        {t("dayStartTime")}
                      </label>
                      <input
                        type="time"
                        value={dialog.start}
                        onChange={(e) =>
                          setDialog((d) =>
                            d ? { ...d, start: e.target.value } : d,
                          )
                        }
                        className={TIME_INPUT_CLS + " w-full py-1.5"}
                      />
                    </div>
                    <div className="min-w-0">
                      <label className="mb-1 block text-xs font-medium text-gray-600">
                        {t("dayEndTime")}
                      </label>
                      <input
                        type="time"
                        value={dialog.end}
                        onChange={(e) =>
                          setDialog((d) =>
                            d ? { ...d, end: e.target.value } : d,
                          )
                        }
                        className={TIME_INPUT_CLS + " w-full py-1.5"}
                      />
                    </div>
                  </div>
                  {error && (
                    <p className="text-xs font-medium text-red-600">{error}</p>
                  )}
                </div>
                <DialogFooter>
                  <button
                    type="button"
                    onClick={() => setDialog(null)}
                    className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
                  >
                    {t("cancel")}
                  </button>
                  <button
                    type="button"
                    disabled={!dialog.trainerId || error !== null}
                    onClick={() => {
                      addShift(dialog.date, dialog.trainerId, dialog.start, dialog.end);
                      setDialog(null);
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
