"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  CheckCircle2,
  Loader2,
  XCircle,
  AlertTriangle,
} from "lucide-react";
import {
  getPaymentStatusByCampEnrollment,
  type PaymentStatusDto,
} from "@/lib/api/payments";

const POLL_INTERVAL_MS = 3_000;
// Lokaal dev (geen webhook) leunt volledig op de polling — Mollie checkout
// loopt typisch binnen 30s af, maar laat marge voor trage methodes.
const POLL_MAX_MS = 5 * 60_000;

export default function CampPaymentThankYouPage() {
  // Next.js eist een Suspense boundary rond useSearchParams om client-side
  // bailout tijdens prerender te voorkomen.
  return (
    <Suspense fallback={<PageShellFallback />}>
      <CampPaymentThankYouInner />
    </Suspense>
  );
}

function PageShellFallback() {
  return (
    <PageShell>
      <div className="flex items-center justify-center py-16">
        <Loader2 size={28} className="text-tennis-green animate-spin" />
      </div>
    </PageShell>
  );
}

function CampPaymentThankYouInner() {
  const t = useTranslations("paymentThankYou");
  const searchParams = useSearchParams();
  const campEnrollmentId = searchParams.get("campEnrollmentId");

  if (!campEnrollmentId) {
    return (
      <PageShell>
        <StateCard
          tone="error"
          icon={<AlertTriangle size={40} className="text-amber-500" />}
          title={t("missingParam")}
        />
      </PageShell>
    );
  }

  return (
    <PageShell>
      <PaymentStatusView campEnrollmentId={campEnrollmentId} t={t} />
    </PageShell>
  );
}

interface PaymentStatusViewProps {
  campEnrollmentId: string;
  t: ReturnType<typeof useTranslations<"paymentThankYou">>;
}

function PaymentStatusView({ campEnrollmentId, t }: PaymentStatusViewProps) {
  const { data, isLoading, isError } = useQuery<PaymentStatusDto>({
    queryKey: ["campPaymentStatus", campEnrollmentId],
    // sync=true → backend forceert een Mollie-roundtrip wanneer status nog
    // Pending is; vangt het lokaal-dev scenario zonder webhook delivery op.
    queryFn: () => getPaymentStatusByCampEnrollment(campEnrollmentId, true),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      if (status === "Paid" || status === "Failed" || status === "Refunded") {
        return false;
      }
      const elapsed = Date.now() - query.state.dataUpdatedAt;
      return elapsed > POLL_MAX_MS ? false : POLL_INTERVAL_MS;
    },
    staleTime: 0,
  });

  if (isLoading) {
    return (
      <StateCard
        tone="pending"
        icon={<Loader2 size={40} className="text-tennis-green animate-spin" />}
        title={t("pendingTitle")}
        body={t("pendingBody")}
      />
    );
  }

  if (isError || !data) {
    return (
      <StateCard
        tone="error"
        icon={<AlertTriangle size={40} className="text-amber-500" />}
        title={t("loadError")}
      />
    );
  }

  if (data.status === "Paid") {
    return (
      <StateCard
        tone="success"
        icon={<CheckCircle2 size={40} className="text-emerald-500" />}
        title={t("paidTitle")}
        body={t("paidBody")}
        details={[{ label: t("amountLabel"), value: formatAmount(data) }]}
      />
    );
  }

  if (data.status === "Failed") {
    const details: { label: string; value: string }[] = [
      { label: t("amountLabel"), value: formatAmount(data) },
    ];
    if (data.failureReason) {
      details.push({ label: t("reasonLabel"), value: data.failureReason });
    }
    return (
      <StateCard
        tone="error"
        icon={<XCircle size={40} className="text-red-500" />}
        title={t("failedTitle")}
        body={t("failedBody")}
        details={details}
        action={
          data.checkoutUrl
            ? { label: t("retryButton"), href: data.checkoutUrl }
            : undefined
        }
      />
    );
  }

  // Pending / Refunded → blijf op pending-view tonen; polling regelt updates.
  return (
    <StateCard
      tone="pending"
      icon={<Loader2 size={40} className="text-tennis-green animate-spin" />}
      title={t("pendingTitle")}
      body={t("pendingBody")}
    />
  );
}

// ─── Presentational ──────────────────────────────────────────────────────────

function PageShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-[#FAFAF8] px-4 py-12">
      <div className="w-full max-w-md">{children}</div>
    </div>
  );
}

interface StateCardProps {
  tone: "pending" | "success" | "error";
  icon: React.ReactNode;
  title: string;
  body?: string;
  details?: { label: string; value: string }[];
  action?: { label: string; href: string };
}

function StateCard({ tone, icon, title, body, details, action }: StateCardProps) {
  const accent =
    tone === "success"
      ? "bg-emerald-50"
      : tone === "error"
        ? "bg-amber-50"
        : "bg-tennis-green/5";

  return (
    <div className="bg-white rounded-2xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className={`${accent} flex items-center justify-center py-10`}>{icon}</div>
      <div className="p-6 text-center space-y-3">
        <h1 className="text-xl font-bold text-gray-900">{title}</h1>
        {body && <p className="text-sm text-gray-500 leading-relaxed">{body}</p>}
        {details && details.length > 0 && (
          <dl className="grid grid-cols-2 gap-2 text-left text-xs text-gray-600 pt-4 border-t border-gray-100">
            {details.map((d) => (
              <div key={d.label} className="contents">
                <dt className="font-medium text-gray-500">{d.label}</dt>
                <dd className="text-gray-800">{d.value}</dd>
              </div>
            ))}
          </dl>
        )}
        {action && (
          <a
            href={action.href}
            className="inline-flex items-center justify-center px-4 py-2 bg-tennis-green text-white text-xs font-semibold rounded-lg hover:bg-tennis-green/90 transition-colors mt-4"
          >
            {action.label}
          </a>
        )}
      </div>
    </div>
  );
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function formatAmount(data: PaymentStatusDto): string {
  try {
    return new Intl.NumberFormat("nl-BE", {
      style: "currency",
      currency: data.currency || "EUR",
    }).format(data.amount);
  } catch {
    return `${data.amount.toFixed(2)} ${data.currency}`;
  }
}
