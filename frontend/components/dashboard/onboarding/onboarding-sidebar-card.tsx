"use client";

import { useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ArrowRight, Check, PartyPopper } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  dismissOnboarding,
  getOnboardingState,
  type OnboardingStepKey,
} from "@/lib/api/onboarding";
import { cn } from "@/lib/utils";
import { TrainerModeDialog } from "./trainer-mode-dialog";

const STEP_ORDER: OnboardingStepKey[] = ["mollie", "club", "trainerMode", "series"];

function stepHref(key: OnboardingStepKey): string | null {
  switch (key) {
    case "mollie":
    case "club":
      return "/dashboard/settings";
    case "series":
      return "/dashboard/lessons/new";
    case "trainerMode":
      return null;
  }
}

/**
 * Kleine kaart onderaan de sidenav met de eerstvolgende onboardingstap. Een klik opent een
 * dialog met alle stappen (voltooid en nog te doen). Enkel voor admins die de onboarding
 * nog niet gesloten hebben.
 */
export function OnboardingSidebarCard({ enabled }: { enabled: boolean }) {
  const t = useTranslations("onboarding");
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
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
      setDialogOpen(false);
      queryClient.invalidateQueries({ queryKey: ["onboarding"] });
    },
  });

  if (!enabled || !data?.shouldShow) return null;

  const completedByKey = new Map(data.steps.map((s) => [s.key, s.completed]));
  const done = data.steps.filter((s) => s.completed).length;
  const total = data.steps.length;
  const progressPct = total === 0 ? 0 : Math.round((done / total) * 100);
  const nextStep = STEP_ORDER.find((key) => !completedByKey.get(key));

  return (
    <>
      <button
        type="button"
        onClick={() => setDialogOpen(true)}
        className="group relative z-10 mx-2.5 mb-2.5 cursor-pointer rounded-lg bg-black/[.18] px-3 py-2.5 text-left transition-colors hover:bg-black/[.28] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-tennis-lime"
      >
        <div className="flex items-center justify-between gap-2">
          <span className="text-[9.5px] font-bold uppercase tracking-[0.12em] text-white/50">
            {t("sidebarLabel")}
          </span>
          <span className="font-mono text-[10px] text-white/50">
            {t("sidebarProgress", { done, total })}
          </span>
        </div>
        <p className="mt-1.5 flex items-center justify-between gap-2 text-[12px] font-semibold text-white">
          <span className="min-w-0 truncate">
            {nextStep ? t(`step.${nextStep}.title`) : t("sidebarAllDone")}
          </span>
          <ArrowRight
            size={13}
            aria-hidden
            className="shrink-0 text-white/50 transition-transform group-hover:translate-x-0.5"
          />
        </p>
        <div className="mt-2 h-1 overflow-hidden rounded-full bg-white/15">
          <div
            className="h-full rounded-full bg-tennis-lime transition-all"
            style={{ width: `${progressPct}%` }}
          />
        </div>
      </button>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("checklistTitle")}</DialogTitle>
            <DialogDescription>
              {t("checklistSubtitle", { done, total })}
            </DialogDescription>
          </DialogHeader>

          {data.allCompleted && (
            <div className="flex items-start gap-3 rounded-lg border border-tennis-green/30 bg-tennis-green/5 px-4 py-3">
              <PartyPopper className="mt-0.5 size-5 shrink-0 text-tennis-green" />
              <div>
                <p className="text-sm font-semibold text-ink">
                  {t("celebrationTitle")}
                </p>
                <p className="text-xs text-muted-foreground">
                  {t("celebrationBody")}
                </p>
              </div>
            </div>
          )}

          <div className="flex flex-col gap-2">
            {STEP_ORDER.map((key) => (
              <StepRow
                key={key}
                stepKey={key}
                completed={completedByKey.get(key) ?? false}
                onTrainerModeClick={() => {
                  setDialogOpen(false);
                  setTrainerDialogOpen(true);
                }}
                onNavigate={() => setDialogOpen(false)}
              />
            ))}
          </div>

          <div className="flex justify-end">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => dismissMutation.mutate()}
              disabled={dismissMutation.isPending}
            >
              {data.allCompleted ? t("celebrationCta") : t("hideCard")}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <TrainerModeDialog
        open={trainerDialogOpen}
        onOpenChange={setTrainerDialogOpen}
      />
    </>
  );
}

function StepRow({
  stepKey,
  completed,
  onTrainerModeClick,
  onNavigate,
}: {
  stepKey: OnboardingStepKey;
  completed: boolean;
  onTrainerModeClick: () => void;
  onNavigate: () => void;
}) {
  const t = useTranslations("onboarding");
  const tStep = useTranslations(`onboarding.step.${stepKey}`);
  const href = stepHref(stepKey);

  return (
    <div
      className={cn(
        "flex items-center justify-between gap-3 rounded-lg border px-4 py-3",
        completed ? "border-tennis-green/30 bg-tennis-green/5" : "border-rule",
      )}
    >
      <div className="flex min-w-0 items-center gap-3">
        <div
          className={cn(
            "flex size-6 shrink-0 items-center justify-center rounded-full",
            completed
              ? "bg-tennis-green text-white"
              : "bg-muted text-muted-foreground",
          )}
          aria-hidden
        >
          {completed ? <Check className="size-3.5" /> : null}
        </div>
        <div className="min-w-0">
          <p className="truncate text-sm font-medium text-ink">
            {tStep("title")}
          </p>
          <p className="text-xs text-muted-foreground">{tStep("description")}</p>
        </div>
      </div>

      {completed ? (
        <span className="shrink-0 text-xs font-medium text-tennis-green">
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
        <Button size="sm" variant="outline" asChild className="shrink-0">
          <Link href={href} onClick={onNavigate}>
            {tStep("cta")}
            <ArrowRight className="size-3" />
          </Link>
        </Button>
      ) : null}
    </div>
  );
}
