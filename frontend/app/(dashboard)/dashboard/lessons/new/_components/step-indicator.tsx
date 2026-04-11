"use client";

import { Check } from "lucide-react";
import { useTranslations } from "next-intl";

export function StepIndicator({ currentStep }: { currentStep: 1 | 2 | 3 }) {
  const t = useTranslations("lessonWizard");

  const steps = [
    { number: 1 as const, label: t("step1Label") },
    { number: 2 as const, label: t("step2Label") },
    { number: 3 as const, label: t("step3Label") },
  ];

  return (
    <div className="flex items-center mb-8">
      {steps.map((step, i) => (
        <div key={step.number} className="flex items-center">
          <div className="flex items-center gap-2">
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-semibold transition-colors shrink-0 ${
                step.number < currentStep
                  ? "bg-tennis-green text-white"
                  : step.number === currentStep
                    ? "bg-tennis-green text-white ring-4 ring-tennis-green/20"
                    : "bg-gray-100 text-gray-400"
              }`}
            >
              {step.number < currentStep ? <Check size={14} /> : step.number}
            </div>
            <span
              className={`text-sm font-medium whitespace-nowrap ${
                step.number <= currentStep ? "text-gray-900" : "text-gray-400"
              }`}
            >
              {step.label}
            </span>
          </div>
          {i < steps.length - 1 && (
            <div
              className={`h-px w-10 mx-4 shrink-0 ${
                step.number < currentStep ? "bg-tennis-green" : "bg-gray-200"
              }`}
            />
          )}
        </div>
      ))}
    </div>
  );
}
