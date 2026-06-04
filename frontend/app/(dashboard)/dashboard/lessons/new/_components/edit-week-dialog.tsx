"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import type { TrainerDto } from "@/lib/api/trainers";
import { formatDateShort } from "@/lib/date-utils";
import { toLocalISODate, type WizardSlot } from "../_types";
import { WeekTemplateBuilder } from "./week-template-builder";

interface EditWeekDialogProps {
  weekIndex: number;
  weekStartDate: string;
  weekEndDate: string;
  currentSlots: WizardSlot[];
  trainers: TrainerDto[];
  seriesStartDate: string;
  seriesEndDate: string;
  onSave: (slots: WizardSlot[]) => void;
  onClose: () => void;
}

export function EditWeekDialog({
  weekIndex,
  weekStartDate,
  weekEndDate,
  currentSlots,
  trainers,
  seriesStartDate,
  seriesEndDate,
  onSave,
  onClose,
}: EditWeekDialogProps) {
  const t = useTranslations("lessonWizard");

  const dayDates = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(weekStartDate + "T00:00:00");
    d.setDate(d.getDate() + i);
    return toLocalISODate(d);
  });

  // Filter out slots for days outside the series range
  const filteredSlots = currentSlots.filter((s) => {
    const dayDate = dayDates[s.dayOfWeek];
    return dayDate >= seriesStartDate && dayDate <= seriesEndDate;
  });

  const [slots, setSlots] = useState<WizardSlot[]>(filteredSlots);

  return (
    <Dialog open onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="max-w-4xl" aria-describedby={undefined}>
        <DialogHeader>
          <DialogTitle>
            {t("adjustWeekDialogTitle")} — {t("weekLabel")} {weekIndex + 1} (
            {formatDateShort(weekStartDate)} – {formatDateShort(weekEndDate)})
          </DialogTitle>
        </DialogHeader>

        <DialogBody className="mt-2">
          <WeekTemplateBuilder
            slots={slots}
            onChange={setSlots}
            trainers={trainers}
            variant="list"
            dayDates={dayDates}
            seriesStartDate={seriesStartDate}
            seriesEndDate={seriesEndDate}
            removeOnly
          />
        </DialogBody>

        <DialogFooter className="gap-3 border-t border-gray-100 pt-4 sm:justify-stretch">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 px-4 py-2 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            {t("cancel")}
          </button>
          <button
            type="button"
            onClick={() => {
              onSave(slots);
              onClose();
            }}
            className="flex-1 px-4 py-2 text-sm font-semibold text-white bg-tennis-green rounded-lg hover:bg-tennis-green/90 transition-colors"
          >
            {t("saveWeek")}
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
