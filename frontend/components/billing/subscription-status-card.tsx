"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import type { BillingOverview } from "@/lib/api/billing";
import { formatEuro } from "@/lib/billing";
import { formatDateNL } from "@/lib/date-utils";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { btnSecondary } from "@/lib/styles";

interface SubscriptionStatusCardProps {
  overview: BillingOverview;
  cancelling: boolean;
  onCancel: () => void;
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/** Wat de club nu heeft, met opzeggen (de tekst verschilt als de betaalde periode nog niet begon). */
export function SubscriptionStatusCard({ overview, cancelling, onCancel }: SubscriptionStatusCardProps) {
  const t = useTranslations("billing");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const today = todayIso();

  const interval = overview.interval === "Yearly" ? t("intervalYear") : t("intervalMonth");
  const plan = overview.plan ?? "";
  const periodStart = overview.currentPeriodStart;
  const periodEnd = overview.currentPeriodEnd ? formatDateNL(overview.currentPeriodEnd) : "";
  const beforeStart = periodStart !== null && periodStart > today;

  let text: string;
  switch (overview.status) {
    case "Trialing":
      text = overview.trialEndsAt && overview.trialEndsAt < today
        ? t("statusTrialExpired", { date: formatDateNL(overview.trialEndsAt) })
        : t("statusTrialing", { date: overview.trialEndsAt ? formatDateNL(overview.trialEndsAt) : "" });
      break;
    case "Active":
      text = overview.isDeal
        ? t("statusDeal", { plan, date: periodEnd })
        : beforeStart && periodStart
          ? t("statusActiveUpcoming", { plan, interval, date: formatDateNL(periodStart) })
          : t("statusActive", { plan, interval, date: periodEnd });
      break;
    case "Cancelled":
      text = t("statusCancelled", { plan, date: overview.cancelAt ? formatDateNL(overview.cancelAt) : periodEnd });
      break;
    case "Locked":
      text = t("statusLocked");
      break;
    default:
      text = t("statusLegacy");
  }

  return (
    <div className="p-5 space-y-3">
      <p className="text-sm text-gray-800">{text}</p>
      {overview.status === "Active" && !overview.isDeal && overview.priceExclVat > 0 && (
        <p className="text-xs text-gray-500">
          {t("priceLine", { amount: formatEuro(overview.priceExclVat), gross: formatEuro(overview.priceInclVat) })}
          {" · "}
          {overview.paymentMethod === "BankTransfer" ? t("methodBankTransfer") : t("methodMollie")}
        </p>
      )}
      {overview.status === "Active" && (
        <button type="button" onClick={() => setConfirmOpen(true)} disabled={cancelling} className={btnSecondary}>
          {t("cancelCta")}
        </button>
      )}

      <AlertDialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("cancelTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {beforeStart && !overview.isDeal ? t("cancelBeforeStartBody") : t("cancelRunningBody", { date: periodEnd })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("cancelKeep")}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                setConfirmOpen(false);
                onCancel();
              }}
              className="bg-red-600 hover:bg-red-700"
            >
              {t("cancelConfirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
