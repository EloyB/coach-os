"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getTennisClubs } from "@/lib/api/tennisClubs";
import {
  createOwnTrainerAvailability,
  deleteOwnTrainerAvailability,
  getOwnTrainerAvailabilities,
  updateOwnTrainerAvailability,
} from "@/lib/api/trainerAvailabilities";
import { AvailabilityEditor } from "@/components/trainer-availability/availability-editor";
import type { AvailabilityFormValues } from "@/components/trainer-availability/availability-form-dialog";

const AVAILABILITIES_KEY = ["ownTrainerAvailabilities"];

/** "admin" is een beheerder die zelf lesgeeft: eigen titel en uitleg, want er is geen andere admin die het leest. */
export function AvailabilitySection({ audience = "trainer" }: { audience?: "trainer" | "admin" }) {
  const t = useTranslations("trainerSettings");
  const queryClient = useQueryClient();

  const { data: availabilities = [], isError } = useQuery({
    queryKey: AVAILABILITIES_KEY,
    queryFn: getOwnTrainerAvailabilities,
  });
  const { data: clubs = [] } = useQuery({
    queryKey: ["tennisClubs"],
    queryFn: getTennisClubs,
  });

  async function handleSave(values: AvailabilityFormValues, editingId?: string) {
    if (editingId) {
      await updateOwnTrainerAvailability(editingId, values);
    } else {
      await createOwnTrainerAvailability(values);
    }
    await queryClient.invalidateQueries({ queryKey: AVAILABILITIES_KEY });
  }

  async function handleDelete(id: string) {
    await deleteOwnTrainerAvailability(id);
    await queryClient.invalidateQueries({ queryKey: AVAILABILITIES_KEY });
  }

  return (
    <section className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
      <h2 className="text-lg font-semibold text-gray-900">
        {t(audience === "admin" ? "availabilityTitleAdmin" : "availabilityTitle")}
      </h2>
      <p className="text-sm text-gray-500 mt-0.5 mb-4">
        {t(audience === "admin" ? "availabilityIntroAdmin" : "availabilityIntro")}
      </p>

      <AvailabilityEditor
        availabilities={availabilities}
        clubs={clubs}
        isLoadError={isError}
        onSave={handleSave}
        onDelete={handleDelete}
      />
    </section>
  );
}
