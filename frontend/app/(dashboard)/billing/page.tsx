"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { SlashLabel } from "@/components/ui/slash-label";
import { getBillingStatus } from "@/lib/api/billing";

// Marketing site pricing page. Hardcoded for now — no in-app plan picker yet
// (Phase 2). See task-7-report.md for the reasoning.
const PRICING_URL = "https://coach-os.be/prijzen";

function BillingContent() {
  const t = useTranslations("billing");
  const searchParams = useSearchParams();
  const lockedParam = searchParams.get("locked") === "1";

  const { data, isLoading } = useQuery({
    queryKey: ["billing", "status"],
    queryFn: getBillingStatus,
  });

  if (isLoading) {
    return null;
  }

  const isLocked = lockedParam || data?.hasAccess === false;

  if (isLocked) {
    return (
      <Card className="max-w-lg border-tennis-green/20">
        <CardHeader className="border-b pb-4">
          <SlashLabel>/billing</SlashLabel>
          <CardTitle className="text-xl">{t("lockedTitle")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col items-start gap-4">
          <p className="text-sm text-muted-foreground">{t("lockedBody")}</p>
          <Button asChild className="bg-tennis-green hover:bg-tennis-green/90 text-white">
            <a href={PRICING_URL}>{t("choosePlan")}</a>
          </Button>
        </CardContent>
      </Card>
    );
  }

  if (data?.status === "Trialing" && data.trialDaysLeft != null) {
    return (
      <Card className="max-w-lg border-tennis-green/20">
        <CardHeader className="border-b pb-4">
          <SlashLabel>/billing</SlashLabel>
          <CardTitle className="text-xl">{t("trialActiveTitle")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col items-start gap-4">
          <p className="text-sm font-medium text-tennis-green">
            {t("trialDaysLeft", { days: data.trialDaysLeft })}
          </p>
          <Button asChild className="bg-tennis-green hover:bg-tennis-green/90 text-white">
            <a href={PRICING_URL}>{t("choosePlan")}</a>
          </Button>
        </CardContent>
      </Card>
    );
  }

  // Has access and isn't (or is no longer) trialing — nothing actionable to show yet.
  return null;
}

export default function BillingPage() {
  return (
    <Suspense>
      <BillingContent />
    </Suspense>
  );
}
