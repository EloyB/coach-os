"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import type { TennisClubDto } from "@/lib/api/tennisClubs";
import type { TrainerAvailabilityDto } from "@/lib/api/trainerAvailabilities";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import { clubColorIndex } from "@/lib/trainer-availability-layout";
import { dotColorClass } from "./availability-colors";
import {
  AvailabilityFormDialog,
  type AvailabilityFormValues,
} from "./availability-form-dialog";
import { AvailabilityWeekGrid } from "./availability-week-grid";
import { AvailabilityDayList } from "./availability-day-list";

interface DialogState {
  mode: "create" | "edit";
  initial: AvailabilityFormValues;
  editingId?: string;
}

interface AvailabilityEditorProps {
  /** Tijdvakken van precies één trainer. */
  availabilities: TrainerAvailabilityDto[];
  clubs: TennisClubDto[];
  isLoadError?: boolean;
  /** Maakt aan (zonder `editingId`) of wijzigt (met `editingId`); een fout wordt in het formulier getoond. */
  onSave: (values: AvailabilityFormValues, editingId?: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
}

/**
 * Weekoverzicht (desktop) of dagenlijst (mobiel) met het formulier om tijdvakken
 * toe te voegen, te wijzigen en te verwijderen. Gedeeld door de trainer-instellingen
 * en de beschikbaarheidsdialoog van de admin.
 */
export function AvailabilityEditor({
  availabilities,
  clubs,
  isLoadError = false,
  onSave,
  onDelete,
}: AvailabilityEditorProps) {
  const t = useTranslations("trainerSettings");
  const [dialog, setDialog] = useState<DialogState | null>(null);
  const [apiErrors, setApiErrors] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);

  const clubIds = clubs.map((club) => club.id);

  function closeDialog() {
    setDialog(null);
    setApiErrors([]);
  }

  async function run(action: () => Promise<void>) {
    setIsSaving(true);
    try {
      await action();
      closeDialog();
    } catch (error) {
      setApiErrors(getAxiosErrorMessages(error, t("saveError")));
    } finally {
      setIsSaving(false);
    }
  }

  function handleAdd(initial: AvailabilityFormValues) {
    setApiErrors([]);
    setDialog({ mode: "create", initial });
  }

  function handleEdit(availability: TrainerAvailabilityDto) {
    setApiErrors([]);
    setDialog({
      mode: "edit",
      editingId: availability.id,
      initial: {
        tennisClubId: availability.tennisClubId,
        dayOfWeek: availability.dayOfWeek,
        startTime: availability.startTime,
        endTime: availability.endTime,
      },
    });
  }

  return (
    <>
      {isLoadError && (
        <p role="alert" className="text-sm text-red-600 mb-3">
          {t("loadError")}
        </p>
      )}

      <AvailabilityWeekGrid
        availabilities={availabilities}
        clubIds={clubIds}
        onAdd={handleAdd}
        onEdit={handleEdit}
      />
      <AvailabilityDayList
        availabilities={availabilities}
        clubs={clubs}
        onAdd={handleAdd}
        onEdit={handleEdit}
      />

      <div className="mt-4 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-500">
        <span className="font-medium">{t("legendTitle")}:</span>
        <span className="inline-flex items-center gap-1.5">
          <span className={`h-2.5 w-2.5 rounded-full ${dotColorClass(0)}`} />
          {t("anyClub")}
        </span>
        {clubs.map((club) => (
          <span key={club.id} className="inline-flex items-center gap-1.5">
            <span
              className={`h-2.5 w-2.5 rounded-full ${dotColorClass(clubColorIndex(club.id, clubIds))}`}
            />
            {club.name}
          </span>
        ))}
      </div>

      {dialog && (
        <AvailabilityFormDialog
          mode={dialog.mode}
          initial={dialog.initial}
          editingId={dialog.editingId}
          existing={availabilities}
          clubs={clubs}
          isSaving={isSaving}
          apiErrors={apiErrors}
          onSave={(values) => run(() => onSave(values, dialog.editingId))}
          onDelete={
            dialog.editingId
              ? () => run(() => onDelete(dialog.editingId!))
              : undefined
          }
          onClose={closeDialog}
        />
      )}
    </>
  );
}
