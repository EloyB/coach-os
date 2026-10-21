"use client";

import { useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ChevronLeft, ChevronRight, Clock } from "lucide-react";
import {
  CalendarGrid,
  getSlotPosition,
  getTrainerColor,
  parseTime,
  type CalendarSlot,
} from "@/components/calendar/calendar-grid";
import type { TrainerDto } from "@/lib/api/trainers";
import { formatDateShort } from "@/lib/date-utils";
import { formatDayHeading, type CampDayDraft } from "../_types";
import { CampDayDialog } from "./camp-day-dialog";

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

export function CampDaysEditor({
  days,
  onChange,
  trainers,
  footer,
}: CampDaysEditorProps) {
  const t = useTranslations("camps");
  const [weekIndex, setWeekIndex] = useState(0);
  const [openDate, setOpenDate] = useState<string | null>(null);

  // Alle weken (als maandag) die de kampdagen bestrijken.
  const weeks = useMemo(() => {
    if (days.length === 0) return [] as Date[];
    const sorted = days.map((d) => d.date).sort();
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

  const trainerName = (day: CampDayDraft, trainerId: string): string => {
    const tr = trainers.find((x) => x.id === trainerId);
    if (tr) return `${tr.firstName} ${tr.lastName}`;
    return (
      day.trainers.find((x) => x.trainerId === trainerId)?.trainerName ??
      trainerId
    );
  };

  // Zichtbaar uurbereik over het hele kamp, zodat de rasterhoogte bij het bladeren gelijk blijft.
  let calStartHour: number | undefined;
  let calEndHour: number | undefined;
  if (days.length > 0) {
    let minMin = Infinity;
    let maxMin = -Infinity;
    for (const d of days) {
      for (const [start, end] of [
        [d.startTime, d.endTime],
        ...d.trainers.map((tr) => [tr.startTime, tr.endTime]),
      ]) {
        minMin = Math.min(minMin, parseTime(start));
        maxMin = Math.max(maxMin, parseTime(end));
      }
    }
    calStartHour = Math.max(0, Math.floor(minMin / 60) - 1) + 0.5;
    calEndHour = Math.min(24, Math.ceil(maxMin / 60) + 1);
  }

  const weekDates = monday
    ? Array.from({ length: 7 }, (_, i) => {
        const d = new Date(monday);
        d.setDate(d.getDate() + i);
        return toIso(d);
      })
    : [];

  // Elke shift is een blok in het raster; de kampuren van een dag staan als achtergrond.
  const slots: CalendarSlot[] = weekDates.flatMap((date, dayOfWeek) => {
    const day = dayByDate.get(date);
    if (!day) return [];
    return day.trainers.map((tr, index) => ({
      id: `${date}:${index}`,
      dayOfWeek,
      startTime: tr.startTime,
      endTime: tr.endTime,
      trainerId: tr.trainerId,
      trainerName: trainerName(day, tr.trainerId),
    }));
  });

  const legendTrainerIds = [
    ...new Set(days.flatMap((d) => d.trainers.map((tr) => tr.trainerId))),
  ];
  const firstDayWithTrainer = (id: string) =>
    days.find((d) => d.trainers.some((tr) => tr.trainerId === id));

  const rangeLabel = monday
    ? `${formatDateShort(weekDates[0])} – ${formatDateShort(weekDates[6])}`
    : "";

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
        {monday && (
          <div className="flex shrink-0 items-center gap-2">
            <span className="hidden sm:inline text-xs text-gray-500 whitespace-nowrap">
              {t("weekOf", { current: safeIndex + 1, total: weeks.length })}
            </span>
            <span className="text-xs text-gray-400 whitespace-nowrap">
              {rangeLabel}
            </span>
            {weeks.length > 1 && (
              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={() => setWeekIndex(Math.max(safeIndex - 1, 0))}
                  disabled={safeIndex === 0}
                  aria-label={t("weekPrevious")}
                  className="w-7 h-7 flex items-center justify-center rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                >
                  <ChevronLeft size={14} />
                </button>
                <button
                  type="button"
                  onClick={() =>
                    setWeekIndex(Math.min(safeIndex + 1, weeks.length - 1))
                  }
                  disabled={safeIndex >= weeks.length - 1}
                  aria-label={t("weekNext")}
                  className="w-7 h-7 flex items-center justify-center rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                >
                  <ChevronRight size={14} />
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      <div className="p-6">
        {days.length === 0 || !monday ? (
          <p className="text-xs text-gray-400 py-2">{t("noDays")}</p>
        ) : (
          <>
            {/* Mobiel: lijst met de kampdagen van deze week */}
            <div className="sm:hidden space-y-2">
              {weekDates
                .map((date) => dayByDate.get(date))
                .filter((d): d is CampDayDraft => d !== undefined)
                .map((day) => (
                  <button
                    key={day.date}
                    type="button"
                    onClick={() => setOpenDate(day.date)}
                    className="w-full rounded-xl border border-gray-100 p-3 text-left hover:border-gray-200"
                  >
                    <p className="text-sm font-semibold text-gray-900 first-letter:uppercase">
                      {formatDayHeading(day.date)}
                    </p>
                    <p className="text-xs text-gray-500">
                      {day.startTime} – {day.endTime}
                    </p>
                    <p className="mt-1 text-xs text-gray-400">
                      {day.trainers.length === 0
                        ? t("noTrainersOnDay")
                        : [
                            ...new Set(
                              day.trainers.map((tr) =>
                                trainerName(day, tr.trainerId),
                              ),
                            ),
                          ].join(", ")}
                    </p>
                  </button>
                ))}
            </div>

            {/* Desktop: kalender-raster, klik een kampdag of shift om de trainers in te stellen */}
            <div className="hidden sm:block">
              <CalendarGrid
                slots={slots}
                readOnly
                dayDates={weekDates.map(formatDateShort)}
                startHour={calStartHour}
                endHour={calEndHour}
                onSlotClick={(slot) => setOpenDate(slot.id.split(":")[0])}
                renderDayOverlay={(dayIndex) => {
                  const date = weekDates[dayIndex];
                  const day = dayByDate.get(date);
                  if (!day) return null;
                  const pos = getSlotPosition(
                    {
                      id: date,
                      dayOfWeek: dayIndex,
                      startTime: day.startTime,
                      endTime: day.endTime,
                      trainerId: null,
                    },
                    calStartHour,
                  );
                  return (
                    <button
                      type="button"
                      onClick={() => setOpenDate(date)}
                      aria-label={formatDayHeading(date)}
                      className="absolute inset-x-0 border-y border-tennis-green/25 bg-tennis-green/5 text-left transition-colors hover:bg-tennis-green/10"
                      style={{ top: pos.top, height: pos.height + 2 }}
                    >
                      {day.trainers.length === 0 && (
                        <span className="block px-2 pt-1 text-[10px] text-gray-400">
                          {t("noTrainersOnDay")}
                        </span>
                      )}
                    </button>
                  );
                }}
              />
            </div>

            {legendTrainerIds.length > 0 && (
              <div className="mt-3 flex flex-wrap items-center gap-4 px-1">
                {legendTrainerIds.map((id) => {
                  const day = firstDayWithTrainer(id);
                  return (
                    <div
                      key={id}
                      className="flex items-center gap-1.5 text-xs text-gray-500"
                    >
                      <div
                        className="h-3 w-3 rounded-full"
                        style={{ backgroundColor: getTrainerColor(id).border }}
                      />
                      {day ? trainerName(day, id) : id}
                    </div>
                  );
                })}
              </div>
            )}
          </>
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
