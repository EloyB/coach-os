"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { btnPrimary } from "@/lib/styles";
import { useAuthUser } from "@/lib/hooks/use-auth-user";
import { useClientValue } from "@/lib/hooks/use-client-value";

const SEEN_KEY = "coachos.welcome.seen";

interface WelcomeModalProps {
  shouldShow: boolean;
}

export function WelcomeModal({ shouldShow }: WelcomeModalProps) {
  const t = useTranslations("onboarding");
  const [dismissed, setDismissed] = useState(false);
  // localStorage is enkel beschikbaar na hydration: tot dan `null`/`undefined`, zodat de
  // eerste render gelijk is aan de SSR-pass en er geen hydration-mismatch ontstaat.
  const seen = useClientValue(() => localStorage.getItem(SEEN_KEY) !== null);
  const user = useAuthUser();
  const firstName = user?.firstName ?? null;
  const open = shouldShow && seen === false && !dismissed;

  function handleClose() {
    localStorage.setItem(SEEN_KEY, "1");
    setDismissed(true);
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
          <button
            type="button"
            onClick={handleClose}
            className={btnPrimary + " w-full sm:w-auto"}
          >
            {t("welcomeCta")}
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
