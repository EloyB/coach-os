"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { NativeSelect } from "@/components/ui/native-select";
import { btnPrimary, btnSecondary, inputClass } from "@/lib/styles";
import type { TennisClubDto } from "@/lib/api/tennisClubs";
import type { TrainerAvailabilityDto } from "@/lib/api/trainerAvailabilities";
import { overlaps, toMinutes } from "@/lib/trainer-availability-layout";

export interface AvailabilityFormValues {
  tennisClubId: string | null;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

interface AvailabilityFormDialogProps {
  mode: "create" | "edit";
  initial: AvailabilityFormValues;
  /** Id van het tijdvak dat bewerkt wordt (uitgesloten van de overlapcontrole). */
  editingId?: string;
  /** Alle eigen tijdvakken, voor de overlapcontrole aan de clientzijde. */
  existing: TrainerAvailabilityDto[];
  clubs: TennisClubDto[];
  isSaving: boolean;
  apiErrors: string[];
  onSave: (values: AvailabilityFormValues) => void;
  onDelete?: () => void;
  onClose: () => void;
}

export function AvailabilityFormDialog({
  mode,
  initial,
  editingId,
  existing,
  clubs,
  isSaving,
  apiErrors,
  onSave,
  onDelete,
  onClose,
}: AvailabilityFormDialogProps) {
  const t = useTranslations("trainerSettings");
  const [clubId, setClubId] = useState(initial.tennisClubId ?? "");
  const [dayOfWeek, setDayOfWeek] = useState(initial.dayOfWeek);
  const [startTime, setStartTime] = useState(initial.startTime);
  const [endTime, setEndTime] = useState(initial.endTime);
  const [validationError, setValidationError] = useState<string | null>(null);

  function handleSave() {
    if (!startTime || !endTime || toMinutes(endTime) <= toMinutes(startTime)) {
      setValidationError(t("endBeforeStart"));
      return;
    }
    const clashes = existing.some(
      (other) =>
        other.id !== editingId
        && other.dayOfWeek === dayOfWeek
        && overlaps(other, { startTime, endTime }),
    );
    if (clashes) {
      setValidationError(t("overlapsExisting"));
      return;
    }
    setValidationError(null);
    onSave({ tennisClubId: clubId || null, dayOfWeek, startTime, endTime });
  }

  const errors = [...(validationError ? [validationError] : []), ...apiErrors];

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent
        className="max-w-md"
        aria-describedby={undefined}
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>
            {mode === "create" ? t("addSlotTitle") : t("editSlotTitle")}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("fieldDay")}
              </label>
              <NativeSelect
                aria-label={t("fieldDay")}
                value={dayOfWeek}
                onChange={(e) => setDayOfWeek(Number(e.target.value))}
              >
                {[0, 1, 2, 3, 4, 5, 6].map((day) => (
                  <option key={day} value={day}>
                    {t(`days.${day}`)}
                  </option>
                ))}
              </NativeSelect>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("fieldClub")}
              </label>
              <NativeSelect
                aria-label={t("fieldClub")}
                value={clubId}
                onChange={(e) => setClubId(e.target.value)}
              >
                <option value="">{t("anyClub")}</option>
                {clubs.map((club) => (
                  <option key={club.id} value={club.id}>
                    {club.name}
                  </option>
                ))}
              </NativeSelect>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="min-w-0">
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("fieldFrom")}
              </label>
              <input
                type="time"
                step={900}
                aria-label={t("fieldFrom")}
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                className={`${inputClass} appearance-none min-w-0`}
              />
            </div>
            <div className="min-w-0">
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("fieldUntil")}
              </label>
              <input
                type="time"
                step={900}
                aria-label={t("fieldUntil")}
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                className={`${inputClass} appearance-none min-w-0`}
              />
            </div>
          </div>

          {errors.map((message) => (
            <p key={message} role="alert" className="text-sm text-red-600">
              {message}
            </p>
          ))}

          <div className="flex gap-2 pt-1">
            {mode === "edit" && onDelete && (
              <button
                type="button"
                onClick={onDelete}
                disabled={isSaving}
                className={`${btnSecondary} text-red-600 hover:bg-red-50`}
              >
                {t("delete")}
              </button>
            )}
            <div className="flex-1" />
            <button type="button" onClick={onClose} className={btnSecondary}>
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={handleSave}
              disabled={isSaving}
              className={btnPrimary}
            >
              {isSaving ? t("saving") : t("save")}
            </button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
