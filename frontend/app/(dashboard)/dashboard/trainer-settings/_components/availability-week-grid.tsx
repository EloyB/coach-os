"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import type { TrainerAvailabilityDto } from "@/lib/api/trainerAvailabilities";
import {
  blockPosition,
  clubColorIndex,
  computeAxisRange,
  groupByDay,
  minutesFromOffset,
  toTime,
} from "@/lib/trainer-availability-layout";
import { blockColorClass } from "./availability-colors";
import type { AvailabilityFormValues } from "./availability-form-dialog";

const HOUR_HEIGHT_PX = 40;
/** Beweging onder deze afstand telt als klik, daarboven als slepen. */
const DRAG_THRESHOLD_PX = 6;
const DEFAULT_SLOT_MINUTES = 60;
const MIN_SLOT_MINUTES = 15;

interface AvailabilityWeekGridProps {
  availabilities: TrainerAvailabilityDto[];
  clubIds: string[];
  onAdd: (initial: AvailabilityFormValues) => void;
  onEdit: (availability: TrainerAvailabilityDto) => void;
}

interface DragState {
  day: number;
  startY: number;
  currentY: number;
}

/** Weekoverzicht met 7 kolommen en een tijdas, bedoeld voor desktop (vanaf `md`). */
export function AvailabilityWeekGrid({
  availabilities,
  clubIds,
  onAdd,
  onEdit,
}: AvailabilityWeekGridProps) {
  const t = useTranslations("trainerSettings");
  const range = computeAxisRange(availabilities);
  const grouped = groupByDay(availabilities);
  const heightPx = (range.endHour - range.startHour) * HOUR_HEIGHT_PX;
  const hours = Array.from(
    { length: range.endHour - range.startHour + 1 },
    (_, i) => range.startHour + i,
  );

  const [drag, setDrag] = useState<DragState | null>(null);
  const dragRef = useRef<DragState | null>(null);

  function offsetY(event: React.PointerEvent<HTMLDivElement>): number {
    return event.clientY - event.currentTarget.getBoundingClientRect().top;
  }

  function handlePointerDown(event: React.PointerEvent<HTMLDivElement>, day: number) {
    if (event.target !== event.currentTarget && !(event.target as HTMLElement).dataset.gridline) {
      return;
    }
    event.currentTarget.setPointerCapture(event.pointerId);
    const y = offsetY(event);
    dragRef.current = { day, startY: y, currentY: y };
    setDrag(dragRef.current);
  }

  function handlePointerMove(event: React.PointerEvent<HTMLDivElement>) {
    if (!dragRef.current) return;
    dragRef.current = { ...dragRef.current, currentY: offsetY(event) };
    setDrag(dragRef.current);
  }

  function handlePointerUp(event: React.PointerEvent<HTMLDivElement>) {
    const current = dragRef.current;
    dragRef.current = null;
    setDrag(null);
    if (!current) return;

    const endY = offsetY(event);
    let start: number;
    let end: number;
    if (Math.abs(endY - current.startY) < DRAG_THRESHOLD_PX) {
      start = minutesFromOffset(current.startY, heightPx, range);
      end = Math.min(start + DEFAULT_SLOT_MINUTES, range.endHour * 60);
    } else {
      const a = minutesFromOffset(current.startY, heightPx, range);
      const b = minutesFromOffset(endY, heightPx, range);
      start = Math.min(a, b);
      end = Math.max(a, b);
    }
    if (end - start < MIN_SLOT_MINUTES) {
      start = Math.max(end - MIN_SLOT_MINUTES, range.startHour * 60);
      end = Math.max(end, start + MIN_SLOT_MINUTES);
    }

    onAdd({
      tennisClubId: null,
      dayOfWeek: current.day,
      startTime: toTime(start),
      endTime: toTime(end),
    });
  }

  const dragTop = drag ? Math.min(drag.startY, drag.currentY) : 0;
  const dragHeight = drag ? Math.abs(drag.currentY - drag.startY) : 0;

  return (
    <div data-testid="availability-week-grid" className="hidden md:block">
      <p className="text-xs text-gray-400 mb-2">{t("gridHint")}</p>
      <div className="grid grid-cols-[44px_repeat(7,minmax(0,1fr))] gap-1">
        <div />
        {[0, 1, 2, 3, 4, 5, 6].map((day) => (
          <div key={day} className="text-center text-sm font-medium text-gray-700 pb-1">
            {t(`daysShort.${day}`)}
          </div>
        ))}

        <div className="relative" style={{ height: heightPx }}>
          {hours.map((hour) => (
            <span
              key={hour}
              className="absolute right-1 -translate-y-1/2 text-[11px] text-gray-400"
              style={{ top: (hour - range.startHour) * HOUR_HEIGHT_PX }}
            >
              {String(hour).padStart(2, "0")}u
            </span>
          ))}
        </div>

        {[0, 1, 2, 3, 4, 5, 6].map((day) => (
          <div
            key={day}
            data-testid={`availability-column-${day}`}
            className="relative rounded bg-gray-50 cursor-crosshair touch-none select-none"
            style={{ height: heightPx }}
            onPointerDown={(event) => handlePointerDown(event, day)}
            onPointerMove={handlePointerMove}
            onPointerUp={handlePointerUp}
          >
            {hours.slice(1, -1).map((hour) => (
              <div
                key={hour}
                data-gridline="true"
                className="absolute left-0 right-0 border-t border-gray-200"
                style={{ top: (hour - range.startHour) * HOUR_HEIGHT_PX }}
              />
            ))}

            {grouped[day].map((availability) => {
              const { topPercent, heightPercent } = blockPosition(
                availability.startTime,
                availability.endTime,
                range,
              );
              const colorIndex = clubColorIndex(availability.tennisClubId, clubIds);
              return (
                <button
                  key={availability.id}
                  type="button"
                  data-testid="availability-block"
                  onClick={() => onEdit(availability)}
                  className={`absolute left-0.5 right-0.5 overflow-hidden rounded border px-1.5 py-0.5 text-left text-[11px] leading-tight shadow-sm ${blockColorClass(colorIndex)}`}
                  style={{ top: `${topPercent}%`, height: `${heightPercent}%` }}
                >
                  <span className="font-semibold">
                    {availability.startTime} - {availability.endTime}
                  </span>
                  <span className="block truncate opacity-90">
                    {availability.tennisClubName ?? t("anyClub")}
                  </span>
                </button>
              );
            })}

            {drag && drag.day === day && (
              <div
                className="pointer-events-none absolute left-0.5 right-0.5 rounded border border-dashed border-tennis-green bg-tennis-green/20"
                style={{ top: dragTop, height: dragHeight }}
              />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
