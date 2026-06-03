"use client";

import { useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ArrowRight, Check, PartyPopper } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  dismissOnboarding,
  getOnboardingState,
  type OnboardingStepKey,
} from "@/lib/api/onboarding";
import { TrainerModeDialog } from "./trainer-mode-dialog";

interface OnboardingChecklistProps {
  enabled: boolean;
}

const STEP_ORDER: OnboardingStepKey[] = ["mollie", "club", "trainerMode", "series"];

function stepHref(key: OnboardingStepKey): string | null {
  switch (key) {
    case "mollie":
      return "/dashboard/settings";
    case "club":
      return "/dashboard/settings";
    case "series":
      return "/dashboard/lessons/new";
    case "trainerMode":
      return null;
  }
}

export function OnboardingChecklist({ enabled }: OnboardingChecklistProps) {
  const t = useTranslations("onboarding");
  const queryClient = useQueryClient();
  const [trainerDialogOpen, setTrainerDialogOpen] = useState(false);

  const { data } = useQuery({
    queryKey: ["onboarding", "state"],
    queryFn: getOnboardingState,
    enabled,
    staleTime: 30_000,
  });

  const dismissMutation = useMutation({
    mutationFn: dismissOnboarding,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["onboarding"] });
    },
  });

  if (!enabled || !data?.shouldShow) {
    return (
      <>
        <TrainerModeDialog
          open={trainerDialogOpen}
          onOpenChange={setTrainerDialogOpen}
        />
      </>
    );
  }

  const stepMap = new Map(data.steps.map((s) => [s.key, s.completed]));
  const done = data.steps.filter((s) => s.completed).length;
  const total = data.steps.length;
  const progressPct = total === 0 ? 0 : Math.round((done / total) * 100);

  if (data.allCompleted) {
    return (
      <>
        <Card className="mb-3.5 border-tennis-green/20">
          <CardHeader className="border-b pb-4">
            <CardTitle className="flex items-center gap-2 text-base">
              <PartyPopper className="size-5 text-tennis-green" />
              {t("celebrationTitle")}
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col items-start gap-3">
            <p className="text-sm text-muted-foreground">
              {t("celebrationBody")}
            </p>
            <Button
              onClick={() => dismissMutation.mutate()}
              disabled={dismissMutation.isPending}
            >
              {t("celebrationCta")}
            </Button>
          </CardContent>
        </Card>
        <TrainerModeDialog
          open={trainerDialogOpen}
          onOpenChange={setTrainerDialogOpen}
        />
      </>
    );
  }

  return (
    <>
      <Card className="mb-3.5">
        <CardHeader className="border-b pb-4">
          <div className="flex items-start justify-between gap-3">
            <div>
              <CardTitle className="text-base">{t("checklistTitle")}</CardTitle>
              <p className="text-xs text-muted-foreground mt-1">
                {t("checklistSubtitle", { done, total })}
              </p>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => dismissMutation.mutate()}
              disabled={dismissMutation.isPending}
            >
              {t("dismiss")}
            </Button>
          </div>
          <div className="mt-3 h-1.5 w-full rounded-full bg-muted overflow-hidden">
            <div
              className="h-full bg-tennis-green transition-all"
              style={{ width: `${progressPct}%` }}
            />
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          {STEP_ORDER.map((key) => (
            <StepRow
              key={key}
              stepKey={key}
              completed={stepMap.get(key) ?? false}
              onTrainerModeClick={() => setTrainerDialogOpen(true)}
            />
          ))}
        </CardContent>
      </Card>
      <TrainerModeDialog
        open={trainerDialogOpen}
        onOpenChange={setTrainerDialogOpen}
      />
    </>
  );
}

interface StepRowProps {
  stepKey: OnboardingStepKey;
  completed: boolean;
  onTrainerModeClick: () => void;
}

function StepRow({ stepKey, completed, onTrainerModeClick }: StepRowProps) {
  const t = useTranslations("onboarding");
  const tStep = useTranslations(`onboarding.step.${stepKey}`);
  const href = stepHref(stepKey);

  return (
    <div
      className={`flex items-center justify-between gap-3 rounded-lg border px-4 py-3 ${
        completed ? "border-tennis-green/30 bg-tennis-green/5" : "border-rule"
      }`}
    >
      <div className="flex items-center gap-3 min-w-0">
        <div
          className={`flex size-6 items-center justify-center rounded-full ${
            completed
              ? "bg-tennis-green text-white"
              : "bg-muted text-muted-foreground"
          }`}
          aria-hidden
        >
          {completed ? <Check className="size-3.5" /> : null}
        </div>
        <div className="min-w-0">
          <p className="text-sm font-medium text-ink truncate">
            {tStep("title")}
          </p>
          <p className="text-xs text-muted-foreground truncate">
            {tStep("description")}
          </p>
        </div>
      </div>

      {completed ? (
        <span className="text-xs font-medium text-tennis-green shrink-0">
          {t("completedBadge")}
        </span>
      ) : stepKey === "trainerMode" ? (
        <Button
          size="sm"
          variant="outline"
          onClick={onTrainerModeClick}
          className="shrink-0"
        >
          {tStep("cta")}
          <ArrowRight className="size-3" />
        </Button>
      ) : href ? (
        <Button
          size="sm"
          variant="outline"
          asChild
          className="shrink-0"
        >
          <Link href={href}>
            {tStep("cta")}
            <ArrowRight className="size-3" />
          </Link>
        </Button>
      ) : null}
    </div>
  );
}
