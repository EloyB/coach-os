"use client";

import React from "react";

// ─── Constants ───────────────────────────────────────────────────────────────

export const START_HOUR = 6.5; // 06:30 — half-hour buffer so the 07:00 label isn't on the edge
export const END_HOUR = 24;
export const ROW_HEIGHT = 80; // px per hour
export const SLOT_GAP = 2; // px gap below each slot

// Full hour labels to render (skip 6 since we start at 6:30)
export const HOURS = Array.from(
  { length: END_HOUR - Math.ceil(START_HOUR) },
  (_, i) => Math.ceil(START_HOUR) + i
);

export const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];
export const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

// ─── Slot type ───────────────────────────────────────────────────────────────

export interface CalendarSlot {
  id: string;
  dayOfWeek: number; // 0 = Monday, 6 = Sunday
  startTime: string; // "HH:mm"
  endTime: string; // "HH:mm"
  trainerId: string | null;
  trainerName?: string | null;
  courtName?: string | null;
  maxStudents?: number;
  isCancelled?: boolean;
}

// ─── Utility functions ───────────────────────────────────────────────────────

export function formatTime(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
}

export function parseTime(time: string): number {
  const [h, m] = time.split(":").map(Number);
  return h * 60 + m;
}

// ─── Trainer colors ──────────────────────────────────────────────────────────

const NO_TRAINER_COLOR = {
  bg: "rgba(45,80,22,0.10)",
  border: "#2D5016",
  text: "#2D5016",
};

const TRAINER_COLORS = [
  { bg: "rgba(139,92,246,0.12)", border: "#8B5CF6", text: "#6D28D9" },
  { bg: "rgba(59,130,246,0.12)", border: "#3B82F6", text: "#2563EB" },
  { bg: "rgba(245,158,11,0.12)", border: "#F59E0B", text: "#B45309" },
  { bg: "rgba(236,72,153,0.12)", border: "#EC4899", text: "#BE185D" },
  { bg: "rgba(20,184,166,0.12)", border: "#14B8A6", text: "#0D9488" },
  { bg: "rgba(239,68,68,0.12)", border: "#EF4444", text: "#DC2626" },
  { bg: "rgba(249,115,22,0.12)", border: "#F97316", text: "#C2410C" },
  { bg: "rgba(168,85,247,0.12)", border: "#A855F7", text: "#7C3AED" },
] as const;

function hashString(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash + str.charCodeAt(i)) | 0;
  }
  return Math.abs(hash);
}

export function getTrainerColor(trainerId: string | null) {
  if (!trainerId) return NO_TRAINER_COLOR;
  return TRAINER_COLORS[hashString(trainerId) % TRAINER_COLORS.length];
}

// ─── Layout algorithm ────────────────────────────────────────────────────────

export function getSlotPosition(slot: CalendarSlot, startHour: number = START_HOUR) {
  const startMin = parseTime(slot.startTime);
  const endMin = parseTime(slot.endTime);
  const gridStartMin = startHour * 60;

  return {
    top: ((startMin - gridStartMin) / 60) * ROW_HEIGHT,
    height: Math.max(((endMin - startMin) / 60) * ROW_HEIGHT - SLOT_GAP, 20),
  };
}

export function layoutDaySlots(
  daySlots: CalendarSlot[]
): Map<string, { colIndex: number; totalCols: number }> {
  const sorted = [...daySlots].sort(
    (a, b) => parseTime(a.startTime) - parseTime(b.startTime)
  );
  const result = new Map<string, { colIndex: number; totalCols: number }>();
  if (sorted.length === 0) return result;

  const clusters: CalendarSlot[][] = [];
  let currentCluster: CalendarSlot[] = [sorted[0]];
  let clusterEnd = parseTime(sorted[0].endTime);

  for (let i = 1; i < sorted.length; i++) {
    const s = sorted[i];
    const start = parseTime(s.startTime);
    if (start < clusterEnd) {
      currentCluster.push(s);
      clusterEnd = Math.max(clusterEnd, parseTime(s.endTime));
    } else {
      clusters.push(currentCluster);
      currentCluster = [s];
      clusterEnd = parseTime(s.endTime);
    }
  }
  clusters.push(currentCluster);

  for (const cluster of clusters) {
    const columns: CalendarSlot[][] = [];

    for (const slot of cluster) {
      const slotStart = parseTime(slot.startTime);
      let placed = false;

      for (let col = 0; col < columns.length; col++) {
        const lastInCol = columns[col][columns[col].length - 1];
        if (parseTime(lastInCol.endTime) <= slotStart) {
          columns[col].push(slot);
          placed = true;
          break;
        }
      }

      if (!placed) {
        columns.push([slot]);
      }
    }

    const totalCols = columns.length;
    for (let col = 0; col < columns.length; col++) {
      for (const slot of columns[col]) {
        result.set(slot.id, { colIndex: col, totalCols });
      }
    }
  }

  return result;
}

