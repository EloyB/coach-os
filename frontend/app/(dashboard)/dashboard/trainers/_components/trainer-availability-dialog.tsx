"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Trash2, Plus } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { NativeSelect } from "@/components/ui/native-select";
import { inputClass } from "@/lib/styles";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import {
  getTrainerAvailabilities,
  createTrainerAvailability,
  deleteTrainerAvailability,
  type TrainerAvailabilityDto,
} from "@/lib/api/trainerAvailabilities";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import type { TrainerDto } from "@/lib/api/trainers";

const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

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

  const [clubId, setClubId] = useState("");
  const [dayOfWeek, setDayOfWeek] = useState(0);
  const [startTime, setStartTime] = useState("17:00");
  const [endTime, setEndTime] = useState("21:00");
  const [errorMessages, setErrorMessages] = useState<string[]>([]);

  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  const { data: availabilities = [] } = useQuery({
    queryKey: ["trainerAvailabilities"],
    queryFn: getTrainerAvailabilities,
  });

  const trainerAvailabilities = availabilities.filter(
    (a) => a.trainerId === trainer.id
  );

  const createMutation = useMutation({
    mutationFn: createTrainerAvailability,
    onSuccess: () => {
      setErrorMessages([]);
      queryClient.invalidateQueries({ queryKey: ["trainerAvailabilities"] });
    },
    onError: (error) =>
      setErrorMessages(getAxiosErrorMessages(error, t("availabilityError"))),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteTrainerAvailability,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["trainerAvailabilities"] }),
    onError: (error) =>
      setErrorMessages(getAxiosErrorMessages(error, t("availabilityError"))),
  });

  function handleAdd() {
    createMutation.mutate({
      trainerId: trainer.id,
      tennisClubId: clubId || null,
      dayOfWeek,
      startTime,
      endTime,
    });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-lg" aria-describedby={undefined}>
        <DialogHeader>
          <DialogTitle>
            {t("availabilityTitle", {
              name: `${trainer.firstName} ${trainer.lastName}`,
            })}
          </DialogTitle>
        </DialogHeader>

        {/* Bestaande beschikbaarheden */}
        <div className="space-y-2">
          {trainerAvailabilities.length === 0 && (
            <p className="text-sm text-gray-500">{t("availabilityEmpty")}</p>
          )}
          {trainerAvailabilities.map((a: TrainerAvailabilityDto) => (
            <div
              key={a.id}
              className="flex items-center justify-between rounded-lg border border-gray-200 px-3 py-2 text-sm"
            >
              <span>
                {a.tennisClubName ?? t("availabilityAnyClub")},{" "}
                {DAY_NAMES_FULL[a.dayOfWeek]} {a.startTime} tot {a.endTime}
              </span>
              <button
                type="button"
                onClick={() => deleteMutation.mutate(a.id)}
                disabled={deleteMutation.isPending}
                className="text-gray-400 hover:text-red-600 transition-colors"
                aria-label={t("availabilityDelete")}
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          ))}
        </div>

        {/* Nieuwe beschikbaarheid */}
        <div className="border-t border-gray-100 pt-4 space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityClub")}
              </label>
              <NativeSelect
                value={clubId}
                onChange={(e) => setClubId(e.target.value)}
              >
                <option value="">{t("availabilityAnyClub")}</option>
                {clubs.map((club) => (
                  <option key={club.id} value={club.id}>
                    {club.name}
                  </option>
                ))}
              </NativeSelect>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityDay")}
              </label>
              <NativeSelect
                value={dayOfWeek}
                onChange={(e) => setDayOfWeek(Number(e.target.value))}
              >
                {DAY_NAMES_FULL.map((name, index) => (
                  <option key={index} value={index}>
                    {name}
                  </option>
                ))}
              </NativeSelect>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityFrom")}
              </label>
              <input
                type="time"
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">
                {t("availabilityUntil")}
              </label>
              <input
                type="time"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                className={inputClass}
              />
            </div>
          </div>

          {errorMessages.map((message) => (
            <p key={message} className="text-sm text-red-600">
              {message}
            </p>
          ))}

          <button
            type="button"
            onClick={handleAdd}
            disabled={createMutation.isPending}
            className="w-full flex items-center justify-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-tennis-green rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-50"
          >
            <Plus className="h-4 w-4" />
            {createMutation.isPending
              ? t("availabilitySaving")
              : t("availabilityAdd")}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
