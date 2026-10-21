"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Check, Pencil, Plus, Trash2, X } from "lucide-react";
import { NativeSelect } from "@/components/ui/native-select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
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

const TIME_INPUT_CLS =
  "border border-gray-200 rounded-lg px-2 py-1 text-sm bg-white appearance-none min-w-0 focus:outline-none focus:ring-2 focus:ring-tennis-green/30 focus:border-tennis-green";

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
    shifts: [...g.shifts].sort((a, b) =>
      a.startTime.localeCompare(b.startTime),
    ),
  }));
}

function formatHours(hours: number): string {
  return hours.toLocaleString("nl-BE", { maximumFractionDigits: 1 });
}

interface CampDayDialogProps {
  day: CampDayDraft;
  trainers: TrainerDto[];
  saveLabel: string;
  /** Dag die nog niet bestaat en met deze dialog wordt toegevoegd. */
  isNew: boolean;
  /** Mag de dag geschrapt worden (niet de laatste kampdag)? */
  canRemove: boolean;
  /** Schrapt de dag uit het kamp; geeft foutmeldingen terug (leeg = gelukt). */
  onRemove: () => Promise<string[]>;
  /** Bewaart de bewerkte dag; geeft foutmeldingen terug (leeg = gelukt). */
  onSave: (day: CampDayDraft) => Promise<string[]>;
  onClose: () => void;
}

