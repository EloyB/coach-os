"use client";

import { useState, useEffect, useRef } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { ChevronLeft } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import type { Step1Data, WizardSlot, WeekOverride } from "./_types";
import { StepIndicator } from "./_components/step-indicator";
import { Step1Basisinfo } from "./_components/step-1-basisinfo";
import { Step2Planning } from "./_components/step-2-planning";
import { Step3Validation } from "./_components/step-3-validation";

export default function NewLessonSeriesPage() {
  const t = useTranslations("lessonWizard");
  const router = useRouter();

  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [step1Data, setStep1Data] = useState<Step1Data | null>(null);
  const [weeklyTemplate, setWeeklyTemplate] = useState<WizardSlot[]>([]);
  const [weekOverrides, setWeekOverrides] = useState<WeekOverride[]>([]);
  const [showLeaveDialog, setShowLeaveDialog] = useState(false);
  const [pendingHref, setPendingHref] = useState<string | null>(null);
  const [isDirty, setIsDirty] = useState(false);

  const hasDataRef = useRef(false);
  const hasData = isDirty || step1Data !== null || weeklyTemplate.length > 0;
  useEffect(() => {
    hasDataRef.current = hasData;
  }, [hasData]);

  // Browser refresh / tab close
  useEffect(() => {
    if (!hasData) return;

    function handleBeforeUnload(e: BeforeUnloadEvent) {
      e.preventDefault();
    }

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [hasData]);

  // Intercept all client-side link clicks that navigate away from the wizard
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (!hasDataRef.current) return;

      const anchor = (e.target as HTMLElement).closest("a");
      if (!anchor) return;

      const href = anchor.getAttribute("href");
      if (!href || href.startsWith("/dashboard/lessons/new")) return;

      e.preventDefault();
      e.stopPropagation();
      setPendingHref(href);
      setShowLeaveDialog(true);
    }

    document.addEventListener("click", handleClick, true);
    return () => document.removeEventListener("click", handleClick, true);
  }, []);

  // Browser back button
  useEffect(() => {
    if (!hasData) return;

    // Push a dummy state so we can intercept the back button
    window.history.pushState({ wizardGuard: true }, "");

    function handlePopState() {
      setShowLeaveDialog(true);
      // Re-push so the user stays on the page until they confirm
      window.history.pushState({ wizardGuard: true }, "");
    }

    window.addEventListener("popstate", handlePopState);
    return () => window.removeEventListener("popstate", handlePopState);
  }, [hasData]);

  function handleConfirmLeave() {
    setShowLeaveDialog(false);
    // Reset dirty state so guards don't re-trigger
    setIsDirty(false);
    setStep1Data(null);
    setWeeklyTemplate([]);

    if (pendingHref) {
      router.push(pendingHref);
      setPendingHref(null);
    } else {
      router.push("/dashboard/lessons");
    }
  }

  function handleStep1Next(data: Step1Data) {
    setStep1Data(data);
    setStep(2);
  }

  function handleStep2Next(slots: WizardSlot[]) {
    setWeeklyTemplate(slots);
    setStep(3);
  }

  return (
    <div
      className={
        step === 1
          ? "max-w-lg mx-auto"
          : step === 2
            ? "w-full mx-auto"
            : "max-w-5xl mx-auto"
      }
      onChangeCapture={() => setIsDirty(true)}
    >
      {/* Back */}
      <Link
        href="/dashboard/lessons"
        className="inline-flex items-center gap-1.5 text-sm text-gray-400 hover:text-gray-600 transition-colors mb-6"
      >
        <ChevronLeft size={15} />
        {t("backToLessons")}
      </Link>

      {/* Page title */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">
          {t("pageTitle")}
        </h1>
        <p className="text-gray-400 text-sm mt-1">{t("pageSubtitle")}</p>
      </div>

      {/* Step indicator */}
      <StepIndicator currentStep={step} />

      {/* Steps */}
      {step === 1 && (
        <Step1Basisinfo defaultValues={step1Data} onNext={handleStep1Next} />
      )}

      {step === 2 && (
        <Step2Planning
          slots={weeklyTemplate}
          tennisClubId={step1Data?.tennisClubId ?? ""}
          onNext={handleStep2Next}
          onBack={() => setStep(1)}
        />
      )}

      {step === 3 && step1Data && (
        <Step3Validation
          step1Data={step1Data}
          weeklyTemplate={weeklyTemplate}
          weekOverrides={weekOverrides}
          onOverridesChange={setWeekOverrides}
          onBack={() => setStep(2)}
        />
      )}

      {/* Leave confirmation dialog */}
      <Dialog
        open={showLeaveDialog}
        onOpenChange={(open) => {
          if (!open) {
            setShowLeaveDialog(false);
            setPendingHref(null);
          }
        }}
      >
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>{t("leaveTitle")}</DialogTitle>
            <DialogDescription>{t("leaveDesc")}</DialogDescription>
          </DialogHeader>
          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={() => {
                setShowLeaveDialog(false);
                setPendingHref(null);
              }}
              className="flex-1 px-4 py-2 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
            >
              {t("cancel")}
            </button>
            <button
              type="button"
              onClick={handleConfirmLeave}
              className="flex-1 px-4 py-2 text-sm font-semibold text-white bg-red-500 rounded-lg hover:bg-red-600 transition-colors"
            >
              {t("leaveConfirm")}
            </button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
