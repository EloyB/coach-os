"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import {
  createOwnTrainerAvailability,
  deleteOwnTrainerAvailability,
  getOwnTrainerAvailabilities,
  updateOwnTrainerAvailability,
  type TrainerAvailabilityDto,
} from "@/lib/api/trainerAvailabilities";
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

const AVAILABILITIES_KEY = ["ownTrainerAvailabilities"];

export function AvailabilitySection() {
  const t = useTranslations("trainerSettings");
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<DialogState | null>(null);
  const [apiErrors, setApiErrors] = useState<string[]>([]);

  const { data: availabilities = [], isError } = useQuery({
    queryKey: AVAILABILITIES_KEY,
    queryFn: getOwnTrainerAvailabilities,
  });
  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const clubIds = clubs.map((club) => club.id);

  function closeDialog() {
    setDialog(null);
    setApiErrors([]);
  }

  const saveMutation = useMutation({
    mutationFn: async (values: AvailabilityFormValues): Promise<void> => {
      if (dialog?.editingId) {
        await updateOwnTrainerAvailability(dialog.editingId, values);
      } else {
        await createOwnTrainerAvailability(values);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: AVAILABILITIES_KEY });
      closeDialog();
    },
    onError: (error) => setApiErrors(getAxiosErrorMessages(error, t("saveError"))),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteOwnTrainerAvailability(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: AVAILABILITIES_KEY });
      closeDialog();
    },
    onError: (error) => setApiErrors(getAxiosErrorMessages(error, t("saveError"))),
  });

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
    <section className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
      <h2 className="text-lg font-semibold text-gray-900">
        {t("availabilityTitle")}
      </h2>
      <p className="text-sm text-gray-500 mt-0.5 mb-4">
        {t("availabilityIntro")}
      </p>

      {isError && (
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
          isSaving={saveMutation.isPending || deleteMutation.isPending}
          apiErrors={apiErrors}
          onSave={(values) => saveMutation.mutate(values)}
          onDelete={
            dialog.editingId
              ? () => deleteMutation.mutate(dialog.editingId!)
              : undefined
          }
          onClose={closeDialog}
        />
      )}
    </section>
  );
}
