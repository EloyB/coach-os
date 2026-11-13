"use client";

import { useTranslations } from "next-intl";
import { AvailabilitySection } from "./_components/availability-section";

export default function TrainerSettingsPage() {
  const t = useTranslations("trainerSettings");

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          {t("title")}
        </h1>
        <p className="text-gray-400 text-sm mt-1">{t("subtitle")}</p>
      </div>

      <AvailabilitySection />
    </div>
  );
}