// ─── Component ───────────────────────────────────────────────────────────────

interface CalendarGridProps {
  slots: CalendarSlot[];
  readOnly?: boolean;
  dayDates?: string[]; // 7 formatted date strings for day sub-headers
  /** Override the default start/end hours for the visible range */
  startHour?: number;
  endHour?: number;
  /** Reserve px on the right of each slot for action buttons (e.g. add-parallel) */
  slotRightPadding?: number;
  /** Ref forwarded to the grid body div (used by interactive wrappers for drag) */
  gridBodyRef?: React.RefObject<HTMLDivElement | null>;
  /** Render extra content inside each day column (e.g. hover cells, add buttons) */
  renderDayOverlay?: (dayIndex: number) => React.ReactNode;
  /** Render extra content inside the grid body wrapper (e.g. drag ghost) */
  renderGridOverlay?: () => React.ReactNode;
  /** Slot event handlers */
  onSlotMouseDown?: (slot: CalendarSlot, e: React.MouseEvent) => void;
  onSlotClick?: (slot: CalendarSlot, e: React.MouseEvent) => void;
  onSlotRemove?: (id: string, e: React.MouseEvent) => void;
  /** Additional class on the outer container */
  className?: string;
}

export function CalendarGrid({
  slots,
  readOnly,
  dayDates,
  startHour: startHourProp,
  endHour: endHourProp,
  slotRightPadding = 0,
  gridBodyRef,
  renderDayOverlay,
  renderGridOverlay,
  onSlotMouseDown,
  onSlotClick,
  onSlotRemove,
  className,
}: CalendarGridProps) {
  const effectiveStartHour = startHourProp ?? START_HOUR;
  const effectiveEndHour = endHourProp ?? END_HOUR;
  const totalHeight = (effectiveEndHour - effectiveStartHour) * ROW_HEIGHT;
  const gridColumns = `60px repeat(7, 1fr)`;
  const visibleHours = Array.from(
    { length: effectiveEndHour - Math.ceil(effectiveStartHour) },
    (_, i) => Math.ceil(effectiveStartHour) + i
  );

  return (
    <div
      className={`bg-paper rounded-xl border border-rule overflow-x-auto select-none ${className ?? ""}`}
    >
      <div style={{ minWidth: 1940 }}>
      {/* Day headers */}
      <div
        className="border-b border-rule bg-[#fbfaf6]"
        style={{ display: "grid", gridTemplateColumns: gridColumns }}
      >
        <div className="px-2 py-3" />
        {DAY_NAMES_SHORT.map((day, i) => (
          <div
            key={i}
            className="px-3 py-2 text-center border-l border-rule"
          >
            <div className="flex justify-between items-baseline px-1">
              <span className="text-[10.5px] font-bold text-ink uppercase tracking-[0.06em]">
                {day}
              </span>
              <span className="text-[10.5px] text-ink-3 font-mono">
                {dayDates?.[i] ?? ""}
              </span>
            </div>
          </div>
        ))}
      </div>

      {/* Grid body wrapper */}
      <div className="relative">
        <div
          ref={gridBodyRef}
          className="relative"
          style={{ display: "grid", gridTemplateColumns: gridColumns }}
        >
          {/* Time axis */}
          <div className="relative" style={{ height: totalHeight }}>
            {visibleHours.map((hour) => (
              <div
                key={hour}
                className="absolute left-0 right-0 flex items-start justify-end px-2"
                style={{ top: (hour - effectiveStartHour) * ROW_HEIGHT }}
              >
                <span className="relative -top-2 text-[9.5px] text-ink-3 font-mono">
                  {String(hour).padStart(2, "0")}
                </span>
              </div>
            ))}
          </div>

          {/* Day columns */}
          {DAY_NAMES_SHORT.map((_, dayIndex) => {
            const daySlots = slots
              .filter((s) => s.dayOfWeek === dayIndex)
              .sort((a, b) => a.startTime.localeCompare(b.startTime));
            const layout = layoutDaySlots(daySlots);

            return (
              <div
                key={dayIndex}
                className={`relative border-l border-rule ${readOnly ? "" : "cursor-pointer group/day"}`}
                style={{ height: totalHeight }}
              >
                {/* Leading half-hour cell */}
                {effectiveStartHour % 1 !== 0 && (
                  <div
                    key="leading-half"
                    className={`absolute left-0 right-0 border-b border-rule ${!readOnly ? "hover:bg-tennis-green/5 transition-colors" : ""}`}
                    style={{ top: 0, height: (Math.ceil(effectiveStartHour) - effectiveStartHour) * ROW_HEIGHT }}
                  />
                )}

                {/* Gridlines */}
                {visibleHours.flatMap((hour) => {
                  const baseTop = (hour - effectiveStartHour) * ROW_HEIGHT;
                  const halfHeight = ROW_HEIGHT / 2;
                  return [
                    <div
                      key={`${hour}-0`}
                      className={`absolute left-0 right-0 border-b border-dashed border-rule ${!readOnly ? "hover:bg-tennis-green/5 transition-colors" : ""}`}
                      style={{ top: baseTop, height: halfHeight }}
                    />,
                    <div
                      key={`${hour}-30`}
                      className={`absolute left-0 right-0 border-b border-rule ${!readOnly ? "hover:bg-tennis-green/5 transition-colors" : ""}`}
                      style={{ top: baseTop + halfHeight, height: halfHeight }}
                    />,
                  ];
                })}

                {/* Day overlay (interactive cells, add buttons, etc.) */}
                {renderDayOverlay?.(dayIndex)}

                {/* Slot blocks */}
                {daySlots.map((slot) => {
                  const pos = getSlotPosition(slot, effectiveStartHour);
                  const color = getTrainerColor(slot.trainerId);
                  const col = layout.get(slot.id) ?? {
                    colIndex: 0,
                    totalCols: 1,
                  };
                  const cancelled = slot.isCancelled === true;
                  const slotBg = cancelled ? "rgba(156,163,175,0.15)" : color.bg;
                  const slotBorder = cancelled ? "#9CA3AF" : color.border;

                  return (
                    <div
                      key={slot.id}
                      data-slot-id={slot.id}
                      className={`absolute border-l-[3px] rounded-r-md px-1.5 py-1 transition-all group z-10 ${
                        onSlotClick ? "cursor-pointer hover:brightness-95 hover:shadow-md" : readOnly ? "cursor-default" : "cursor-grab"
                      } ${cancelled ? "opacity-60" : ""}`}
                      style={{
                        top: pos.top,
                        height: pos.height,
                        left: `calc(${(col.colIndex / col.totalCols)} * (100% - ${slotRightPadding}px) + 1px)`,
                        width: `calc(${(1 / col.totalCols)} * (100% - ${slotRightPadding}px) - 2px)`,
                        backgroundColor: slotBg,
                        borderLeftColor: slotBorder,
                      }}
                      onMouseDown={
                        onSlotMouseDown
                          ? (e) => onSlotMouseDown(slot, e)
                          : undefined
                      }
                      onClick={
                        onSlotClick
                          ? (e) => onSlotClick(slot, e)
                          : undefined
                      }
                    >
                      {/* Remove button */}
                      {onSlotRemove && (
                        <button
                          type="button"
                          data-slot-id={slot.id}
                          onClick={(e) => {
                            e.stopPropagation();
                            onSlotRemove(slot.id, e);
                          }}
                          className="absolute top-1 right-1 opacity-0 group-hover:opacity-100 w-4 h-4 flex items-center justify-center text-gray-400 hover:text-red-500 transition-all z-20"
                        >
                          <svg
                            width="10"
                            height="10"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="2"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                          >
                            <line x1="18" y1="6" x2="6" y2="18" />
                            <line x1="6" y1="6" x2="18" y2="18" />
                          </svg>
                        </button>
                      )}

                      {cancelled && (
                        <div className="text-[9px] font-bold uppercase tracking-wide text-gray-500 mb-0.5">
                          Geannuleerd
                        </div>
                      )}
                      <div
                        className={`text-[11px] font-semibold ${cancelled ? "line-through" : ""}`}
                        style={{ color: cancelled ? "#9CA3AF" : color.text }}
                      >
                        {slot.startTime} — {slot.endTime}
                      </div>
                      {pos.height >= 36 && (
                        <div className="text-[10px] text-ink-2 truncate">
                          {slot.courtName || slot.trainerName ? (
                            <>
                              {slot.courtName && <>{slot.courtName} · </>}
                              {slot.trainerName && <>{slot.trainerName} · </>}
                              {slot.maxStudents != null && (
                                <span className="text-gray-400">
                                  {slot.maxStudents} max
                                </span>
                              )}
                            </>
                          ) : slot.maxStudents != null ? (
                            <span className="text-gray-400 italic">
                              {slot.maxStudents} max
                            </span>
                          ) : null}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            );
          })}
        </div>

        {/* Grid overlay (drag ghost, etc.) */}
        {renderGridOverlay?.()}
      </div>
      </div>
    </div>
  );
}
