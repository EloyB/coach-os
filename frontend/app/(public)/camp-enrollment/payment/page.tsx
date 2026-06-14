"use client";

import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import {
  AlertTriangle,
  Banknote,
  CheckCircle2,
  CreditCard,
  Loader2,
} from "lucide-react";
import {
  chooseCampPayment,
  getCampPaymentOptions,
  type CampPaymentOptions,
} from "@/lib/api/camps";
import { getAxiosErrorMessages } from "@/lib/utils/api-errors";

const METHOD_ONLINE = 1;
const METHOD_CASH = 2;

export default function CampPaymentPage() {
  // Next.js eist een Suspense boundary rond useSearchParams om client-side
  // bailout tijdens prerender te voorkomen.
  return (
    <Suspense fallback={<PageShellFallback />}>
      <CampPaymentInner />
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

function CampPaymentInner() {
  const t = useTranslations("camps");
  const searchParams = useSearchParams();
  const campEnrollmentId = searchParams.get("campEnrollmentId");
  const campId = searchParams.get("campId");

  if (!campEnrollmentId || !campId) {
    return (
      <PageShell>
        <StateCard
          tone="error"
          icon={<AlertTriangle size={40} className="text-amber-500" />}
          title={t("payMissingParam")}
        />
      </PageShell>
    );
  }

  return (
    <PageShell>
      <PaymentChoiceView campEnrollmentId={campEnrollmentId} campId={campId} t={t} />
    </PageShell>
  );
}

interface PaymentChoiceViewProps {
  campEnrollmentId: string;
  campId: string;
  t: ReturnType<typeof useTranslations<"camps">>;
}

function PaymentChoiceView({ campEnrollmentId, campId, t }: PaymentChoiceViewProps) {
  const [submitting, setSubmitting] = useState<number | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [cashConfirmed, setCashConfirmed] = useState(false);

  const { data, isLoading, isError } = useQuery<CampPaymentOptions>({
    queryKey: ["campPaymentOptions", campId],
    queryFn: () => getCampPaymentOptions(campId),
  });

  async function choose(method: number) {
    setErrors([]);
    setSubmitting(method);
    try {
      const result = await chooseCampPayment(campEnrollmentId, method);
      if (method === METHOD_ONLINE) {
        if (result.checkoutUrl) {
          // Online → doorsturen naar Mollie checkout.
          window.location.href = result.checkoutUrl;
          return;
        }
        // Online gekozen maar geen checkout-URL (bv. Mollie-storing) → toon fout,
        // val niet door naar de cash-bevestiging.
        setErrors([t("payError")]);
        setSubmitting(null);
        return;
      }
      // Cash → checkoutUrl is null; toon bevestiging op de pagina.
      setCashConfirmed(true);
      setSubmitting(null);
    } catch (error) {
      setErrors(getAxiosErrorMessages(error, t("payError")));
      setSubmitting(null);
    }
  }

  if (isLoading) {
    return (
      <StateCard
        tone="pending"
        icon={<Loader2 size={40} className="text-tennis-green animate-spin" />}
        title={t("payLoading")}
      />
    );
  }

  if (isError || !data) {
    return (
      <StateCard
        tone="error"
        icon={<AlertTriangle size={40} className="text-amber-500" />}
        title={t("payLoadError")}
      />
    );
  }

  if (cashConfirmed) {
    return (
      <StateCard
        tone="success"
        icon={<CheckCircle2 size={40} className="text-emerald-500" />}
        title={t("payCashConfirmedTitle")}
        body={t("payCashConfirmedBody")}
      />
    );
  }

  return (
    <div className="bg-white rounded-2xl shadow-sm shadow-gray-100 overflow-hidden">
      <div className="bg-tennis-green/5 px-6 py-8 text-center">
        <h1 className="text-xl font-bold text-gray-900">{t("payTitle")}</h1>
        <p className="text-sm text-gray-500 mt-1">{t("payAmount")}</p>
        <p className="text-3xl font-bold text-tennis-green mt-2">
          {formatAmount(data.price)}
        </p>
      </div>

      <div className="p-6 space-y-3">
        {errors.length > 0 && (
          <div className="px-4 py-3 rounded-lg bg-red-50 border border-red-100">
            {errors.map((msg, i) => (
              <p key={i} className="text-red-600 text-sm">
                {msg}
              </p>
            ))}
          </div>
        )}

        {data.onlineAvailable && (
          <PaymentOption
            icon={<CreditCard size={20} className="text-tennis-green" />}
            title={t("payOnline")}
            hint={t("payOnlineHint")}
            actionLabel={t("payChoose")}
            loading={submitting === METHOD_ONLINE}
            disabled={submitting !== null}
            onClick={() => choose(METHOD_ONLINE)}
          />
        )}

        <PaymentOption
          icon={<Banknote size={20} className="text-tennis-green" />}
          title={t("payCash")}
          hint={data.onlineAvailable ? t("payCashHint") : t("payCashOnly")}
          actionLabel={t("payChoose")}
          loading={submitting === METHOD_CASH}
          disabled={submitting !== null}
          onClick={() => choose(METHOD_CASH)}
        />
      </div>
    </div>
  );
}

interface PaymentOptionProps {
  icon: React.ReactNode;
  title: string;
  hint: string;
  actionLabel: string;
  loading: boolean;
  disabled: boolean;
  onClick: () => void;
}

function PaymentOption({
  icon,
  title,
  hint,
  actionLabel,
  loading,
  disabled,
  onClick,
}: PaymentOptionProps) {
  return (
    <div className="border border-gray-200 rounded-xl p-4">
      <div className="flex items-start gap-3">
        <div className="w-10 h-10 rounded-full bg-tennis-green/10 flex items-center justify-center shrink-0">
          {icon}
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-gray-900">{title}</p>
          <p className="text-xs text-gray-500 mt-0.5 leading-relaxed">{hint}</p>
        </div>
      </div>
      <button
        type="button"
        onClick={onClick}
        disabled={disabled}
        className="mt-3 w-full bg-tennis-green text-white py-2.5 rounded-lg font-medium hover:bg-tennis-green/90 transition text-sm disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center gap-2"
      >
        {loading && <Loader2 size={16} className="animate-spin" />}
        {actionLabel}
      </button>
    </div>
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
}

function StateCard({ tone, icon, title, body }: StateCardProps) {
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
      </div>
    </div>
  );
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function formatAmount(amount: number): string {
  try {
    return new Intl.NumberFormat("nl-BE", {
      style: "currency",
      currency: "EUR",
    }).format(amount);
  } catch {
    return `€${amount.toFixed(2)}`;
  }
}
