"use client";

import { useState } from "react";
import { Plus, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { LESSON_LEVELS } from "@/lib/api/lessonSeries";
import type { TrainerDto } from "@/lib/api/trainers";
import { formatDateShort } from "@/lib/date-utils";
import type { WizardSlot } from "../_types";
import { SlotDialog } from "./slot-dialog";

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

function SlotCard({
  slot,
  variant,
  onRemove,
}: {
  slot: WizardSlot;
  variant: "grid" | "list";
  onRemove: () => void;
}) {
  if (variant === "list") {
    return (
      <div className="flex items-center gap-3 bg-tennis-green/5 border border-tennis-green/15 rounded-lg px-3 py-2 group">
        <p className="text-xs font-semibold text-gray-800 shrink-0">
          {slot.startTime}–{slot.endTime}
        </p>
        <span className="text-[11px] text-gray-400">·</span>
        <p className="text-xs text-gray-600 truncate">{slot.trainerName}</p>
        <span className="text-[11px] text-gray-400">·</span>
        <p className="text-xs text-gray-500 truncate">{slot.courtName}</p>
        <div className="flex items-center gap-1.5 ml-auto shrink-0">
          <span className="bg-gray-100 text-gray-500 rounded px-1.5 py-0.5 text-[10px] leading-none">
            {slot.maxStudents} max
          </span>
          {slot.level !== null && (
            <span className="bg-tennis-lime/30 text-tennis-green rounded px-1.5 py-0.5 text-[10px] leading-none">
              {LESSON_LEVELS[slot.level]}
            </span>
          )}
          <button
            type="button"
            onClick={onRemove}
            className="opacity-0 group-hover:opacity-100 w-5 h-5 flex items-center justify-center text-gray-400 hover:text-red-500 transition-all"
          >
            <X size={12} />
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-tennis-green/5 border border-tennis-green/15 rounded-lg p-2 relative group">
      <button
        type="button"
        onClick={onRemove}
        className="absolute top-1 right-1 opacity-0 group-hover:opacity-100 w-4 h-4 flex items-center justify-center text-gray-400 hover:text-red-500 transition-all"
      >
        <X size={11} />
      </button>

      <p className="text-xs font-semibold text-gray-800 pr-4">
        {slot.startTime}–{slot.endTime}
      </p>
      <p className="text-[11px] text-gray-500 truncate mt-0.5">
        {slot.trainerName}
      </p>
      <p className="text-[11px] text-gray-400 truncate">{slot.courtName}</p>
      <div className="flex flex-wrap gap-1 mt-1.5">
        <span className="bg-gray-100 text-gray-500 rounded px-1 py-0.5 text-[10px] leading-none">
          {slot.maxStudents} max
        </span>
        {slot.level !== null && (
          <span className="bg-tennis-lime/30 text-tennis-green rounded px-1 py-0.5 text-[10px] leading-none">
            {LESSON_LEVELS[slot.level]}
          </span>
        )}
      </div>
    </div>
  );
}

interface WeekTemplateBuilderProps {
  slots: WizardSlot[];
  onChange: (slots: WizardSlot[]) => void;
  trainers: TrainerDto[];
  trainersLoading?: boolean;
  variant?: "grid" | "list";
  dayDates?: string[]; // 7 ISO dates (Mon–Sun) to show actual dates in list variant
  seriesStartDate?: string; // ISO date — days before this are disabled
  seriesEndDate?: string; // ISO date — days after this are disabled
  removeOnly?: boolean; // hide "Slot toevoegen" buttons, only allow removing
}

export function WeekTemplateBuilder({
  slots,
  onChange,
  trainers,
  trainersLoading,
  variant = "grid",
  dayDates,
  seriesStartDate,
  seriesEndDate,
  removeOnly,
}: WeekTemplateBuilderProps) {
  const t = useTranslations("lessonWizard");
  const [dialogDay, setDialogDay] = useState<number | null>(null);

  function handleAddSlot(slot: Omit<WizardSlot, "id">) {
    onChange([...slots, { ...slot, id: crypto.randomUUID() }]);
  }

  function handleRemoveSlot(id: string) {
    onChange(slots.filter((s) => s.id !== id));
  }

  if (variant === "list") {
    return (
      <>
        <div className="divide-y divide-gray-100">
          {DAY_NAMES_FULL.map((day, dayIndex) => {
            const dayDate = dayDates?.[dayIndex];
            const isInRange =
              !dayDate ||
              !seriesStartDate ||
              !seriesEndDate ||
              (dayDate >= seriesStartDate && dayDate <= seriesEndDate);

            const daySlots = slots
              .filter((s) => s.dayOfWeek === dayIndex)
              .sort((a, b) => a.startTime.localeCompare(b.startTime));

            return (
              <div
                key={dayIndex}
                className={`py-3 first:pt-0 last:pb-0 ${!isInRange ? "opacity-40" : ""}`}
              >
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-gray-700">
                    {day}
                    {dayDate && (
                      <span className="text-gray-400 font-normal ml-1.5">
                        {formatDateShort(dayDate)}
                      </span>
                    )}
                  </span>
                  {isInRange && !removeOnly && (
                    <button
                      type="button"
                      onClick={() => setDialogDay(dayIndex)}
                      disabled={trainersLoading}
                      className="flex items-center gap-1 text-xs text-gray-400 hover:text-tennis-green transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      <Plus size={12} />
                      {t("addSlot")}
                    </button>
                  )}
                </div>

                {isInRange && daySlots.length > 0 ? (
                  <div className="flex flex-col gap-1.5">
                    {daySlots.map((slot) => (
                      <SlotCard
                        key={slot.id}
                        slot={slot}
                        variant="list"
                        onRemove={() => handleRemoveSlot(slot.id)}
                      />
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-gray-300 italic">—</p>
                )}
              </div>
            );
          })}
        </div>

        {dialogDay !== null && (
          <SlotDialog
            open
            dayOfWeek={dialogDay}
            trainers={trainers}
            onSave={handleAddSlot}
            onClose={() => setDialogDay(null)}
          />
        )}
      </>
    );
  }

  return (
    <>
      <div className="overflow-x-auto">
        <div className="grid grid-cols-7 gap-2 min-w-[640px]">
          {DAY_NAMES_SHORT.map((day, dayIndex) => {
            const daySlots = slots
              .filter((s) => s.dayOfWeek === dayIndex)
              .sort((a, b) => a.startTime.localeCompare(b.startTime));

            return (
              <div key={dayIndex} className="flex flex-col gap-2">
                {/* Day header */}
                <div className="text-center py-2 bg-gray-50 rounded-lg">
                  <span className="text-xs font-semibold text-gray-600">
                    {day}
                  </span>
                </div>

                {/* Slot cards */}
                {daySlots.map((slot) => (
                  <SlotCard
                    key={slot.id}
                    slot={slot}
                    variant="grid"
                    onRemove={() => handleRemoveSlot(slot.id)}
                  />
                ))}

                {/* Add button */}
                {!removeOnly && (
                  <button
                    type="button"
                    onClick={() => setDialogDay(dayIndex)}
                    disabled={trainersLoading}
                    className="flex items-center justify-center gap-1 py-2.5 rounded-lg border border-dashed border-gray-200 text-xs text-gray-400 hover:border-tennis-green/40 hover:text-tennis-green hover:bg-tennis-green/5 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    <Plus size={12} />
                    <span>{t("addSlot")}</span>
                  </button>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {dialogDay !== null && (
        <SlotDialog
          open
          dayOfWeek={dialogDay}
          trainers={trainers}
          onSave={handleAddSlot}
          onClose={() => setDialogDay(null)}
        />
      )}
    </>
  );
}