/** Dialog om de kampuren en de trainers (met hun shiften) van één kampdag in te stellen. */
export function CampDayDialog({
  day: initialDay,
  trainers,
  saveLabel,
  isNew,
  canRemove,
  onRemove,
  onSave,
  onClose,
}: CampDayDialogProps) {
  const t = useTranslations("camps");
  const assignable = trainers.filter(isAssignableTrainer);
  // Werkkopie: wijzigingen gaan pas naar het kamp met de opslaan-knop in de footer.
  const [day, setDay] = useState(initialDay);
  const onChange = setDay;
  const [saving, setSaving] = useState(false);
  const [saveErrors, setSaveErrors] = useState<string[]>([]);
  const [confirmRemove, setConfirmRemove] = useState(false);
  const [editingShift, setEditingShift] = useState<number | null>(null);
  const [form, setForm] = useState({
    trainerId: "",
    start: day.startTime,
    end: day.endTime,
  });

  const groups = groupByTrainer(day);
  const overlapping = findOverlappingShiftIndexes(day);

  function trainerName(group: {
    trainerId: string;
    trainerName?: string;
  }): string {
    const tr = trainers.find((x) => x.id === group.trainerId);
    if (tr) return `${tr.firstName} ${tr.lastName}`;
    return group.trainerName ?? group.trainerId;
  }

  // Kies een trainer en stel meteen een tijd voor: na zijn laatste shift, zolang daar ruimte voor is.
  function selectTrainer(trainerId: string) {
    const existing = day.trainers.filter((x) => x.trainerId === trainerId);
    const lastEnd = existing.reduce(
      (max, s) => (s.endTime > max ? s.endTime : max),
      day.startTime,
    );
    const hasRoom = existing.length > 0 && lastEnd < day.endTime;
    setForm({
      trainerId,
      start: hasRoom ? lastEnd : day.startTime,
      end: day.endTime,
    });
  }

  let formError: string | null = null;
  if (form.end <= form.start) formError = t("shiftEndBeforeStart");
  else if (form.start < day.startTime || form.end > day.endTime)
    formError = t("shiftOutsideCampHours");
  else if (
    day.trainers.some(
      (x) =>
        x.trainerId === form.trainerId &&
        shiftsOverlap(x, { startTime: form.start, endTime: form.end }),
    )
  )
    formError = t("shiftOverlapsOther");

  function addShift() {
    if (!form.trainerId || formError) return;
    onChange({
      ...day,
      trainers: [
        ...day.trainers,
        { trainerId: form.trainerId, startTime: form.start, endTime: form.end },
      ],
    });
    setForm({ trainerId: "", start: day.startTime, end: day.endTime });
  }

  function removeShift(index: number) {
    setEditingShift(null);
    onChange({ ...day, trainers: day.trainers.filter((_, i) => i !== index) });
  }

  function removeTrainer(trainerId: string) {
    setEditingShift(null);
    onChange({
      ...day,
      trainers: day.trainers.filter((tr) => tr.trainerId !== trainerId),
    });
  }

  function updateShiftTime(
    index: number,
    field: "startTime" | "endTime",
    value: string,
  ) {
    onChange({
      ...day,
      trainers: day.trainers.map((tr, i) =>
        i === index
          ? { ...tr, [field]: clampTime(value, day.startTime, day.endTime) }
          : tr,
      ),
    });
  }

  return (
    <Dialog open onOpenChange={(o) => !o && onClose()}>
      <DialogContent
        className="flex max-h-[85vh] flex-col gap-0 overflow-hidden p-0 sm:max-w-xl"
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <div className="flex flex-col gap-5 overflow-y-auto px-6 pb-6 pt-6">
          <DialogHeader>
            <DialogTitle className="first-letter:uppercase">
              {formatDayHeading(day.date)}
            </DialogTitle>
            <DialogDescription>{t("dayDialogDescription")}</DialogDescription>
          </DialogHeader>

          {/* Kampuren */}
          <div>
            <p className="mb-1.5 text-[10.5px] uppercase tracking-[0.04em] text-gray-400">
              {t("dayHours")}
            </p>
            <div className="flex items-center gap-1.5">
              <input
                type="time"
                value={day.startTime}
                onChange={(e) =>
                  onChange({ ...day, startTime: e.target.value })
                }
                className={TIME_INPUT_CLS}
              />
              <span className="text-xs text-gray-400">
                {t("trainerEndTime")}
              </span>
              <input
                type="time"
                value={day.endTime}
                onChange={(e) => onChange({ ...day, endTime: e.target.value })}
                className={TIME_INPUT_CLS}
              />
            </div>
          </div>

          {/* Trainers en hun shiften */}
          <div>
            <p className="mb-2 text-[10.5px] uppercase tracking-[0.04em] text-gray-400">
              {t("dayTrainers")}
            </p>
            {groups.length === 0 ? (
              <p className="text-[11px] text-gray-400">
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
                            onClick={() => selectTrainer(group.trainerId)}
                            className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-[11px] font-medium text-gray-500 transition-colors hover:bg-tennis-green/10 hover:text-tennis-green"
                          >
                            <Plus size={12} />
                            {t("addShift")}
                          </button>
                          <button
                            type="button"
                            onClick={() => removeTrainer(group.trainerId)}
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
                          const isEditing = editingShift === shift.index;
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
                                  setEditingShift(
                                    isEditing ? null : shift.index,
                                  )
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
                                onClick={() => removeShift(shift.index)}
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
          </div>

          {/* Trainer of shift toevoegen */}
          <div className="rounded-xl border border-dashed border-gray-200 bg-[#FAFAF8] p-3">
            <p className="mb-2 text-xs font-medium text-gray-600">
              {t("addTrainer")}
            </p>
            {assignable.length === 0 ? (
              <p className="text-[11px] text-gray-400">{t("noTrainers")}</p>
            ) : (
              <div className="space-y-3">
                <NativeSelect
                  value={form.trainerId}
                  onChange={(e) => selectTrainer(e.target.value)}
                >
                  <option value="">{t("selectTrainer")}</option>
                  {assignable.map((tr) => (
                    <option key={tr.id} value={tr.id}>
                      {tr.firstName} {tr.lastName}
                    </option>
                  ))}
                </NativeSelect>
                <div className="grid grid-cols-2 gap-3">
                  <div className="min-w-0">
                    <label className="mb-1 block text-xs font-medium text-gray-600">
                      {t("dayStartTime")}
                    </label>
                    <input
                      type="time"
                      value={form.start}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, start: e.target.value }))
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
                      value={form.end}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, end: e.target.value }))
                      }
                      className={TIME_INPUT_CLS + " w-full py-1.5"}
                    />
                  </div>
                </div>
                {form.trainerId && formError && (
                  <p className="text-xs font-medium text-red-600">
                    {formError}
                  </p>
                )}
                <button
                  type="button"
                  disabled={!form.trainerId || formError !== null}
                  onClick={addShift}
                  className="rounded-lg bg-tennis-green px-4 py-2 text-sm font-semibold text-white hover:bg-tennis-green/90 disabled:opacity-50"
                >
                  {t("addTrainerSubmit")}
                </button>
              </div>
            )}
          </div>

          {overlapping.size > 0 && (
            <p className="text-xs font-medium text-red-600">
              {t("shiftsOverlapBlock")}
            </p>
          )}
          {saveErrors.length > 0 && (
            <div className="space-y-0.5 text-xs text-red-600">
              {saveErrors.map((msg, i) => (
                <p key={i}>{msg}</p>
              ))}
            </div>
          )}
        </div>

        <div className="flex items-center justify-between gap-3 border-t border-gray-100 px-6 py-3">
          {canRemove && !isNew ? (
            confirmRemove ? (
              <div className="flex items-center gap-2 text-xs">
                <button
                  type="button"
                  disabled={saving}
                  onClick={async () => {
                    setSaving(true);
                    setSaveErrors([]);
                    const errors = await onRemove();
                    setSaving(false);
                    setSaveErrors(errors);
                    setConfirmRemove(false);
                  }}
                  className="font-semibold text-red-600 hover:underline disabled:opacity-60"
                >
                  {t("removeDayConfirm")}
                </button>
                <button
                  type="button"
                  onClick={() => setConfirmRemove(false)}
                  className="text-gray-500 hover:underline"
                >
                  {t("cancel")}
                </button>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => setConfirmRemove(true)}
                className="text-sm font-medium text-red-600 hover:underline"
              >
                {t("removeDay")}
              </button>
            )
          ) : (
            <span />
          )}
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={onClose}
              disabled={saving}
              className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50"
            >
              {t("cancel")}
            </button>
            <button
              type="button"
              disabled={saving || overlapping.size > 0}
              onClick={async () => {
                setSaving(true);
                setSaveErrors([]);
                const errors = await onSave(day);
                setSaving(false);
                setSaveErrors(errors);
              }}
              className="rounded-lg bg-tennis-green px-5 py-2 text-sm font-semibold text-white hover:bg-tennis-green/90 disabled:opacity-60"
            >
              {saving ? t("saving") : saveLabel}
            </button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
