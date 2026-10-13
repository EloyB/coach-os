"use client";

import { useTranslations } from "next-intl";

interface SubscriptionBadgeProps {
  plan: string | null;
  active: boolean;
  paymentStatus: string | null;
}

export function SubscriptionBadge({
  plan,
  active,
  paymentStatus,
}: SubscriptionBadgeProps) {
  const t = useTranslations("superAdmin");

  if (!plan) {
    return (
      <span className="inline-flex items-center rounded-full bg-ink/5 px-2.5 py-0.5 text-xs font-medium text-ink-3">
        {t("subNone")}
      </span>
    );
  }

  const paid = paymentStatus === "paid";
  const tone = !active
    ? "bg-ink/5 text-ink-3"
    : paid
      ? "bg-tennis-green/10 text-tennis-green"
      : "bg-amber-100 text-amber-700";
  const status = !active
    ? t("subInactive")
    : paid
      ? t("subPaid")
      : t("subUnpaid");

  return (
    <span className="inline-flex items-center gap-1.5">
      <span
        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${tone}`}
      >
        {plan}
      </span>
      <span className="text-xs text-ink-3">{status}</span>
    </span>
  );
}
