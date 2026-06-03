"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ArrowLeft, User, Users } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { setTrainerMode } from "@/lib/api/onboarding";

interface TrainerModeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

type Stage = "choose" | "teamSubQuestion";

export function TrainerModeDialog({ open, onOpenChange }: TrainerModeDialogProps) {
  const t = useTranslations("onboarding.trainerDialog");
  const queryClient = useQueryClient();
  const router = useRouter();
  const [stage, setStage] = useState<Stage>("choose");

  const mutation = useMutation({
    mutationFn: ({ adminActsAsTrainer }: { adminActsAsTrainer: boolean; redirectToTrainers: boolean }) =>
      setTrainerMode(adminActsAsTrainer),
    onSuccess: (_data, vars) => {
      queryClient.invalidateQueries({ queryKey: ["onboarding"] });
      reset();
      onOpenChange(false);
      if (vars.redirectToTrainers) {
        router.push("/dashboard/trainers");
      }
    },
  });

  function reset() {
    setStage("choose");
  }

  function handleOpenChange(next: boolean) {
    if (!next) reset();
    onOpenChange(next);
  }

  function chooseSolo() {
    mutation.mutate({ adminActsAsTrainer: true, redirectToTrainers: false });
  }

  function chooseTeamYes() {
    mutation.mutate({ adminActsAsTrainer: true, redirectToTrainers: true });
  }

  function chooseTeamNo() {
    mutation.mutate({ adminActsAsTrainer: false, redirectToTrainers: true });
  }

  const saving = mutation.isPending;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        {stage === "choose" ? (
          <>
            <DialogHeader>
              <DialogTitle>{t("title")}</DialogTitle>
              <DialogDescription>{t("description")}</DialogDescription>
            </DialogHeader>
            <div className="grid gap-3">
              <Button
                variant="outline"
                className="h-auto justify-start gap-3 py-3"
                onClick={chooseSolo}
                disabled={saving}
              >
                <User className="size-4" />
                <span className="text-left">{t("solo")}</span>
              </Button>
              <Button
                variant="outline"
                className="h-auto justify-start gap-3 py-3"
                onClick={() => setStage("teamSubQuestion")}
                disabled={saving}
              >
                <Users className="size-4" />
                <span className="text-left">{t("team")}</span>
              </Button>
            </div>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>{t("subQuestionTitle")}</DialogTitle>
              <DialogDescription>{t("subQuestionDescription")}</DialogDescription>
            </DialogHeader>
            <div className="grid gap-3">
              <Button
                onClick={chooseTeamYes}
                disabled={saving}
              >
                {saving ? t("saving") : t("yes")}
              </Button>
              <Button
                variant="outline"
                onClick={chooseTeamNo}
                disabled={saving}
              >
                {saving ? t("saving") : t("no")}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="justify-start"
                onClick={() => setStage("choose")}
                disabled={saving}
              >
                <ArrowLeft className="size-3" />
                {t("back")}
              </Button>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
