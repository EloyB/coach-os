"use client";

import { useState, type ReactNode } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { ArrowLeft, FileText } from "lucide-react";

import {
  assignDeal,
  cancelOrganizationSubscription,
  extendOrganizationTrial,
  getSubscription,
  getSubscriptionPlans,
  listOrganizations,
  markSubscriptionPaymentPaid,
  sendPaymentInvitation,
  type SubscriptionDto,
} from "@/lib/api/super-admin";
import { formatEuro } from "@/lib/billing";
import { formatDateNL } from "@/lib/date-utils";
import { inputClass } from "@/lib/styles";
import { Button } from "@/components/ui/button";
import { NativeSelect } from "@/components/ui/native-select";
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

const TRIAL_EXTENSION_DAYS = 14;

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex gap-4 py-1.5 text-sm">
      <dt className="w-36 shrink-0 text-ink-3">{label}</dt>
      <dd className="text-ink">{children}</dd>
    </div>
  );
}

function Card({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <section className="rounded-lg border border-rule bg-white p-6">
      <h2 className="text-base font-bold text-ink">{title}</h2>
      {subtitle && <p className="mt-1 text-sm text-ink-3">{subtitle}</p>}
      <div className="mt-4">{children}</div>
    </section>
  );
}

export default function OrganizationDetailPage() {
  const t = useTranslations("superAdmin");
  const organizationId = useParams().id as string;
  const queryClient = useQueryClient();
  const subscriptionKey = ["super-admin", "subscription", organizationId] as const;

  const { data: orgs } = useQuery({ queryKey: ["super-admin", "organizations"], queryFn: listOrganizations });
  const org = orgs?.find((o) => o.id === organizationId);
  const { data: plans } = useQuery({ queryKey: ["super-admin", "subscription-plans"], queryFn: getSubscriptionPlans });
  const { data: subscription, isLoading } = useQuery({
    queryKey: subscriptionKey,
    queryFn: () => getSubscription(organizationId),
  });

  const [dealPlan, setDealPlan] = useState(1);
  const [dealPrice, setDealPrice] = useState(0);
  const [dealEndDate, setDealEndDate] = useState("");
  const [confirmCancel, setConfirmCancel] = useState(false);

  function apply(next: SubscriptionDto) {
    queryClient.setQueryData(subscriptionKey, next);
    queryClient.invalidateQueries({ queryKey: ["super-admin", "organizations"] });
  }

  const dealMutation = useMutation({
    mutationFn: () => assignDeal(organizationId, { plan: dealPlan, price: dealPrice, endDate: dealEndDate }),
    onSuccess: (next) => {
      apply(next);
      toast.success(t("dealSaved"));
    },
  });
  const markPaidMutation = useMutation({
    mutationFn: (paymentId: string) => markSubscriptionPaymentPaid(organizationId, paymentId),
    onSuccess: (next) => {
      apply(next);
      toast.success(t("paymentMarkedPaid"));
    },
  });
  const cancelMutation = useMutation({
    mutationFn: () => cancelOrganizationSubscription(organizationId),
    onSuccess: (next) => {
      apply(next);
      toast.success(t("billingCancelled"));
    },
  });
  const inviteMutation = useMutation({
    mutationFn: () => sendPaymentInvitation(organizationId),
    onSuccess: () => toast.success(t("billingInviteSent")),
  });
  const extendMutation = useMutation({
    mutationFn: () => extendOrganizationTrial(organizationId, TRIAL_EXTENSION_DAYS),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: subscriptionKey }),
  });

  const status = subscription?.status ?? "Legacy";

  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <Link href="/super-admin/organizations" className="inline-flex items-center gap-1.5 text-sm text-ink-3 hover:text-ink">
          <ArrowLeft className="h-4 w-4" />
          {t("navOrganizations")}
        </Link>
        <h1 className="mt-2 text-2xl font-bold text-ink">{org?.name ?? t("loading")}</h1>
        {org && <p className="mt-1 text-sm text-ink-3">{org.email}</p>}
      </div>

      {isLoading ? (
        <div className="text-sm text-ink-3">{t("loading")}</div>
      ) : (
        <>
          <Card title={t("billingTitle")}>
            {!subscription ? (
              <p className="text-sm text-ink-3">{t("billingNone")}</p>
            ) : (
              <>
                <dl>
                  <Row label={t("billingStatus")}>
                    {t(`billingStatus${status}`)}
                    {subscription.isDeal && (
                      <span className="ml-2 rounded-full bg-tennis-lime px-2 py-0.5 text-[11px] font-semibold text-ink">
                        {t("billingDealBadge")}
                      </span>
                    )}
                  </Row>
                  {subscription.plan && (
                    <Row label={t("billingPlan")}>
                      {subscription.plan}
                      {subscription.interval && ` · ${subscription.interval}`}
                    </Row>
                  )}
                  {subscription.paymentMethod && (
                    <Row label={t("billingMethod")}>
                      {subscription.paymentMethod === "Mollie" ? t("billingMethodMollie") : t("billingMethodBankTransfer")}
                    </Row>
                  )}
                  {subscription.plan && (
                    <Row label={t("billingPrice")}>
                      {t("billingPriceValue", {
                        net: formatEuro(subscription.price),
                        gross: formatEuro(subscription.priceInclVat),
                      })}
                    </Row>
                  )}
                  {subscription.currentPeriodStart && subscription.currentPeriodEnd && (
                    <Row label={t("billingPeriod")}>
                      {formatDateNL(subscription.currentPeriodStart)} – {formatDateNL(subscription.currentPeriodEnd)}
                    </Row>
                  )}
                  {status === "Trialing" && subscription.trialEndsAt && (
                    <Row label={t("billingTrialEnds")}>{formatDateNL(subscription.trialEndsAt)}</Row>
                  )}
                  {subscription.cancelAt && <Row label={t("billingCancelAt")}>{formatDateNL(subscription.cancelAt)}</Row>}
                  {subscription.mollieCustomerId && (
                    <Row label={t("billingMollieIds")}>
                      <span className="font-mono text-xs">
                        {subscription.mollieCustomerId}
                        {subscription.mollieSubscriptionId && ` · ${subscription.mollieSubscriptionId}`}
                      </span>
                    </Row>
                  )}
                </dl>

                {(!subscription.billingConfigured || !subscription.invoicingConfigured) && (
                  <div className="mt-3 space-y-1 rounded-md bg-amber-50 p-3 text-xs text-amber-800">
                    {!subscription.billingConfigured && <p>{t("billingFakeMollie")}</p>}
                    {!subscription.invoicingConfigured && <p>{t("billingFakeInvoicing")}</p>}
                  </div>
                )}

                <div className="mt-4 flex flex-wrap gap-2">
                  {status === "Trialing" && (
                    <Button variant="outline" onClick={() => extendMutation.mutate()} disabled={extendMutation.isPending}>
                      {t("extendTrial", { days: TRIAL_EXTENSION_DAYS })}
                    </Button>
                  )}
                  {status === "Legacy" && (
                    <Button variant="outline" onClick={() => inviteMutation.mutate()} disabled={inviteMutation.isPending}>
                      {t("billingInvite")}
                    </Button>
                  )}
                  {status === "Active" && (
                    <Button variant="outline" onClick={() => setConfirmCancel(true)} disabled={cancelMutation.isPending}>
                      {t("billingCancel")}
                    </Button>
                  )}
                </div>
              </>
            )}
          </Card>

          <Card title={t("dealTitle")} subtitle={t("dealSubtitle")}>
            <div className="grid gap-3 sm:grid-cols-3">
              <label className="text-xs font-medium text-ink-2">
                {t("dealPlan")}
                <NativeSelect value={dealPlan} onChange={(e) => setDealPlan(Number(e.target.value))} className="mt-1">
                  {plans?.map((p) => (
                    <option key={p.plan} value={p.plan}>
                      {p.name}
                    </option>
                  ))}
                </NativeSelect>
              </label>
              <label className="text-xs font-medium text-ink-2">
                {t("dealPrice")}
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  value={dealPrice}
                  onChange={(e) => setDealPrice(e.target.valueAsNumber || 0)}
                  className={`${inputClass} mt-1`}
                />
              </label>
              <label className="text-xs font-medium text-ink-2">
                {t("dealEndDate")}
                <input type="date" value={dealEndDate} onChange={(e) => setDealEndDate(e.target.value)} className={`${inputClass} mt-1`} />
              </label>
            </div>
            <Button className="mt-4" onClick={() => dealMutation.mutate()} disabled={dealMutation.isPending || !dealEndDate}>
              {t("dealSubmit")}
            </Button>
          </Card>

          <Card title={t("profileTitle")}>
            {subscription?.profile ? (
              <dl>
                <Row label={t("profileTitle")}>
                  {subscription.profile.name}
                  <br />
                  {subscription.profile.street}, {subscription.profile.postalCode} {subscription.profile.city}
                  <br />
                  {subscription.profile.invoiceEmail}
                </Row>
                {subscription.profile.vatNumber && (
                  <Row label={t("profileVat")}>
                    {subscription.profile.vatNumber} (
                    {subscription.profile.vatVerified ? t("profileVatVerified") : t("profileVatUnverified")})
                  </Row>
                )}
              </dl>
            ) : (
              <p className="text-sm text-ink-3">{t("profileNone")}</p>
            )}
          </Card>

          <Card title={t("paymentsTitle")}>
            {!subscription || subscription.payments.length === 0 ? (
              <p className="text-sm text-ink-3">{t("paymentsEmpty")}</p>
            ) : (
              <ul className="divide-y divide-rule">
                {subscription.payments.map((p) => (
                  <li key={p.id} className="flex flex-wrap items-center gap-x-4 gap-y-1 py-3 text-sm">
                    <div className="min-w-[200px] flex-1">
                      <p className="font-semibold text-ink">
                        {p.plan} · {formatEuro(p.amountInclVat)} · {p.kind}
                      </p>
                      <p className="text-xs text-ink-3">
                        {formatDateNL(p.periodStart)} – {formatDateNL(p.periodEnd)}
                        {p.status === "Open" && p.dueDate && ` · ${t("paymentDue", { date: formatDateNL(p.dueDate) })}`}
                      </p>
                      {p.invoiceLastError && p.invoiceStatus === "Failed" && (
                        <p className="text-xs text-red-600">{t("paymentInvoiceError", { error: p.invoiceLastError })}</p>
                      )}
                    </div>
                    <span className="text-xs font-semibold text-ink-2">{p.status}</span>
                    {p.invoiceNumber &&
                      (p.invoicePdfUrl ? (
                        <a href={p.invoicePdfUrl} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1 text-xs text-tennis-green">
                          <FileText className="h-3 w-3" />
                          {t("paymentInvoice", { number: p.invoiceNumber })}
                        </a>
                      ) : (
                        <span className="text-xs text-ink-3">{t("paymentInvoice", { number: p.invoiceNumber })}</span>
                      ))}
                    {p.status === "Open" && p.method === "BankTransfer" && (
                      <Button size="sm" variant="outline" onClick={() => markPaidMutation.mutate(p.id)} disabled={markPaidMutation.isPending}>
                        {t("paymentMarkPaid")}
                      </Button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </>
      )}

      <AlertDialog open={confirmCancel} onOpenChange={setConfirmCancel}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("billingCancelConfirmTitle")}</AlertDialogTitle>
            <AlertDialogDescription>{t("billingCancelConfirmBody")}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("billingCancelKeep")}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                setConfirmCancel(false);
                cancelMutation.mutate();
              }}
              className="bg-red-600 hover:bg-red-700"
            >
              {t("billingCancelConfirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
