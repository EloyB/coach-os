"use client";

import { useState, useEffect } from "react";
import { useTranslations } from "next-intl";
import { useQuery } from "@tanstack/react-query";
import {
  Popover,
  PopoverContent,
  PopoverAnchor,
} from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { LESSON_LEVELS } from "@/lib/api/lessonSeries";
import { getTrainers } from "@/lib/api/trainers";
import { inputClass } from "@/lib/styles";
import type { WizardSlot } from "../_types";

interface SlotEditPopoverProps {
  slot: WizardSlot;
  anchorRef: HTMLElement;
  onSave: (updated: WizardSlot) => void;
  onClose: () => void;
}

export function SlotEditPopover({
  slot,
  anchorRef,
  onSave,
  onClose,
}: SlotEditPopoverProps) {
  const t = useTranslations("lessonWizard");

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  const [trainerId, setTrainerId] = useState(slot.trainerId ?? "none");
  const [startTime, setStartTime] = useState(slot.startTime);
  const [endTime, setEndTime] = useState(slot.endTime);
  const [courtName, setCourtName] = useState(slot.courtName ?? "");
  const [maxStudents, setMaxStudents] = useState(slot.maxStudents);
  const [level, setLevel] = useState(
    slot.level !== null ? String(slot.level) : "none"
  );

  // Reset form when slot changes
  useEffect(() => {
    setTrainerId(slot.trainerId ?? "none");
    setStartTime(slot.startTime);
    setEndTime(slot.endTime);
    setCourtName(slot.courtName ?? "");
    setMaxStudents(slot.maxStudents);
    setLevel(slot.level !== null ? String(slot.level) : "none");
  }, [slot.id]);

  function handleSave() {
    const trainer = trainers.find((tr) => tr.id === trainerId);
    onSave({
      ...slot,
      startTime,
      endTime,
      trainerId: trainerId === "none" ? null : trainerId,
      trainerName:
        trainerId === "none" || !trainer
          ? null
          : `${trainer.firstName} ${trainer.lastName}`,
      courtName: courtName || null,
      maxStudents,
      level: level === "none" ? null : parseInt(level),
    });
  }

  return (
    <Popover open onOpenChange={(open) => !open && onClose()}>
      <PopoverAnchor virtualRef={{ current: anchorRef }} />
      <PopoverContent
        side="right"
        align="start"
        sideOffset={8}
        className="w-64 p-4 space-y-3"
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <p className="text-xs font-semibold text-gray-900 mb-2">
          {t("editSlotTitle")}
        </p>

        {/* Start / End time */}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-[11px] font-medium text-gray-500 mb-1">
              {t("startTime")}
            </label>
            <input
              type="time"
              value={startTime}
              onChange={(e) => setStartTime(e.target.value)}
              className={inputClass + " !h-7 !text-xs !px-2"}
            />
          </div>
          <div>
            <label className="block text-[11px] font-medium text-gray-500 mb-1">
              {t("endTime")}
            </label>
            <input
              type="time"
              value={endTime}
              onChange={(e) => setEndTime(e.target.value)}
              className={inputClass + " !h-7 !text-xs !px-2"}
            />
          </div>
        </div>

        {/* Trainer */}
        <div>
          <label className="block text-[11px] font-medium text-gray-500 mb-1">
            {t("trainer")}
          </label>
          <Select onValueChange={setTrainerId} value={trainerId}>
            <SelectTrigger className="border border-gray-200 rounded-lg h-7 text-xs">
              <SelectValue placeholder={t("trainerPlaceholder")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="none">
                <span className="text-gray-400">{t("nonePicker")}</span>
              </SelectItem>
              {trainers.map((tr) => (
                <SelectItem key={tr.id} value={tr.id}>
                  {tr.firstName} {tr.lastName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Court name */}
        <div>
          <label className="block text-[11px] font-medium text-gray-500 mb-1">
            {t("courtName")}
          </label>
          <input
            type="text"
            value={courtName}
            onChange={(e) => setCourtName(e.target.value)}
            placeholder={t("courtNamePlaceholder")}
            className={inputClass + " !h-7 !text-xs !px-2"}
          />
        </div>

        {/* Max students + Level */}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-[11px] font-medium text-gray-500 mb-1">
              {t("maxStudents")}
            </label>
            <input
              type="number"
              min={1}
              value={maxStudents}
              onChange={(e) => setMaxStudents(parseInt(e.target.value) || 1)}
              className={inputClass + " !h-7 !text-xs !px-2"}
            />
          </div>
          <div>
            <label className="block text-[11px] font-medium text-gray-500 mb-1">
              {t("level")}
            </label>
            <Select onValueChange={setLevel} value={level}>
              <SelectTrigger className="border border-gray-200 rounded-lg h-7 text-xs">
                <SelectValue placeholder={t("levelNone")} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">
                  <span className="text-gray-400">{t("levelNone")}</span>
                </SelectItem>
                {Object.entries(LESSON_LEVELS).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Save button */}
        <button
          type="button"
          onClick={handleSave}
          className="w-full px-3 py-1.5 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
        >
          {t("saveSlot")}
        </button>
      </PopoverContent>
    </Popover>
  );
}
