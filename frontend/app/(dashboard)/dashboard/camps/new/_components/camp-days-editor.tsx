"use client";

import { useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { ChevronLeft, ChevronRight, Clock } from "lucide-react";
import {
  CalendarGrid,
  getSlotPosition,
  parseTime,
} from "@/components/calendar/calendar-grid";
import type { TrainerDto } from "@/lib/api/trainers";
import { formatDateShort } from "@/lib/date-utils";
import {
  DEFAULT_DAY_PARTS,
  dayEnd,
  dayStart,
  formatDayHeading,
  type CampDayDraft,
  type CampDayParts,
} from "../_types";
import { CampDayDialog } from "./camp-day-dialog";

interface CampDaysEditorProps {
  days: CampDayDraft[];
  onChange: (days: CampDayDraft[]) => void;
  trainers: TrainerDto[];
  /** Standaarduren voor nieuwe kampdagen en voor dagdelen die je aanzet. */
  defaultParts?: CampDayParts;
  /** Kampperiode: data hierin zonder kampdag tonen als "geen kampdag" en kunnen toegevoegd worden. */
  rangeStart?: string;
  rangeEnd?: string;
  /**
   * Bewaart de dagen vanuit de dag-dialog en geeft foutmeldingen terug (leeg = gelukt).
   * Zonder deze prop past de dialog de wijzigingen enkel lokaal toe.
   */
  onSaveDays?: (days: CampDayDraft[]) => Promise<string[]>;
  /** Optionele voettekst onderaan de kaart (bv. een opslaan-balk die in beeld blijft plakken). */
  footer?: React.ReactNode;
}

/** "Voormiddag 09:15 – 12:00 · Namiddag 13:15 – 16:00", enkel de dagdelen die bestaan. */
function partsSummary(
  day: CampDayDraft,
  morningLabel: string,
  afternoonLabel: string,
): string {
  const parts: string[] = [];
  if (day.morningStart && day.morningEnd)
    parts.push(`${morningLabel} ${day.morningStart} – ${day.morningEnd}`);
  if (day.afternoonStart && day.afternoonEnd)
    parts.push(`${afternoonLabel} ${day.afternoonStart} – ${day.afternoonEnd}`);
  return parts.join(" · ");
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
  defaultParts = DEFAULT_DAY_PARTS,
  rangeStart,
  rangeEnd,
  onSaveDays,
  footer,
}: CampDaysEditorProps) {
  const t = useTranslations("camps");
  const [weekIndex, setWeekIndex] = useState(0);
  const [openDate, setOpenDate] = useState<string | null>(null);

  // Alle weken (als maandag) die de kampperiode en de kampdagen bestrijken.
  const weeks = useMemo(() => {
    const dates = days.map((d) => d.date);
    if (rangeStart) dates.push(rangeStart);
    if (rangeEnd) dates.push(rangeEnd);
    if (dates.length === 0) return [] as Date[];
    dates.sort();
    const result: Date[] = [];
    const cur = mondayOf(dates[0]);
    const last = mondayOf(dates[dates.length - 1]);
    while (cur <= last) {
      result.push(new Date(cur));
      cur.setDate(cur.getDate() + 7);
    }
    return result;
  }, [days, rangeStart, rangeEnd]);

  const safeIndex = Math.min(weekIndex, Math.max(weeks.length - 1, 0));
  const monday = weeks[safeIndex];
  const dayByDate = new Map(days.map((d) => [d.date, d]));
  // Een nieuw toe te voegen dag bestaat pas in `days` nadat de dialog is opgeslagen.
  const openDay = openDate
    ? (dayByDate.get(openDate) ?? {
        date: openDate,
        ...defaultParts,
        trainers: [],
      })
    : undefined;
  const openDayExists = openDate ? dayByDate.has(openDate) : false;
  const isInRange = (date: string) =>
    !rangeStart || !rangeEnd || (date >= rangeStart && date <= rangeEnd);

  /** Bewaar (indien mogelijk) en pas de dagen toe; geeft foutmeldingen terug (leeg = gelukt). */
  async function commit(next: CampDayDraft[]): Promise<string[]> {
    const errors = onSaveDays ? await onSaveDays(next) : [];
    if (errors.length === 0) {
      onChange(next);
      setOpenDate(null);
    }
    return errors;
  }

  const trainerName = (day: CampDayDraft, trainerId: string): string => {
    const tr = trainers.find((x) => x.id === trainerId);
    if (tr) return `${tr.firstName} ${tr.lastName}`;
    return (
      day.trainers.find((x) => x.trainerId === trainerId)?.trainerName ??
      trainerId
    );
  };

  // Zichtbaar uurbereik over het hele kamp (en de standaarduren voor nieuwe dagen), zodat de
  // rasterhoogte bij het bladeren gelijk blijft.
  const hourRanges: [string, string][] = [
    [dayStart(defaultParts), dayEnd(defaultParts)],
  ];
  for (const d of days) {
    hourRanges.push([dayStart(d), dayEnd(d)]);
    d.trainers.forEach((tr) => hourRanges.push([tr.startTime, tr.endTime]));
  }
  const minMin = Math.min(...hourRanges.map(([start]) => parseTime(start)));
  const maxMin = Math.max(...hourRanges.map(([, end]) => parseTime(end)));
  const calStartHour = Math.max(0, Math.floor(minMin / 60) - 1) + 0.5;
  const calEndHour = Math.min(24, Math.ceil(maxMin / 60) + 1);

  const weekDates = monday
    ? Array.from({ length: 7 }, (_, i) => {
        const d = new Date(monday);
        d.setDate(d.getDate() + i);
        return toIso(d);
      })
    : [];

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
        {!monday ? (
          <p className="text-xs text-gray-400 py-2">{t("noDays")}</p>
        ) : (
          <>
            {/* Mobiel: lijst met de kampdagen van deze week */}
            <div className="sm:hidden space-y-2">
              {weekDates.map((date) => {
                const day = dayByDate.get(date);
                if (!day) {
                  if (!isInRange(date)) return null;
                  return (
                    <button
                      key={date}
                      type="button"
                      onClick={() => setOpenDate(date)}
                      className="w-full rounded-xl border border-dashed border-gray-300 p-3 text-left text-gray-400 hover:border-tennis-green/50"
                    >
                      <p className="text-sm font-medium first-letter:uppercase">
                        {formatDayHeading(date)}
                      </p>
                      <p className="text-xs">
                        {t("noCampDay")} · {t("addCampDayShort")}
                      </p>
                    </button>
                  );
                }
                return (
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
                      {partsSummary(day, t("morning"), t("afternoon"))}
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
                );
              })}
            </div>

            {/* Desktop: kalender-raster, klik een kampdag of shift om de trainers in te stellen */}
            <div className="hidden sm:block">
              <CalendarGrid
                slots={[]}
                readOnly
                dayDates={weekDates.map(formatDateShort)}
                startHour={calStartHour}
                endHour={calEndHour}
                renderDayOverlay={(dayIndex) => {
                  const date = weekDates[dayIndex];
                  const day = dayByDate.get(date);
                  if (!day) {
                    if (!isInRange(date)) return null;
                    const offPos = getSlotPosition(
                      {
                        id: date,
                        dayOfWeek: dayIndex,
                        startTime: dayStart(defaultParts),
                        endTime: dayEnd(defaultParts),
                        trainerId: null,
                      },
                      calStartHour,
                    );
                    return (
                      <button
                        type="button"
                        onClick={() => setOpenDate(date)}
                        aria-label={t("addCampDay", {
                          date: formatDayHeading(date),
                        })}
                        className="absolute inset-x-px z-10 flex cursor-pointer flex-col items-start justify-start rounded-md border border-dashed border-gray-300 px-1.5 py-1 text-left text-gray-400 transition-colors hover:border-tennis-green/50 hover:bg-tennis-green/5 hover:text-tennis-green"
                        style={{ top: offPos.top, height: offPos.height }}
                      >
                        <span className="text-[11px] font-medium">
                          {t("noCampDay")}
                        </span>
                        <span className="text-[10px]">
                          {t("addCampDayShort")}
                        </span>
                      </button>
                    );
                  }
                  const count = new Set(day.trainers.map((tr) => tr.trainerId))
                    .size;
                  const parts = [
                    {
                      key: "morning",
                      start: day.morningStart,
                      end: day.morningEnd,
                    },
                    {
                      key: "afternoon",
                      start: day.afternoonStart,
                      end: day.afternoonEnd,
                    },
                  ].filter(
                    (p): p is { key: string; start: string; end: string } =>
                      p.start !== null && p.end !== null,
                  );
                  return (
                    <>
                      {parts.map((part, i) => {
                        const pos = getSlotPosition(
                          {
                            id: `${date}-${part.key}`,
                            dayOfWeek: dayIndex,
                            startTime: part.start,
                            endTime: part.end,
                            trainerId: null,
                          },
                          calStartHour,
                        );
                        return (
                          <button
                            key={part.key}
                            type="button"
                            onClick={() => setOpenDate(date)}
                            aria-label={`${formatDayHeading(date)} ${t(part.key)}`}
                            className="absolute inset-x-px z-10 cursor-pointer flex flex-col items-start justify-start overflow-hidden rounded-r-md border-l-[3px] border-tennis-green bg-tennis-green/10 px-1.5 py-1 text-left transition-all hover:shadow-md hover:brightness-95"
                            style={{ top: pos.top, height: pos.height }}
                          >
                            <div className="text-[11px] font-semibold text-tennis-green">
                              {part.start} — {part.end}
                            </div>
                            {i === 0 && (
                              <div
                                className={`text-[10px] ${count === 0 ? "italic text-gray-400" : "text-ink-2"}`}
                              >
                                {t("trainersPresent", { count })}
                              </div>
                            )}
                          </button>
                        );
                      })}
                    </>
                  );
                }}
              />
            </div>
          </>
        )}
      </div>

      {footer}

      {openDay && (
        <CampDayDialog
          key={openDay.date}
          day={openDay}
          trainers={trainers}
          saveLabel={onSaveDays ? t("save") : t("dayDialogApply")}
          defaultParts={defaultParts}
          isNew={!openDayExists}
          canRemove={openDayExists && days.length > 1}
          onSave={async (updated) => {
            const next = openDayExists
              ? days.map((d) => (d.date === updated.date ? updated : d))
              : [...days, updated].sort((a, b) => a.date.localeCompare(b.date));
            return commit(next);
          }}
          onRemove={async () =>
            commit(days.filter((d) => d.date !== openDay.date))
          }
          onClose={() => setOpenDate(null)}
        />
      )}
    </div>
  );
}
