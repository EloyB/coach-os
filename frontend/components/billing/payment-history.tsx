"use client";

import { useTranslations } from "next-intl";
import { FileText } from "lucide-react";
import type { SubscriptionPayment } from "@/lib/api/billing";
import { formatEuro } from "@/lib/billing";
import { formatDateNL } from "@/lib/date-utils";

const STATUS_STYLES: Record<SubscriptionPayment["status"], string> = {
  Open: "bg-amber-50 text-amber-800",
  Paid: "bg-tennis-green/10 text-tennis-green",
  Failed: "bg-red-50 text-red-700",
  Canceled: "bg-gray-100 text-gray-500",
  Expired: "bg-gray-100 text-gray-500",
  Refunded: "bg-gray-100 text-gray-500",
};

/** Betalingen (nieuwste eerst) met status, factuur en een betaallink voor open Mollie-betalingen. */
export function PaymentHistory({ payments }: { payments: SubscriptionPayment[] }) {
  const t = useTranslations("billing");

  if (payments.length === 0) {
    return <p className="p-5 text-sm text-gray-400">{t("historyEmpty")}</p>;
  }

  return (
    <ul className="divide-y divide-gray-100">
      {payments.map((payment) => (
        <li key={payment.id} className="px-5 py-3 flex flex-wrap items-center gap-x-4 gap-y-1.5">
          <div className="flex-1 min-w-[180px]">
            <p className="text-sm font-semibold text-gray-800">
              {payment.plan} · {formatEuro(payment.amountInclVat)}
            </p>
            <p className="text-xs text-gray-400">
              {t("historyPeriod", { start: formatDateNL(payment.periodStart), end: formatDateNL(payment.periodEnd) })}
              {payment.status === "Open" && payment.dueDate && (
                <> · {t("historyDue", { date: formatDateNL(payment.dueDate) })}</>
              )}
            </p>
          </div>
          <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full ${STATUS_STYLES[payment.status]}`}>
            {t(`paymentStatus${payment.status}`)}
          </span>
          {payment.invoiceNumber ? (
            payment.invoicePdfUrl ? (
              <a
                href={payment.invoicePdfUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 text-xs font-semibold text-tennis-green hover:underline"
              >
                <FileText size={12} />
                {t("historyInvoice", { number: payment.invoiceNumber })}
              </a>
            ) : (
              <span className="text-xs text-gray-500">{t("historyInvoice", { number: payment.invoiceNumber })}</span>
            )
          ) : (
            payment.invoiceStatus === "Pending" && <span className="text-xs text-gray-400">{t("historyInvoicePending")}</span>
          )}
          {payment.checkoutUrl && (
            <a href={payment.checkoutUrl} className="text-xs font-semibold text-white bg-ink px-3 py-1.5 rounded hover:bg-ink/90">
              {t("historyPay")}
            </a>
          )}
        </li>
      ))}
    </ul>
  );
}
