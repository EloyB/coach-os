"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { updateCamp, type CampDetailDto } from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";
import {
  buildCampRequest,
  detailToDays,
  detailToPackages,
  toDateOnly,
  type CampPackageDraft,
} from "../new/_types";
import { CampPackagesEditor, CampPackagesHeading } from "./camp-packages-editor";

interface CampPackagesCardProps {
  camp: CampDetailDto;
  canEdit: boolean;
}

/** Kaart op de kamppagina: pakketten bekijken en (voor beheerders) direct bewaren. */
export function CampPackagesCard({ camp, canEdit }: CampPackagesCardProps) {
  const t = useTranslations("camps");
  const queryClient = useQueryClient();
  const days = detailToDays(camp);

  const mutation = useMutation({
    mutationFn: (packages: CampPackageDraft[]) =>
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
            prices: [],
          },
          days,
          packages,
        ),
        // Volledig tijdstip bewaren: buildCampRequest kapt de deadline af op middernacht.
        registrationDeadline: camp.registrationDeadline,
        // Prijsopties ongemoeid laten.
        prices: undefined,
        isActive: camp.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["camp", camp.id] });
      queryClient.invalidateQueries({ queryKey: ["camps"] });
    },
  });

  if (!canEdit && camp.packages.length === 0) return null;

  async function save(packages: CampPackageDraft[]): Promise<string[]> {
    try {
      await mutation.mutateAsync(packages);
      return [];
    } catch (error) {
      return getAxiosErrorMessages(error, t("saveError"));
    }
  }

  return (
    <div className="overflow-hidden rounded-xl bg-white shadow-sm shadow-gray-100">
      <div className="border-b border-gray-100 px-6 py-4">
        <CampPackagesHeading />
      </div>
      <div className="p-6">
        <CampPackagesEditor
          days={days}
          packages={detailToPackages(camp)}
          onChange={save}
          canEdit={canEdit}
        />
      </div>
    </div>
  );
}
