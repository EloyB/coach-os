"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import {
  createTrainerAvailability,
  deleteTrainerAvailability,
  getTrainerAvailabilities,
  updateTrainerAvailability,
} from "@/lib/api/trainerAvailabilities";
import type { TrainerDto } from "@/lib/api/trainers";
import { AvailabilityEditor } from "@/components/trainer-availability/availability-editor";
import type { AvailabilityFormValues } from "@/components/trainer-availability/availability-form-dialog";

const AVAILABILITIES_KEY = ["trainerAvailabilities"];

interface TrainerAvailabilityDialogProps {
  trainer: TrainerDto;
  onClose: () => void;
}

export function TrainerAvailabilityDialog({
  trainer,
  onClose,
}: TrainerAvailabilityDialogProps) {
  const t = useTranslations("trainers");
  const queryClient = useQueryClient();

  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const { data: availabilities = [], isError } = useQuery({
    queryKey: AVAILABILITIES_KEY,
    queryFn: getTrainerAvailabilities,
  });

  const trainerAvailabilities = availabilities.filter(
    (a) => a.trainerId === trainer.id
  );

  async function handleSave(values: AvailabilityFormValues, editingId?: string) {
    const request = { trainerId: trainer.id, ...values };
    if (editingId) {
      await updateTrainerAvailability(editingId, request);
    } else {
      await createTrainerAvailability(request);
    }
    await queryClient.invalidateQueries({ queryKey: AVAILABILITIES_KEY });
  }

  async function handleDelete(id: string) {
    await deleteTrainerAvailability(id);
    await queryClient.invalidateQueries({ queryKey: AVAILABILITIES_KEY });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent
        className="max-w-4xl max-h-[90vh] overflow-y-auto"
        aria-describedby={undefined}
        // Voorkom dat Radix bij openen de eerste focusbare control autofocust.
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>
            {t("availabilityTitle", {
              name: `${trainer.firstName} ${trainer.lastName}`,
            })}
          </DialogTitle>
        </DialogHeader>

        <AvailabilityEditor
          availabilities={trainerAvailabilities}
          clubs={clubs}
          isLoadError={isError}
          onSave={handleSave}
          onDelete={handleDelete}
        />
      </DialogContent>
    </Dialog>
  );
}
