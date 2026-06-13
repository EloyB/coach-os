"use client";

import { useState } from "react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import { ChevronLeft } from "lucide-react";
import { CampStepIndicator } from "./_components/camp-step-indicator";
import { Step1Basis } from "./_components/step-1-basis";
import { Step2Days } from "./_components/step-2-days";
import { Step3Review } from "./_components/step-3-review";
import type { CampStep1Data, CampDayDraft } from "./_types";

export default function NewCampPage() {
  const t = useTranslations("camps");

  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [step1Data, setStep1Data] = useState<CampStep1Data | null>(null);
  const [days, setDays] = useState<CampDayDraft[]>([]);

  function handleStep1Next(data: CampStep1Data) {
    setStep1Data(data);
    setStep(2);
  }

  return (
    <div className="max-w-2xl mx-auto">
      {/* Back */}
      <Link
        href="/dashboard/camps"
        className="inline-flex items-center gap-1.5 text-sm text-gray-400 hover:text-gray-600 transition-colors mb-6"
      >
        <ChevronLeft size={15} />
        {t("back")}
      </Link>

      {/* Page title */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          {t("createTitle")}
        </h1>
        <p className="text-gray-400 text-sm mt-1">{t("createSubtitle")}</p>
      </div>

      {/* Step indicator */}
      <CampStepIndicator currentStep={step} />

      {/* Steps */}
      {step === 1 && (
        <Step1Basis defaultValues={step1Data} onNext={handleStep1Next} />
      )}

      {step === 2 && step1Data && (
        <Step2Days
          step1Data={step1Data}
          days={days}
          onChange={setDays}
          onBack={() => setStep(1)}
          onNext={() => setStep(3)}
        />
      )}

      {step === 3 && step1Data && (
        <Step3Review
          step1Data={step1Data}
          days={days}
          onBack={() => setStep(2)}
        />
      )}
    </div>
  );
}
