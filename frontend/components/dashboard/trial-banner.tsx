"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { getBillingStatus } from "@/lib/api/billing";

export function TrialBanner() {
  const t = useTranslations("trial");

  const { data } = useQuery({
    queryKey: ["billing", "status"],
    queryFn: getBillingStatus,
    staleTime: 60_000,
  });

  if (!data || data.status !== "Trialing" || data.trialDaysLeft == null) {
    return null;
  }

  return (
    <div className="flex items-center justify-between gap-3 bg-tennis-lime px-7 py-2.5 text-[12.5px] font-medium text-ink shrink-0">
      <span>{t("banner", { days: data.trialDaysLeft })}</span>
      <Link href="/billing" className="font-semibold underline shrink-0">
        {t("cta")}
      </Link>
    </div>
  );
}
