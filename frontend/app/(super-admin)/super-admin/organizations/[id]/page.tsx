"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { ArrowLeft, ExternalLink } from "lucide-react";

import {
  assignSubscription,
  createSubscriptionPaymentLink,
  getSubscription,
  getSubscriptionPlans,
  listOrganizations,
  markSubscriptionPaid,
  refreshSubscriptionPayment,
  setSubscriptionActive,
  type SubscriptionDto,
} from "@/lib/api/super-admin";
import { inputClass } from "@/lib/styles";
import { Button } from "@/components/ui/button";
import { NativeSelect } from "@/components/ui/native-select";
import { SubscriptionBadge } from "@/components/super-admin/subscription-badge";

const INTERVAL_MONTHLY = 1;
const INTERVAL_YEARLY = 2;

export default function OrganizationDetailPage() {
  const t = useTranslations("superAdmin");
  const params = useParams();
  const organizationId = params.id as string;
  const queryClient = useQueryClient();

  const { data: orgs } = useQuery({
    queryKey: ["super-admin", "organizations"],
    queryFn: listOrganizations,
  });
  const org = orgs?.find((o) => o.id === organizationId);

  const { data: plans } = useQuery({
    queryKey: ["super-admin", "subscription-plans"],
    queryFn: getSubscriptionPlans,
  });

  const { data: subscription, isLoading } = useQuery({
    queryKey: ["super-admin", "subscription", organizationId],
    queryFn: () => getSubscription(organizationId),
  });

  const [plan, setPlan] = useState<number>(1);
  const [interval, setInterval] = useState<number>(INTERVAL_YEARLY);
  const [startDate, setStartDate] = useState<string>("");

  // Vul het formulier met het bestaande abonnement, of met verstandige defaults.
  useEffect(() => {
    if (subscription) {
      const planId = plans?.find((p) => p.name === subscription.plan)?.plan ?? 1;
      setPlan(planId);
      setInterval(subscription.interval === "Yearly" ? INTERVAL_YEARLY : INTERVAL_MONTHLY);
      setStartDate(subscription.startDate);
    } else if (!startDate) {
      setStartDate(new Date().toISOString().slice(0, 10));
    }
  }, [subscription, plans, startDate]);

  function onSettled(data: SubscriptionDto) {
    queryClient.setQueryData(["super-admin", "subscription", organizationId], data);
    queryClient.invalidateQueries({ queryKey: ["super-admin", "organizations"] });
  }

  const assignMutation = useMutation({
    mutationFn: () =>
      assignSubscription(organizationId, { plan, interval, startDate: startDate || null }),
    onSuccess: onSettled,
  });

  const activeMutation = useMutation({
    mutationFn: (isActive: boolean) => setSubscriptionActive(organizationId, isActive),
    onSuccess: onSettled,
  });

  const paymentLinkMutation = useMutation({
    mutationFn: () => createSubscriptionPaymentLink(organizationId),
    onSuccess: onSettled,
  });

  const refreshMutation = useMutation({
    mutationFn: () => refreshSubscriptionPayment(organizationId),
    onSuccess: onSettled,
  });

  const markPaidMutation = useMutation({
    mutationFn: () => markSubscriptionPaid(organizationId),
    onSuccess: onSettled,
  });

  const selectedPlan = plans?.find((p) => p.plan === plan);
  const selectedPrice =
    selectedPlan && (interval === INTERVAL_YEARLY ? selectedPlan.yearlyPrice : selectedPlan.monthlyPrice);

  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <Link
          href="/super-admin/organizations"
          className="inline-flex items-center gap-1.5 text-sm text-ink-3 hover:text-ink"
        >
          <ArrowLeft className="h-4 w-4" />
          {t("navOrganizations")}
        </Link>
        <div className="mt-2 flex items-center gap-3">
          <h1 className="text-2xl font-bold text-ink">{org?.name ?? t("loading")}</h1>
          {subscription && (
            <SubscriptionBadge
              plan={subscription.plan}
              active={subscription.isActive}
              paymentStatus={subscription.lastPaymentStatus}
            />
          )}
        </div>
        {org && <p className="mt-1 text-sm text-ink-3">{org.email}</p>}
      </div>

      {isLoading ? (
        <div className="text-sm text-ink-3">{t("loading")}</div>
      ) : (
        <>
          {/* Plan toekennen */}
          <section className="rounded-lg border border-rule bg-white p-6">
            <h2 className="text-base font-bold text-ink">{t("subPlanTitle")}</h2>
            <p className="mt-1 text-sm text-ink-3">{t("subPlanSubtitle")}</p>

            <div className="mt-4 grid gap-4 sm:grid-cols-3">
              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-2">{t("subPlanLabel")}</span>
                <NativeSelect value={plan} onChange={(e) => setPlan(Number(e.target.value))}>
                  {plans?.map((p) => (
                    <option key={p.plan} value={p.plan}>
                      {p.name} ({p.studentLimit} lln.)
                    </option>
                  ))}
                </NativeSelect>
              </label>

              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-2">{t("subIntervalLabel")}</span>
                <NativeSelect value={interval} onChange={(e) => setInterval(Number(e.target.value))}>
                  <option value={INTERVAL_MONTHLY}>{t("subMonthly")}</option>
                  <option value={INTERVAL_YEARLY}>{t("subYearly")}</option>
                </NativeSelect>
              </label>

              <label className="block">
                <span className="mb-1 block text-xs font-medium text-ink-2">{t("subStartDate")}</span>
                <input
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className={inputClass}
                />
              </label>
            </div>

            <div className="mt-4 flex items-center justify-between">
              <span className="text-sm text-ink-2">
                {selectedPrice !== undefined && (
                  <>
                    {t("subPrice")}: <strong className="text-ink">€{selectedPrice}</strong>{" "}
                    {t("subExclVat")}
                  </>
                )}
              </span>
              <Button onClick={() => assignMutation.mutate()} disabled={assignMutation.isPending}>
                {assignMutation.isPending ? t("submitting") : t("subAssign")}
              </Button>
            </div>
            {assignMutation.isError && (
              <p className="mt-2 text-sm text-red-600">{t("subError")}</p>
            )}
          </section>

          {subscription && (
            <>
              {/* Status */}
              <section className="rounded-lg border border-rule bg-white p-6">
                <h2 className="text-base font-bold text-ink">{t("subStatusTitle")}</h2>
                <div className="mt-4 flex items-center justify-between">
                  <div className="text-sm text-ink-2">
                    {subscription.isActive ? t("subActive") : t("subInactive")}
                  </div>
                  <Button
                    variant="outline"
                    onClick={() => activeMutation.mutate(!subscription.isActive)}
                    disabled={activeMutation.isPending}
                  >
                    {subscription.isActive ? t("subDeactivate") : t("subActivate")}
                  </Button>
                </div>
              </section>

              {/* Betaling */}
              <section className="rounded-lg border border-rule bg-white p-6">
                <h2 className="text-base font-bold text-ink">{t("subBillingTitle")}</h2>

                {!subscription.billingConfigured && (
                  <p className="mt-2 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800">
                    {t("subFakeNote")}
                  </p>
                )}

                <div className="mt-4 flex flex-wrap items-center gap-3">
                  <Button
                    onClick={() => paymentLinkMutation.mutate()}
                    disabled={paymentLinkMutation.isPending}
                  >
                    {paymentLinkMutation.isPending ? t("submitting") : t("subGenerateLink")}
                  </Button>
                  {subscription.lastPaymentId && (
                    <>
                      <Button
                        variant="outline"
                        onClick={() => refreshMutation.mutate()}
                        disabled={refreshMutation.isPending}
                      >
                        {t("subRefresh")}
                      </Button>
                      <Button
                        variant="outline"
                        onClick={() => markPaidMutation.mutate()}
                        disabled={markPaidMutation.isPending}
                      >
                        {t("subMarkPaid")}
                      </Button>
                    </>
                  )}
                </div>

                {subscription.lastCheckoutUrl && (
                  <div className="mt-4 space-y-2 text-sm">
                    <div className="flex items-center gap-2">
                      <span className="text-ink-3">{t("subCheckoutLink")}:</span>
                      <a
                        href={subscription.lastCheckoutUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-1 text-tennis-green hover:underline break-all"
                      >
                        {subscription.lastCheckoutUrl}
                        <ExternalLink className="h-3.5 w-3.5 shrink-0" />
                      </a>
                    </div>
                    <div className="text-ink-3">
                      {t("subPaymentStatus")}:{" "}
                      <strong className="text-ink">{subscription.lastPaymentStatus}</strong>
                      {subscription.lastPaidAt &&
                        ` · ${new Date(subscription.lastPaidAt).toLocaleString("nl-BE")}`}
                    </div>
                  </div>
                )}
              </section>
            </>
          )}
        </>
      )}
    </div>
  );
}
