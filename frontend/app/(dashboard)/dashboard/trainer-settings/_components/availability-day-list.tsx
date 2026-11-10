"use client";

import { Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import type { TennisClubDto } from "@/lib/api/tennisClubs";
import type { TrainerAvailabilityDto } from "@/lib/api/trainerAvailabilities";
import { clubColorIndex, groupByDay } from "@/lib/trainer-availability-layout";
import { dotColorClass } from "./availability-colors";
import type { AvailabilityFormValues } from "./availability-form-dialog";

interface AvailabilityDayListProps {
  availabilities: TrainerAvailabilityDto[];
  clubs: TennisClubDto[];
  onAdd: (initial: AvailabilityFormValues) => void;
  onEdit: (availability: TrainerAvailabilityDto) => void;
}

/** Lijst per weekdag, bedoeld voor mobiel (verborgen vanaf `md`). */
export function AvailabilityDayList({
  availabilities,
  clubs,
  onAdd,
  onEdit,
}: AvailabilityDayListProps) {
  const t = useTranslations("trainerSettings");
  const grouped = groupByDay(availabilities);
  const clubIds = clubs.map((club) => club.id);

  return (
    <div data-testid="availability-day-list" className="space-y-2 md:hidden">
      {[0, 1, 2, 3, 4, 5, 6].map((day) => (
        <div key={day} className="rounded-lg border border-gray-200 p-3">
          <div className="font-medium text-gray-900 mb-2">{t(`days.${day}`)}</div>
          <div className="space-y-1.5">
            {grouped[day].length === 0 && (
              <p className="text-sm text-gray-400">{t("notAvailable")}</p>
            )}
            {grouped[day].map((availability) => (
              <button
                key={availability.id}
                type="button"
                onClick={() => onEdit(availability)}
                className="w-full flex items-center gap-2 rounded-md bg-gray-50 px-3 py-2 text-left text-sm hover:bg-gray-100"
              >
                <span
                  className={`h-2.5 w-2.5 shrink-0 rounded-full ${dotColorClass(clubColorIndex(availability.tennisClubId, clubIds))}`}
                />
                <span className="font-medium">
                  {availability.startTime} - {availability.endTime}
                </span>
                <span className="text-gray-500 truncate">
                  {availability.tennisClubName ?? t("anyClub")}
                </span>
              </button>
            ))}
          </div>
          <button
            type="button"
            onClick={() =>
              onAdd({
                tennisClubId: null,
                dayOfWeek: day,
                startTime: "17:00",
                endTime: "21:00",
              })
            }
            className="mt-2 inline-flex items-center gap-1.5 text-sm font-medium text-tennis-green"
          >
            <Plus className="h-4 w-4" />
            {t("addSlot")}
          </button>
        </div>
      ))}
    </div>
  );
}
