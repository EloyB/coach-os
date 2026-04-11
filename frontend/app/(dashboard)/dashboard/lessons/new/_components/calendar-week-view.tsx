"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { Plus, X } from "lucide-react";
import { useTranslations } from "next-intl";
import type { WizardSlot } from "../_types";
import { SlotEditPopover } from "./slot-edit-popover";

const START_HOUR = 7;
const END_HOUR = 21;
const ROW_HEIGHT = 48; // px per hour
const HOURS = Array.from(
  { length: END_HOUR - START_HOUR },
  (_, i) => START_HOUR + i,
);
const DAY_NAMES_SHORT = ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"];
const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];
const DRAG_THRESHOLD = 5; // px before drag starts

// No-trainer color (greenish default)
const NO_TRAINER_COLOR = {
  bg: "rgba(45,80,22,0.10)",
  border: "#2D5016",
  text: "#2D5016",
};

// Trainer color palette — only non-green colors so they stand out from unassigned slots
const TRAINER_COLORS = [
  { bg: "rgba(139,92,246,0.12)", border: "#8B5CF6", text: "#6D28D9" }, // purple
  { bg: "rgba(59,130,246,0.12)", border: "#3B82F6", text: "#2563EB" }, // blue
  { bg: "rgba(245,158,11,0.12)", border: "#F59E0B", text: "#B45309" }, // amber
  { bg: "rgba(236,72,153,0.12)", border: "#EC4899", text: "#BE185D" }, // pink
  { bg: "rgba(20,184,166,0.12)", border: "#14B8A6", text: "#0D9488" }, // teal
  { bg: "rgba(239,68,68,0.12)", border: "#EF4444", text: "#DC2626" }, // red
  { bg: "rgba(249,115,22,0.12)", border: "#F97316", text: "#C2410C" }, // orange
  { bg: "rgba(168,85,247,0.12)", border: "#A855F7", text: "#7C3AED" }, // violet
] as const;

function hashString(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash + str.charCodeAt(i)) | 0;
  }
  return Math.abs(hash);
}

function getTrainerColor(trainerId: string | null) {
  if (!trainerId) return NO_TRAINER_COLOR;
  return TRAINER_COLORS[hashString(trainerId) % TRAINER_COLORS.length];
}

function formatTime(minutes: number): string {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
}

function parseTime(time: string): number {
  const [h, m] = time.split(":").map(Number);
  return h * 60 + m;
}

const SLOT_GAP = 2; // px gap below each slot for visual separation

function getSlotPosition(slot: WizardSlot) {
  const startMin = parseTime(slot.startTime);
  const endMin = parseTime(slot.endTime);
  const gridStartMin = START_HOUR * 60;

  return {
    top: ((startMin - gridStartMin) / 60) * ROW_HEIGHT,
    height: Math.max(((endMin - startMin) / 60) * ROW_HEIGHT - SLOT_GAP, 20),
  };
}

