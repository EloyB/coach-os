"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  CalendarGrid,
  formatTime,
  parseTime,
  getSlotPosition,
  getTrainerColor,
  layoutDaySlots,
  START_HOUR,
  END_HOUR,
  ROW_HEIGHT,
  type CalendarSlot,
} from "@/components/calendar/calendar-grid";
import type { WizardSlot } from "../_types";
import { SlotEditPopover } from "./slot-edit-popover";

const DRAG_THRESHOLD = 5;

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
  const [ghostStyle, setGhostStyle] = useState<React.CSSProperties | null>(
    null
  );

  const slotsRef = useRef(slots);
  const onChangeRef = useRef(onChange);
  useEffect(() => {
    slotsRef.current = slots;
    onChangeRef.current = onChange;
    dragRef.current = drag;
  });

  // ─── Drag handlers ──────────────────────────────────────────────────────

  function handleSlotMouseDown(slot: CalendarSlot, e: React.MouseEvent) {
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

  const calcPosition = useCallback(
    (e: MouseEvent, currentDrag: DragState) => {
      const grid = gridBodyRef.current;
      if (!grid) return null;

      const gridRect = grid.getBoundingClientRect();
      const x = e.clientX - gridRect.left;
      const y = e.clientY - gridRect.top;

      const colWidth = (gridRect.width - 60) / 7;
      const dayIndex = Math.max(
        0,
        Math.min(6, Math.floor((x - 60) / colWidth))
      );

      const adjustedY = y - currentDrag.offsetY;
      const totalMin = START_HOUR * 60 + (adjustedY / ROW_HEIGHT) * 60;
      const snappedMin = Math.floor(totalMin / 30) * 30;
      const clampedStart = Math.max(
        START_HOUR * 60,
        Math.min(snappedMin, END_HOUR * 60 - currentDrag.durationMin)
      );

      return { previewDay: dayIndex, previewStartMin: clampedStart, isValid: true };
    },
    []
  );

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
              : s
          )
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

  // ─── Ghost position ─────────────────────────────────────────────────────

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

  // ─── Click handlers ─────────────────────────────────────────────────────

  function handleSlotRemove(id: string) {
    setEditingSlotId(null);
    onChange(slots.filter((s) => s.id !== id));
  }

  function handleSlotClick(slot: CalendarSlot, e: React.MouseEvent) {
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

  function handleAddParallelSlot(slot: CalendarSlot, e: React.MouseEvent) {
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

  function handleEmptyClick(dayIndex: number, e: React.MouseEvent<HTMLDivElement>) {
    if (justDraggedRef.current) return;
    if ((e.target as HTMLElement).closest("[data-slot-id]")) return;

    const rect = e.currentTarget.getBoundingClientRect();
    const y = e.clientY - rect.top;
    const totalMinutes = START_HOUR * 60 + (y / ROW_HEIGHT) * 60;
    const snappedMinutes = Math.floor(totalMinutes / 30) * 30;

    const clampedStart = Math.max(
      START_HOUR * 60,
      Math.min(snappedMinutes, (END_HOUR - 1) * 60)
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

  // ─── Render ─────────────────────────────────────────────────────────────

  const isDragging = drag?.moved ?? false;

  return (
    <>
      <CalendarGrid
        slots={slots}
        gridBodyRef={gridBodyRef}
        slotRightPadding={24}
        className={isDragging ? "cursor-grabbing" : ""}
        onSlotMouseDown={handleSlotMouseDown}
        onSlotClick={handleSlotClick}
        onSlotRemove={handleSlotRemove}
        renderDayOverlay={(dayIndex) => {
          // Add-parallel buttons for slot clusters
          const daySlots = slots
            .filter((s) => s.dayOfWeek === dayIndex)
            .sort((a, b) => a.startTime.localeCompare(b.startTime));
          const layout = layoutDaySlots(daySlots);

          // Compute clusters
          const clusters: { slots: CalendarSlot[]; top: number; height: number }[] = [];
          for (const slot of daySlots) {
            const pos = getSlotPosition(slot);
            const last = clusters[clusters.length - 1];
            if (last && pos.top < last.top + last.height) {
              last.slots.push(slot);
              const endY = pos.top + pos.height + 2;
              last.height = Math.max(last.height, endY - last.top);
            } else {
              clusters.push({ slots: [slot], top: pos.top, height: pos.height + 2 });
            }
          }

          return (
            <>
              {/* Click-to-add handler */}
              <div
                className="absolute inset-0 z-0"
                onClick={(e) => handleEmptyClick(dayIndex, e)}
              />

              {/* Dragged slot opacity */}
              {isDragging &&
                daySlots
                  .filter((s) => s.id === drag?.slotId)
                  .map((slot) => {
                    const pos = getSlotPosition(slot);
                    const col = layout.get(slot.id) ?? { colIndex: 0, totalCols: 1 };
                    const colWidthPct = 100 / col.totalCols;
                    return (
                      <div
                        key={`drag-overlay-${slot.id}`}
                        className="absolute bg-white/60 z-20 pointer-events-none"
                        style={{
                          top: pos.top,
                          height: pos.height,
                          left: `calc(${col.colIndex * colWidthPct}% + 1px)`,
                          width: `calc(${colWidthPct}% - 2px)`,
                        }}
                      />
                    );
                  })}

              {/* Add-parallel buttons */}
              {clusters.map((cluster) => (
                <button
                  key={`add-${cluster.slots[0].id}`}
                  type="button"
                  data-slot-id="add-parallel"
                  onClick={(e) => handleAddParallelSlot(cluster.slots[0], e)}
                  className="absolute right-0 flex items-center justify-center rounded-md border border-dashed border-gray-200 bg-gray-50/80 text-gray-300 opacity-0 group-hover/day:opacity-100 hover:!border-tennis-green/40 hover:!text-tennis-green hover:!bg-tennis-green/5 transition-all z-10"
                  style={{
                    top: cluster.top,
                    height: cluster.height - 2,
                    width: 22,
                  }}
                >
                  <Plus size={14} strokeWidth={2} />
                </button>
              ))}
            </>
          );
        }}
        renderGridOverlay={() => {
          if (!ghostStyle || !drag) return null;

          const draggedSlot = slots.find((s) => s.id === drag.slotId);
          const ghostColor = getTrainerColor(draggedSlot?.trainerId ?? null);

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
        }}
      />

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
