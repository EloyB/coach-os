"use client";

import { useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ChevronLeft, ChevronRight, Clock } from "lucide-react";
import type { TrainerDto } from "@/lib/api/trainers";
import {
  findOverlappingShiftIndexes,
  shiftHours,
  type CampDayDraft,
} from "../_types";
import { CampDayDialog } from "./camp-day-dialog";

const WEEKDAYS = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];

interface CampDaysEditorProps {
  days: CampDayDraft[];
  onChange: (days: CampDayDraft[]) => void;
  trainers: TrainerDto[];
  /** Optionele voettekst onderaan de kaart (bv. een opslaan-balk die in beeld blijft plakken). */
  footer?: React.ReactNode;
}

function toIso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

/** Maandag van de week waarin `iso` valt. */
function mondayOf(iso: string): Date {
  const d = new Date(iso + "T00:00:00");
  d.setDate(d.getDate() - ((d.getDay() + 6) % 7));
  return d;
}

function formatShort(d: Date): string {
  return new Intl.DateTimeFormat("nl-BE", {
    day: "numeric",
    month: "short",
  }).format(d);
}

export function CampDaysEditor({
  days,
  onChange,
  trainers,
  footer,
}: CampDaysEditorProps) {
  const t = useTranslations("camps");
  const [weekIndex, setWeekIndex] = useState(0);
  const [openDate, setOpenDate] = useState<string | null>(null);

  // Alle weken (maandag) die de kampdagen bestrijken.
  const weeks = useMemo(() => {
    if (days.length === 0) return [] as Date[];
    const sorted = [...days].map((d) => d.date).sort();
    const result: Date[] = [];
    const cur = mondayOf(sorted[0]);
    const last = mondayOf(sorted[sorted.length - 1]);
    while (cur <= last) {
      result.push(new Date(cur));
      cur.setDate(cur.getDate() + 7);
    }
    return result;
  }, [days]);

  const safeIndex = Math.min(weekIndex, Math.max(weeks.length - 1, 0));
  const monday = weeks[safeIndex];
  const dayByDate = new Map(days.map((d) => [d.date, d]));
  const openDay = openDate ? dayByDate.get(openDate) : undefined;

  function trainerLabel(day: CampDayDraft, trainerId: string): string {
    const tr = trainers.find((x) => x.id === trainerId);
    if (tr) return `${tr.firstName} ${tr.lastName.charAt(0)}.`;
    const name = day.trainers.find(
      (x) => x.trainerId === trainerId,
    )?.trainerName;
    return name ?? trainerId;
  }

  return (
    // overflow-clip (niet -hidden): knipt de hoeken af zonder een scrollcontainer te maken, zodat de
    // plakkende voettekst aan de pagina blijft kleven.
    <div className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-clip">
      <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
        <div className="w-6 h-6 shrink-0 rounded-md bg-tennis-green/10 flex items-center justify-center">
          <Clock size={13} className="text-tennis-green" />
        </div>
        <div className="min-w-0 flex-1">
          <h2 className="text-sm font-semibold text-gray-900">
            {t("daysTitle")}
          </h2>
          <p className="text-xs text-gray-400 mt-0.5">{t("daysDescription")}</p>
        </div>
        {weeks.length > 1 && monday && (
          <div className="flex shrink-0 items-center gap-1">
            <button
              type="button"
              onClick={() => setWeekIndex(Math.max(safeIndex - 1, 0))}
              disabled={safeIndex === 0}
              aria-label={t("weekPrevious")}
              className="flex h-7 w-7 items-center justify-center rounded-lg text-gray-500 hover:bg-gray-50 disabled:opacity-30"
            >
              <ChevronLeft size={15} />
            </button>
            <span className="min-w-[8.5rem] text-center text-xs font-medium text-gray-600">
              {t("weekRange", {
                from: formatShort(monday),
                to: formatShort(new Date(monday.getTime() + 6 * 86400000)),
              })}
            </span>
            <button
              type="button"
              onClick={() =>
                setWeekIndex(Math.min(safeIndex + 1, weeks.length - 1))
              }
              disabled={safeIndex >= weeks.length - 1}
              aria-label={t("weekNext")}
              className="flex h-7 w-7 items-center justify-center rounded-lg text-gray-500 hover:bg-gray-50 disabled:opacity-30"
            >
              <ChevronRight size={15} />
            </button>
          </div>
        )}
      </div>

      <div className="p-6">
        {days.length === 0 || !monday ? (
          <p className="text-xs text-gray-400 py-2">{t("noDays")}</p>
        ) : (
          <div className="overflow-x-auto">
            <div className="grid min-w-[760px] grid-cols-7 gap-2">
              {WEEKDAYS.map((label, i) => {
                const date = new Date(monday);
                date.setDate(date.getDate() + i);
                const iso = toIso(date);
                const day = dayByDate.get(iso);

                if (!day) {
                  return (
                    <div key={iso} className="flex flex-col gap-2 opacity-40">
                      <div className="rounded-lg bg-gray-50 py-2 text-center">
                        <span className="text-xs font-semibold text-gray-600">
                          {label}
                        </span>
                        <span className="ml-1.5 text-xs text-gray-400">
                          {date.getDate()}
                        </span>
                      </div>
                      <div className="min-h-[7rem] rounded-lg border border-dashed border-gray-100" />
                    </div>
                  );
                }

                const overlapping = findOverlappingShiftIndexes(day).size > 0;
                const trainerIds = [
                  ...new Set(day.trainers.map((x) => x.trainerId)),
                ];
                return (
                  <div key={iso} className="flex flex-col gap-2">
                    <div className="rounded-lg bg-gray-50 py-2 text-center">
                      <span className="text-xs font-semibold text-gray-600">
                        {label}
                      </span>
                      <span className="ml-1.5 text-xs text-gray-400">
                        {date.getDate()}
                      </span>
                    </div>
                    <button
                      type="button"
                      onClick={() => setOpenDate(iso)}
                      className={
                        "min-h-[7rem] rounded-lg border p-2 text-left transition-colors hover:border-tennis-green/50 hover:bg-tennis-green/10 " +
                        (overlapping
                          ? "border-red-300 bg-red-50"
                          : "border-tennis-green/15 bg-tennis-green/5")
                      }
                    >
                      <p className="text-xs font-semibold text-gray-800">
                        {day.startTime}–{day.endTime}
                      </p>
                      {trainerIds.length === 0 ? (
                        <p className="mt-1.5 text-[11px] text-gray-400">
                          {t("noTrainersOnDay")}
                        </p>
                      ) : (
                        <ul className="mt-1.5 space-y-1">
                          {trainerIds.map((id) => {
                            const shifts = day.trainers
                              .filter((x) => x.trainerId === id)
                              .sort((a, b) =>
                                a.startTime.localeCompare(b.startTime),
                              );
                            const hours = shifts.reduce(
                              (s, x) => s + shiftHours(x),
                              0,
                            );
                            return (
                              <li
                                key={id}
                                className="text-[11px] leading-tight"
                              >
                                <span className="font-medium text-gray-700">
                                  {trainerLabel(day, id)}
                                </span>
                                <span className="block text-gray-400 tabular-nums">
                                  {shifts
                                    .map((s) => `${s.startTime}–${s.endTime}`)
                                    .join(", ")}
                                  {shifts.length > 1 &&
                                    ` (${hours.toLocaleString("nl-BE", { maximumFractionDigits: 1 })}u)`}
                                </span>
                              </li>
                            );
                          })}
                        </ul>
                      )}
                    </button>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {footer}

      {openDay && (
        <CampDayDialog
          key={openDay.date}
          day={openDay}
          trainers={trainers}
          onChange={(updated) =>
            onChange(days.map((d) => (d.date === updated.date ? updated : d)))
          }
          onClose={() => setOpenDate(null)}
        />
      )}
    </div>
  );
}
