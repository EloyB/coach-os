"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Lightbulb, Plus } from "lucide-react";
import { getSlotSuggestions } from "@/lib/api/slotSuggestions";
import type { SlotSuggestionDto } from "@/lib/api/slotSuggestions";
import type { WizardSlot } from "../_types";
import type { SlotDefaults } from "./calendar-week-view";

const DAY_NAMES_FULL = [
  "Maandag",
  "Dinsdag",
  "Woensdag",
  "Donderdag",
  "Vrijdag",
  "Zaterdag",
  "Zondag",
];

interface SlotSuggestionsPanelProps {
  tennisClubId: string;
  defaults: SlotDefaults;
  onApply: (newSlots: WizardSlot[]) => void;
}

/**
 * Leidt tijdvensters af uit de vastgelegde trainerbeschikbaarheid. Per venster
 * geeft de backend terug hoeveel trainers gelijktijdig vrij zijn — dat is het
 * aantal banen dat parallel gepland kan worden.
 */
export function SlotSuggestionsPanel({
  tennisClubId,
  defaults,
  onApply,
}: SlotSuggestionsPanelProps) {
  const t = useTranslations("lessonWizard");
  const [open, setOpen] = useState(false);

  const {
    data: suggestions = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["slotSuggestions", tennisClubId],
    queryFn: () => getSlotSuggestions(tennisClubId),
    enabled: open && Boolean(tennisClubId),
  });

  function handleApply(suggestion: SlotSuggestionDto) {
    const newSlots: WizardSlot[] = Array.from(
      { length: suggestion.suggestedParallelSlots },
      (_, index) => {
        const trainer = suggestion.trainers[index] ?? null;
        return {
          id: crypto.randomUUID(),
          dayOfWeek: suggestion.dayOfWeek,
          startTime: suggestion.startTime,
          endTime: suggestion.endTime,
          trainerId: trainer?.id ?? null,
          trainerName: trainer?.name ?? null,
          courtName: `${t("courtPrefix")} ${index + 1}`,
          maxStudents: defaults.maxStudents,
          level: defaults.level,
        };
      }
    );
    onApply(newSlots);
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center gap-2 px-4 py-3 text-left"
      >
        <Lightbulb size={15} className="text-tennis-green shrink-0" />
        <span className="text-sm font-semibold text-gray-900">
          {t("suggestionsTitle")}
        </span>
      </button>

      {open && (
        <div className="px-4 pb-4 space-y-2">
          <p className="text-[11px] text-gray-400 leading-snug">
            {t("suggestionsDesc")}
          </p>

          {isLoading && (
            <p className="text-xs text-gray-400">{t("suggestionsLoading")}</p>
          )}
          {isError && (
            <p className="text-xs text-amber-600">{t("suggestionsError")}</p>
          )}
          {!isLoading && !isError && suggestions.length === 0 && (
            <p className="text-xs text-gray-400">{t("suggestionsEmpty")}</p>
          )}

          {suggestions.map((s) => (
            <div
              key={`${s.dayOfWeek}-${s.startTime}-${s.endTime}`}
              className="flex items-center justify-between gap-3 rounded-lg border border-gray-200 px-3 py-2"
            >
              <div className="min-w-0">
                <p className="text-xs font-medium text-gray-900">
                  {DAY_NAMES_FULL[s.dayOfWeek]} {s.startTime}–{s.endTime}
                </p>
                <p className="text-[11px] text-gray-400 truncate">
                  {t("suggestionsParallel", {
                    count: s.suggestedParallelSlots,
                  })}
                  {" · "}
                  {s.trainers.map((tr) => tr.name).join(", ")}
                </p>
              </div>
              <button
                type="button"
                onClick={() => handleApply(s)}
                className="flex items-center gap-1 shrink-0 px-2.5 py-1.5 bg-tennis-green text-white text-[11px] font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
              >
                <Plus size={12} strokeWidth={2.5} />
                {t("suggestionsApply")}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
