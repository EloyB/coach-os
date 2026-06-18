"use client";

import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { getTrainers } from "@/lib/api/trainers";
import { CampDaysEditor } from "./camp-days-editor";
import { buildDays, type CampDayDraft, type CampStep1Data } from "../_types";

interface Step2Props {
  step1Data: CampStep1Data;
  days: CampDayDraft[];
  onChange: (days: CampDayDraft[]) => void;
  onBack: () => void;
  onNext: () => void;
}

export function Step2Days({
  step1Data,
  days,
  onChange,
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
    onChange(buildDays(step1Data.startDate, step1Data.endDate, days));
    // `days` is intentionally omitted from the deps: it is the parent state we
    // are writing into, read at call-time only to preserve existing day data.
    // Including it would re-run on every onChange and loop indefinitely.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step1Data.startDate, step1Data.endDate]);

  return (
    <div>
      <CampDaysEditor days={days} onChange={onChange} trainers={trainers} />

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
          className="flex items-center gap-2 px-5 py-2.5 bg-tennis-green text-white text-sm font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors"
        >
          {t("next")}
          <ChevronRight size={15} />
        </button>
      </div>
    </div>
  );
}
