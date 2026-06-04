"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { getAuthUser } from "@/lib/auth";

const SEEN_KEY = "coachos.welcome.seen";

interface WelcomeModalProps {
  shouldShow: boolean;
}

export function WelcomeModal({ shouldShow }: WelcomeModalProps) {
  const t = useTranslations("onboarding");
  const [open, setOpen] = useState(false);
  const [firstName, setFirstName] = useState<string | null>(null);

  // LocalStorage en getAuthUser zijn enkel beschikbaar na mount. Doen we dit synchroon
  // tijdens render, dan triggert React een hydration-mismatch op de eerste SSR pass.
  useEffect(() => {
    if (!shouldShow) return;
    const seen = localStorage.getItem(SEEN_KEY);
    if (seen) return;
    const user = getAuthUser();
    setFirstName(user?.firstName ?? null);
    setOpen(true);
  }, [shouldShow]);

  function handleClose() {
    localStorage.setItem(SEEN_KEY, "1");
    setOpen(false);
  }

  const title = firstName
    ? t("welcomeTitle", { firstName })
    : t("welcomeTitleFallback");

  return (
    <Dialog open={open} onOpenChange={(next) => !next && handleClose()}>
      <DialogContent showCloseButton={false} className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{t("welcomeBody")}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button onClick={handleClose} className="w-full sm:w-auto">
            {t("welcomeCta")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
