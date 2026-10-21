"use client";

import { useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { CampDaysEditor } from "../new/_components/camp-days-editor";
import {
  buildCampRequest,
  detailToDays,
  toDateOnly,
  detailToPrices,
  type CampDayDraft,
} from "../new/_types";
import { getTrainers, isAssignableTrainer } from "@/lib/api/trainers";
import { updateCamp, type CampDetailDto } from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";

interface CampDaysCardProps {
  camp: CampDetailDto;
}

export function CampDaysCard({ camp }: CampDaysCardProps) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();

  const initialDays = useMemo(() => detailToDays(camp), [camp]);
  const [days, setDays] = useState<CampDayDraft[]>(initialDays);

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });
  const assignableTrainers = trainers.filter(isAssignableTrainer);

  const mutation = useMutation({
    mutationFn: (daysToSave: CampDayDraft[]) =>
      updateCamp(camp.id, {
        ...buildCampRequest(
          {
            name: camp.name,
            description: camp.description ?? "",
            tennisClubId: camp.tennisClubId,
            level: camp.level,
            price: camp.price,
            maxParticipants: camp.maxParticipants,
            startDate: toDateOnly(camp.startDate),
            endDate: toDateOnly(camp.endDate),
            registrationDeadline: toDateOnly(camp.registrationDeadline),
            prices: detailToPrices(camp),
          },
          daysToSave,
        ),
        prices: undefined,
        isActive: camp.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camp", camp.id] });
      queryClient.invalidateQueries({ queryKey: ["camps"] });
    },
  });

  /** Opslaan vanuit de dag-dialog: geeft foutmeldingen terug (leeg = gelukt). */
  async function saveDays(next: CampDayDraft[]): Promise<string[]> {
    try {
      await mutation.mutateAsync(next);
      setDays(next);
      return [];
    } catch (error) {
      return getAxiosErrorMessages(error, t("saveError"));
    }
  }

  return (
    <CampDaysEditor
      days={days}
      onChange={setDays}
      trainers={assignableTrainers}
      onSaveDays={saveDays}
      rangeStart={toDateOnly(camp.startDate)}
      rangeEnd={toDateOnly(camp.endDate)}
    />
  );
}