/** Assign column positions to overlapping slots (side-by-side layout) */
function layoutDaySlots(
  daySlots: WizardSlot[]
): Map<string, { colIndex: number; totalCols: number }> {
  const sorted = [...daySlots].sort(
    (a, b) => parseTime(a.startTime) - parseTime(b.startTime)
  );
  const result = new Map<string, { colIndex: number; totalCols: number }>();
  if (sorted.length === 0) return result;

  // Group into clusters of mutually overlapping slots
  const clusters: WizardSlot[][] = [];
  let currentCluster: WizardSlot[] = [sorted[0]];
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

  // For each cluster, greedily assign columns
  for (const cluster of clusters) {
    const columns: WizardSlot[][] = [];

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

// ─── Types ───────────────────────────────────────────────────────────────────

export type SlotDefaults = {
  trainerId: string | null;
  trainerName: string | null;
  courtName: string | null;
  maxStudents: number;
  level: number | null;
};

type DragState = {
  slotId: string;
  durationMin: number;
  offsetY: number;
  startX: number;
  startY: number;
  previewDay: number;
  previewStartMin: number;
  isValid: boolean;
  moved: boolean;
};

// ─── Component ───────────────────────────────────────────────────────────────

interface CalendarWeekViewProps {
  slots: WizardSlot[];
  onChange: (slots: WizardSlot[]) => void;
  defaults?: SlotDefaults;
}

export function CalendarWeekView({
  slots,
  onChange,
  defaults,
}: CalendarWeekViewProps) {
  const t = useTranslations("lessonWizard");
  const gridBodyRef = useRef<HTMLDivElement>(null);
  const [drag, setDrag] = useState<DragState | null>(null);
  const dragRef = useRef<DragState | null>(null);
  const justDraggedRef = useRef(false);
  const [editingSlotId, setEditingSlotId] = useState<string | null>(null);
  const [editAnchor, setEditAnchor] = useState<HTMLElement | null>(null);
  const [ghostStyle, setGhostStyle] = useState<React.CSSProperties | null>(null);

  // Stable refs so the effect doesn't re-register listeners on every change
  const slotsRef = useRef(slots);
  const onChangeRef = useRef(onChange);
  useEffect(() => {
    slotsRef.current = slots;
    onChangeRef.current = onChange;
    dragRef.current = drag;
  });

  // ─── Drag handlers ──────────────────────────────────────────────────────

  function handleSlotMouseDown(slot: WizardSlot, e: React.MouseEvent) {
    // Don't interfere with the X button
    if ((e.target as HTMLElement).closest("button")) return;
    e.preventDefault();

    const slotEl = e.currentTarget as HTMLElement;
    const slotRect = slotEl.getBoundingClientRect();

    setDrag({
      slotId: slot.id,
      durationMin: parseTime(slot.endTime) - parseTime(slot.startTime),
      offsetY: e.clientY - slotRect.top,
      startX: e.clientX,
      startY: e.clientY,
      previewDay: slot.dayOfWeek,
      previewStartMin: parseTime(slot.startTime),
      isValid: true,
      moved: false,
    });
  }

  const calcPosition = useCallback((e: MouseEvent, currentDrag: DragState) => {
    const grid = gridBodyRef.current;
    if (!grid) return null;

    const gridRect = grid.getBoundingClientRect();
    const x = e.clientX - gridRect.left;
    const y = e.clientY - gridRect.top;

    const colWidth = (gridRect.width - 60) / 7;
    const dayIndex = Math.max(0, Math.min(6, Math.floor((x - 60) / colWidth)));

    const adjustedY = y - currentDrag.offsetY;
    const totalMin = START_HOUR * 60 + (adjustedY / ROW_HEIGHT) * 60;
    const snappedMin = Math.floor(totalMin / 30) * 30;
    const clampedStart = Math.max(
      START_HOUR * 60,
      Math.min(snappedMin, END_HOUR * 60 - currentDrag.durationMin),
    );

    return { previewDay: dayIndex, previewStartMin: clampedStart, isValid: true };
  }, []);

  useEffect(() => {
    if (!drag) return;

    function handleMouseMove(e: MouseEvent) {
      setDrag((prev) => {
        if (!prev) return null;

        const dx = e.clientX - prev.startX;
        const dy = e.clientY - prev.startY;
        const moved =
          prev.moved || Math.abs(dx) + Math.abs(dy) >= DRAG_THRESHOLD;
        if (!moved) return { ...prev, moved };

        const pos = calcPosition(e, prev);
        if (!pos) return prev;

        return { ...prev, moved, ...pos };
      });
    }

    function handleMouseUp() {
      const prev = dragRef.current;
      setDrag(null);

      if (prev?.moved && prev.isValid) {
        const newStart = formatTime(prev.previewStartMin);
        const newEnd = formatTime(prev.previewStartMin + prev.durationMin);
        onChangeRef.current(
          slotsRef.current.map((s) =>
            s.id === prev.slotId
              ? {
                  ...s,
                  dayOfWeek: prev.previewDay,
                  startTime: newStart,
                  endTime: newEnd,
                }
              : s,
          ),
        );
        justDraggedRef.current = true;
        requestAnimationFrame(() => {
          justDraggedRef.current = false;
        });
      }
    }

    window.addEventListener("mousemove", handleMouseMove);
    window.addEventListener("mouseup", handleMouseUp);
    return () => {
      window.removeEventListener("mousemove", handleMouseMove);
      window.removeEventListener("mouseup", handleMouseUp);
    };
  }, [drag !== null, calcPosition]);

  // ─── Click handlers ─────────────────────────────────────────────────────

  function handleRemoveSlot(id: string, e: React.MouseEvent) {
    e.stopPropagation();
    setEditingSlotId(null);
    onChange(slots.filter((s) => s.id !== id));
  }

  function handleSlotClick(slot: WizardSlot, e: React.MouseEvent) {
    if (justDraggedRef.current) return;
    if ((e.target as HTMLElement).closest("button")) return;

    const el = e.currentTarget as HTMLElement;
    setEditAnchor(el);
    setEditingSlotId(slot.id);
  }

  function handleSlotSave(updated: WizardSlot) {
    onChange(slots.map((s) => (s.id === updated.id ? updated : s)));
    setEditingSlotId(null);
  }

  function handleAddParallelSlot(slot: WizardSlot, e: React.MouseEvent) {
    e.stopPropagation();
    onChange([
      ...slots,
      {
        id: crypto.randomUUID(),
        dayOfWeek: slot.dayOfWeek,
        startTime: slot.startTime,
        endTime: slot.endTime,
        trainerId: defaults?.trainerId ?? null,
        trainerName: defaults?.trainerName ?? null,
        courtName: defaults?.courtName ?? null,
        maxStudents: defaults?.maxStudents ?? 4,
        level: defaults?.level ?? null,
      },
    ]);
  }

  function handleDayClick(
    dayIndex: number,
    e: React.MouseEvent<HTMLDivElement>,
  ) {
    if (justDraggedRef.current) return;
    if ((e.target as HTMLElement).closest("[data-slot-id]")) return;

    const rect = e.currentTarget.getBoundingClientRect();
    const y = e.clientY - rect.top;
    const totalMinutes = START_HOUR * 60 + (y / ROW_HEIGHT) * 60;
    const snappedMinutes = Math.floor(totalMinutes / 30) * 30;

    const clampedStart = Math.max(
      START_HOUR * 60,
      Math.min(snappedMinutes, (END_HOUR - 1) * 60),
    );
    const clampedEnd = Math.min(END_HOUR * 60, clampedStart + 60);

    onChange([
      ...slots,
      {
        id: crypto.randomUUID(),
        dayOfWeek: dayIndex,
        startTime: formatTime(clampedStart),
        endTime: formatTime(clampedEnd),
        trainerId: defaults?.trainerId ?? null,
        trainerName: defaults?.trainerName ?? null,
        courtName: defaults?.courtName ?? null,
        maxStudents: defaults?.maxStudents ?? 4,
        level: defaults?.level ?? null,
      },
    ]);
  }

  // ─── Ghost position — update via effect when drag changes ───────────────

  useEffect(() => {
    if (!drag?.moved || !gridBodyRef.current) {
      setGhostStyle(null);
      return;
    }

    const gridRect = gridBodyRef.current.getBoundingClientRect();
    const colWidth = (gridRect.width - 60) / 7;

    setGhostStyle({
      left: 60 + drag.previewDay * colWidth + 2,
      top: ((drag.previewStartMin - START_HOUR * 60) / 60) * ROW_HEIGHT,
      width: colWidth - 4,
      height: (drag.durationMin / 60) * ROW_HEIGHT,
    });
  }, [drag]);

  // ─── Render ─────────────────────────────────────────────────────────────

  const totalHeight = HOURS.length * ROW_HEIGHT;
  const gridColumns = `60px repeat(7, 1fr)`;
  const isDragging = drag?.moved ?? false;

  return (
    <>
      <div
        className={`bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden select-none ${isDragging ? "cursor-grabbing" : ""}`}
      >
        {/* Day headers */}
        <div
          className="border-b border-gray-200 bg-gray-50"
          style={{ display: "grid", gridTemplateColumns: gridColumns }}
        >
          <div className="px-2 py-3" />
          {DAY_NAMES_SHORT.map((day, i) => (
            <div
              key={i}
              className="px-3 py-3 text-center border-l border-gray-200"
            >
              <div className="text-xs font-bold text-gray-900 uppercase">
                {day}
              </div>
              <div className="text-[10px] text-gray-400">
                {DAY_NAMES_FULL[i]}
              </div>
            </div>
          ))}
        </div>

        {/* Grid body wrapper (for ghost overlay) */}
        <div className="relative">
          <div
            ref={gridBodyRef}
            className="relative"
            style={{ display: "grid", gridTemplateColumns: gridColumns }}
          >
            {/* Time axis */}
            <div className="relative" style={{ height: totalHeight }}>
              {HOURS.map((hour) => (
                <div
                  key={hour}
                  className="absolute left-0 right-0 flex items-start justify-end px-2"
                  style={{ top: (hour - START_HOUR) * ROW_HEIGHT }}
                >
                  <span className="relative -top-2 text-[10px] text-gray-400 font-medium">
                    {String(hour).padStart(2, "0")}:00
                  </span>
                </div>
              ))}
            </div>

            {/* Day columns */}
            {DAY_NAMES_SHORT.map((_, dayIndex) => {
              const daySlots = slots
                .filter((s) => s.dayOfWeek === dayIndex)
                .sort((a, b) => a.startTime.localeCompare(b.startTime));

              return (
                <div
                  key={dayIndex}
                  className="relative border-l border-gray-100 cursor-pointer group/day"
                  style={{ height: totalHeight }}
                  onClick={(e) => handleDayClick(dayIndex, e)}
                >
                  {/* Half-hour cells (hover targets) + gridlines */}
                  {HOURS.flatMap((hour) => {
                    const baseTop = (hour - START_HOUR) * ROW_HEIGHT;
                    const halfHeight = ROW_HEIGHT / 2;
                    return [
                      <div
                        key={`${hour}-0`}
                        className="absolute left-0 right-0 border-b border-dashed border-gray-200 hover:bg-tennis-green/5 transition-colors"
                        style={{ top: baseTop, height: halfHeight }}
                      />,
                      <div
                        key={`${hour}-30`}
                        className="absolute left-0 right-0 border-b border-gray-100 hover:bg-tennis-green/5 transition-colors"
                        style={{
                          top: baseTop + halfHeight,
                          height: halfHeight,
                        }}
                      />,
                    ];
                  })}

                  {/* Slot blocks + add-parallel buttons */}
                  {(() => {
                    const layout = layoutDaySlots(daySlots);

                    // Group slots into clusters to render one "+" button per cluster
                    const clusters: { slots: WizardSlot[]; top: number; height: number }[] = [];
                    const sorted = [...daySlots].sort((a, b) => a.startTime.localeCompare(b.startTime));
                    for (const slot of sorted) {
                      const pos = getSlotPosition(slot);
                      const last = clusters[clusters.length - 1];
                      if (last && pos.top < last.top + last.height) {
                        last.slots.push(slot);
                        const endY = pos.top + pos.height + SLOT_GAP;
                        last.height = Math.max(last.height, endY - last.top);
                      } else {
                        clusters.push({ slots: [slot], top: pos.top, height: pos.height + SLOT_GAP });
                      }
                    }

                    return (
                      <>
                        {daySlots.map((slot) => {
                          const pos = getSlotPosition(slot);
                          const isBeingDragged =
                            drag?.slotId === slot.id && isDragging;
                          const color = getTrainerColor(slot.trainerId);
                          const col = layout.get(slot.id) ?? { colIndex: 0, totalCols: 1 };
                          // Leave space for the "+" button
                          const addBtnWidth = 24; // px
                          const usableWidthCalc = `calc(100% - ${addBtnWidth + 2}px)`;

                          return (
                            <div
                              key={slot.id}
                              data-slot-id={slot.id}
                              className={`absolute border-l-[3px] rounded-r-md px-1.5 py-1 transition-all group z-10 ${
                                isBeingDragged
                                  ? "opacity-30 cursor-grabbing"
                                  : "cursor-grab"
                              }`}
                              style={{
                                top: pos.top,
                                height: pos.height,
                                left: `calc(${(col.colIndex / col.totalCols)} * ${usableWidthCalc} + 1px)`,
                                width: `calc(${(1 / col.totalCols)} * ${usableWidthCalc})`,
                                backgroundColor: color.bg,
                                borderLeftColor: color.border,
                              }}
                              onMouseDown={(e) => handleSlotMouseDown(slot, e)}
                              onClick={(e) => handleSlotClick(slot, e)}
                            >
                              {/* Delete button */}
                              <button
                                type="button"
                                data-slot-id={slot.id}
                                onClick={(e) => handleRemoveSlot(slot.id, e)}
                                className="absolute top-1 right-1 opacity-0 group-hover:opacity-100 w-4 h-4 flex items-center justify-center text-gray-400 hover:text-red-500 transition-all z-20"
                              >
                                <X size={10} />
                              </button>
                              <div
                                className="text-[11px] font-semibold"
                                style={{ color: color.text }}
                              >
                                {slot.startTime} — {slot.endTime}
                              </div>
                              {pos.height >= 36 && (
                                <div className="text-[10px] text-gray-600 truncate">
                                  {slot.courtName || slot.trainerName ? (
                                    <>
                                      {slot.courtName && <>{slot.courtName} · </>}
                                      {slot.trainerName && <>{slot.trainerName} · </>}
                                      <span className="text-gray-400">
                                        {slot.maxStudents} max
                                      </span>
                                    </>
                                  ) : (
                                    <span className="text-gray-400 italic">
                                      {slot.maxStudents} max
                                    </span>
                                  )}
                                </div>
                              )}
                            </div>
                          );
                        })}

                        {/* Add-parallel buttons — one per cluster, right edge */}
                        {clusters.map((cluster) => (
                          <button
                            key={`add-${cluster.slots[0].id}`}
                            type="button"
                            data-slot-id="add-parallel"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleAddParallelSlot(cluster.slots[0], e);
                            }}
                            className="absolute right-0 flex items-center justify-center rounded-md border border-dashed border-gray-200 bg-gray-50/80 text-gray-300 opacity-0 group-hover/day:opacity-100 hover:!border-tennis-green/40 hover:!text-tennis-green hover:!bg-tennis-green/5 transition-all z-10"
                            style={{
                              top: cluster.top,
                              height: cluster.height - SLOT_GAP,
                              width: 22,
                            }}
                          >
                            <Plus size={14} strokeWidth={2} />
                          </button>
                        ))}
                      </>
                    );
                  })()}
                </div>
              );
            })}
          </div>

          {/* Drag ghost preview */}
          {ghostStyle &&
            drag &&
            (() => {
              const draggedSlot = slots.find((s) => s.id === drag.slotId);
              const ghostColor = drag.isValid
                ? getTrainerColor(draggedSlot?.trainerId ?? null)
                : {
                    bg: "rgba(239,68,68,0.15)",
                    border: "#EF4444",
                    text: "#DC2626",
                  };

              return (
                <div
                  className="absolute pointer-events-none rounded-r-md border-l-[3px] px-2 py-1 z-30"
                  style={{
                    ...ghostStyle,
                    backgroundColor: ghostColor.bg,
                    borderLeftColor: ghostColor.border,
                  }}
                >
                  <div
                    className="text-[11px] font-semibold"
                    style={{ color: ghostColor.text }}
                  >
                    {formatTime(drag.previewStartMin)} —{" "}
                    {formatTime(drag.previewStartMin + drag.durationMin)}
                  </div>
                </div>
              );
            })()}
        </div>
      </div>

      {/* Hint */}
      <p className="text-xs text-gray-400 mt-2 px-1">{t("calendarHint")}</p>

      {/* Edit popover */}
      {editingSlotId &&
        editAnchor &&
        (() => {
          const editingSlot = slots.find((s) => s.id === editingSlotId);
          if (!editingSlot) return null;
          return (
            <SlotEditPopover
              slot={editingSlot}
              anchorRef={editAnchor}
              onSave={handleSlotSave}
              onClose={() => setEditingSlotId(null)}
            />
          );
        })()}
    </>
  );
}
