"use client";

import { hasPendingMollieCheckout } from "@/lib/billing";
import { Suspense, useEffect, useRef, useState, type ReactNode } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { ArrowLeft, CreditCard, FileText, Receipt, Sparkles } from "lucide-react";
import {
  billingQueryKey,
  cancelSubscription,
  getBillingOverview,
  saveBillingProfile,
  setFakePaymentStatus,
  startCheckout,
  syncCheckout,
  type BillingOverview,
} from "@/lib/api/billing";
import { getAuthUser } from "@/lib/auth";
import { PlanPicker } from "@/components/billing/plan-picker";
import { BillingProfileForm } from "@/components/billing/billing-profile-form";
import { PaymentHistory } from "@/components/billing/payment-history";
import { SubscriptionStatusCard } from "@/components/billing/subscription-status-card";
import { btnPrimary, btnSecondary } from "@/lib/styles";

function Section({ icon, title, subtitle, children }: { icon: ReactNode; title: string; subtitle?: string; children: ReactNode }) {
  return (
    <section className="bg-white rounded-xl shadow-sm shadow-gray-100 overflow-hidden mb-6">
      <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2.5">
        <div className="w-7 h-7 rounded-lg bg-tennis-green/10 flex items-center justify-center text-tennis-green">{icon}</div>
        <div>
          <h2 className="text-sm font-semibold text-gray-800">{title}</h2>
          {subtitle && <p className="text-xs text-gray-400">{subtitle}</p>}
        </div>
      </div>
      {children}
    </section>
  );
}

function SubscriptionPageContent() {
  const t = useTranslations("billing");
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();
  const user = getAuthUser();
  const queryKey = billingQueryKey(user?.organizationId);
  const organizationName =
    user?.memberships?.find((m) => m.organizationId === user.organizationId)?.organizationName ?? "";
  const fakePaymentId = searchParams.get("fakePayment");
  const isReturn = searchParams.get("return") === "1";
  const [showFakePanel, setShowFakePanel] = useState(fakePaymentId !== null);

  const { data: overview, isLoading, isError } = useQuery({ queryKey, queryFn: getBillingOverview });

  function applyOverview(next: BillingOverview) {
    queryClient.setQueryData(queryKey, next);
    // Banner en toegang volgen de abonnementsstatus.
    queryClient.invalidateQueries({ queryKey: ["subscription"] });
  }

  // Variabele van de mutatie = "melding tonen" (enkel gebruikt in onSuccess).
  const syncMutation = useMutation<BillingOverview, Error, boolean>({
    mutationFn: () => syncCheckout(),
    onSuccess: (next, announce) => {
      const wasActive = overview?.status === "Active";
      applyOverview(next);
      const latest = next.payments[0];
      if (next.status === "Active" && (announce || !wasActive)) toast.success(t("returnPaid"));
      else if (!announce) return;
      else if (latest?.status === "Failed" || latest?.status === "Expired" || latest?.status === "Canceled") toast.error(t("returnFailed"));
      else toast.info(t("returnOpen"));
      router.replace(pathname);
    },
  });

  // Terugkeer van Mollie (?return=1), of een eerste betaling die nog openstaat (bv. na opnieuw
  // inloggen): status één keer ophalen, ook zonder webhook. Enkel bij terugkeer altijd een melding.
  const syncedOnce = useRef(false);
  const pendingCheckout = hasPendingMollieCheckout(overview);
  useEffect(() => {
    if ((isReturn || pendingCheckout) && !syncedOnce.current) {
      syncedOnce.current = true;
      syncMutation.mutate(isReturn);
    }
  }, [isReturn, pendingCheckout, syncMutation]);

  const fakeMutation = useMutation({
    mutationFn: (status: "paid" | "failed") => setFakePaymentStatus(fakePaymentId ?? "", status),
    onSuccess: () => {
      setShowFakePanel(false);
      syncMutation.mutate(true);
    },
  });

  const profileMutation = useMutation({
    mutationFn: saveBillingProfile,
    onSuccess: () => {
      toast.success(t("profileSaved"));
      queryClient.invalidateQueries({ queryKey });
    },
  });

  const checkoutMutation = useMutation({
    mutationFn: startCheckout,
    onSuccess: ({ checkoutUrl }) => {
      if (checkoutUrl) {
        window.location.href = checkoutUrl;
        return;
      }
      toast.success(t("bankTransferStarted"));
      queryClient.invalidateQueries({ queryKey });
      queryClient.invalidateQueries({ queryKey: ["subscription"] });
    },
  });

  const cancelMutation = useMutation({
    mutationFn: cancelSubscription,
    onSuccess: (next) => {
      applyOverview(next);
      toast.success(t("cancelDone"));
    },
  });

  return (
    <div className="max-w-3xl mx-auto">
      <Link href="/dashboard/settings" className="inline-flex items-center gap-1 text-xs text-gray-500 hover:text-gray-800 mb-3">
        <ArrowLeft size={12} />
        {t("backToSettings")}
      </Link>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 tracking-tight">{t("pageTitle")}</h1>
        <p className="text-gray-400 text-sm mt-1">{t("pageSubtitle")}</p>
      </div>

      {showFakePanel && fakePaymentId && process.env.NODE_ENV === "development" && (
        <div className="mb-6 rounded-xl border border-dashed border-amber-300 bg-amber-50 p-4">
          <p className="text-sm font-semibold text-amber-900">{t("fakeTitle")}</p>
          <p className="text-xs text-amber-800 mt-1 mb-3">{t("fakeBody")}</p>
          <div className="flex gap-2">
            <button type="button" className={btnPrimary} disabled={fakeMutation.isPending} onClick={() => fakeMutation.mutate("paid")}>
              {t("fakePaid")}
            </button>
            <button type="button" className={btnSecondary} disabled={fakeMutation.isPending} onClick={() => fakeMutation.mutate("failed")}>
              {t("fakeFailed")}
            </button>
          </div>
        </div>
      )}

      {syncMutation.isPending && <p className="mb-4 text-sm text-gray-500">{t("returnChecking")}</p>}
      {isError && <p className="text-sm text-red-500">{t("loadError")}</p>}
      {isLoading && <div className="h-40 rounded-xl bg-white animate-pulse" />}

      {overview && (
        <>
          <Section icon={<CreditCard size={14} />} title={t("statusTitle")}>
            <SubscriptionStatusCard overview={overview} cancelling={cancelMutation.isPending} onCancel={() => cancelMutation.mutate()} />
          </Section>

          {overview.canCheckout && (
            <Section icon={<Sparkles size={14} />} title={t("planTitle")} subtitle={t("planSubtitle")}>
              {overview.checkoutAvailable ? (
                <PlanPicker overview={overview} pending={checkoutMutation.isPending} onCheckout={(r) => checkoutMutation.mutate(r)} />
              ) : (
                <p className="p-5 text-sm text-gray-600">{t("checkoutUnavailable")}</p>
              )}
            </Section>
          )}

          <Section icon={<Receipt size={14} />} title={t("profileTitle")} subtitle={t("profileSubtitle")}>
            <BillingProfileForm
              key={overview.profile ? "profile" : "empty"}
              profile={overview.profile}
              defaultName={organizationName}
              pending={profileMutation.isPending}
              onSave={(r) => profileMutation.mutate(r)}
            />
          </Section>

          <Section icon={<FileText size={14} />} title={t("historyTitle")}>
            <PaymentHistory payments={overview.payments} />
          </Section>
        </>
      )}
    </div>
  );
}

export default function SubscriptionPage() {
  return (
    <Suspense>
      <SubscriptionPageContent />
    </Suspense>
  );
}
