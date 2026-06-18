"use client";

import { useTranslations } from "next-intl";
import { Clock } from "lucide-react";
import { formatDayHeading } from "../new/_types";
import type { CampDetailDto } from "@/lib/api/camps";

interface CampDaysReadOnlyCardProps {
  camp: CampDetailDto;
}

/**
 * Read-only rendering of a camp's days and assigned trainers. Used for
 * non-admin (trainer) viewers, who may not edit and cannot resolve trainer
 * names via the admin-only getTrainers endpoint. Names come straight from the
 * server-resolved camp detail (camp.days[].trainers[].trainerName).
 */
export function CampDaysReadOnlyCard({ camp }: CampDaysReadOnlyCardProps) {
  const t = useTranslations("camps");

  return (
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
        <div className="w-6 h-6 rounded-md bg-tennis-green/10 flex items-center justify-center">
          <Clock size={13} className="text-tennis-green" />
        </div>
        <div>
          <h2 className="text-sm font-semibold text-gray-900">
            {t("daysTitle")}
          </h2>
          <p className="text-xs text-gray-400 mt-0.5">{t("daysDescription")}</p>
        </div>
      </div>

      <div className="p-6 space-y-3">
        {camp.days.length === 0 && (
          <p className="text-xs text-gray-400 py-2">{t("noDays")}</p>
        )}

        {camp.days.map((day) => (
          <div
            key={day.date}
            className="border border-gray-100 rounded-xl p-4 bg-[#FAFAF8]"
          >
            <div className="flex items-center justify-between gap-3 mb-3">
              <div className="text-[13px] font-bold text-tennis-green">
                {formatDayHeading(day.date)}
              </div>
              <span className="inline-flex items-center gap-1 text-xs text-gray-500">
                <Clock size={11} className="text-gray-400" />
                {day.startTime} - {day.endTime}
              </span>
            </div>

            {day.trainers.length === 0 ? (
              <p className="text-[11px] text-gray-400">
                {t("noTrainersOnDay")}
              </p>
            ) : (
              <div className="space-y-1.5">
                {day.trainers.map((tr) => (
                  <div
                    key={tr.trainerId}
                    className="flex items-center justify-between gap-3"
                  >
                    <span className="text-[13px] font-semibold text-gray-900">
                      {tr.trainerName}
                    </span>
                    <span className="text-xs text-gray-500 tabular-nums">
                      {tr.startTime}-{tr.endTime}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
