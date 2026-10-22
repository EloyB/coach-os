"use client";

import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { getTrainers } from "@/lib/api/trainers";
import { CampDaysEditor } from "./camp-days-editor";
import {
  buildDays,
  hasOverlappingShifts,
  type CampDayDraft,
  type CampStep1Data,
  type DateRange,
} from "../_types";

interface Step2Props {
  step1Data: CampStep1Data;
  days: CampDayDraft[];
  onChange: (days: CampDayDraft[]) => void;
  daysRange: DateRange | null;
  onRangeChange: (range: DateRange) => void;
  onBack: () => void;
  onNext: () => void;
}

export function Step2Days({
  step1Data,
  days,
  onChange,
  daysRange,
  onRangeChange,
  onBack,
  onNext,
}: Step2Props) {
  const t = useTranslations("camps");

  const { data: trainers = [] } = useQuery({
    queryKey: ["trainers"],
    queryFn: getTrainers,
  });

  // Generate (or re-merge) the day list whenever the camp date range is known.
  useEffect(() => {
    onChange(
      buildDays(step1Data.startDate, step1Data.endDate, days, daysRange),
    );
    onRangeChange({ start: step1Data.startDate, end: step1Data.endDate });
    // `days` is intentionally omitted from the deps: it is the parent state we
    // are writing into, read at call-time only to preserve existing day data.
    // Including it would re-run on every onChange and loop indefinitely.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step1Data.startDate, step1Data.endDate]);

  const hasOverlap = hasOverlappingShifts(days);

  return (
    <div>
      <CampDaysEditor
        days={days}
        onChange={onChange}
        trainers={trainers}
        rangeStart={step1Data.startDate}
        rangeEnd={step1Data.endDate}
      />

      {hasOverlap && (
        <p className="mt-3 text-xs font-medium text-red-600">
          {t("shiftsOverlapBlock")}
        </p>
      )}

      <div className="flex items-center justify-between mt-5">
        <button
          type="button"
          onClick={onBack}
          className="flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          <ChevronLeft size={15} />
          {t("wizardBack")}
        </button>
        <button
          type="button"
          onClick={onNext}
          disabled={hasOverlap}
          className="flex items-center gap-2 px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {t("next")}
          <ChevronRight size={15} />
        </button>
      </div>
    </div>
  );
}
