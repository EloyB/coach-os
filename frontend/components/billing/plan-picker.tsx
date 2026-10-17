"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { AlertTriangle, Check } from "lucide-react";
import type { BillingOverview, CheckoutRequest } from "@/lib/api/billing";
import {
  canPayByBankTransfer,
  exceedsLimit,
  formatEuro,
  grossAmount,
  planPrice,
  suggestPlan,
  type BillingIntervalName,
  type PlanOption,
} from "@/lib/billing";
import { btnPrimary } from "@/lib/styles";

interface PlanPickerProps {
  overview: BillingOverview;
  pending: boolean;
  onCheckout: (request: CheckoutRequest) => void;
}

type MethodName = "Mollie" | "BankTransfer";

/** Plan, maand/jaar en betaalwijze kiezen, met voorstel op basis van het aantal leerlingen. */
export function PlanPicker({ overview, pending, onCheckout }: PlanPickerProps) {
  const t = useTranslations("billing");
  const suggested = suggestPlan(overview.plans, overview.activeStudentCount);
  const [interval, setInterval] = useState<BillingIntervalName>("Yearly");
  const [selected, setSelected] = useState<PlanOption>(suggested);
  const [method, setMethod] = useState<MethodName>("Mollie");

  const bankTransferAllowed = canPayByBankTransfer(interval);
  const effectiveMethod: MethodName = bankTransferAllowed ? method : "Mollie";
  const trialRunning =
    overview.status === "Trialing" &&
    overview.trialEndsAt !== null &&
    overview.trialEndsAt >= new Date().toISOString().slice(0, 10);

  function submit() {
    onCheckout({
      plan: selected.plan,
      interval: interval === "Yearly" ? 2 : 1,
      method: effectiveMethod === "BankTransfer" ? 2 : 1,
    });
  }

  return (
    <div className="p-5 space-y-5">
      <p className="text-xs text-gray-500">
        {t("activeStudents", { count: overview.activeStudentCount })}
      </p>

      <div className="inline-flex rounded-lg border border-gray-200 p-0.5 bg-gray-50" role="radiogroup">
        {(["Monthly", "Yearly"] as const).map((value) => (
          <button
            key={value}
            type="button"
            role="radio"
            aria-checked={interval === value}
            onClick={() => setInterval(value)}
            className={`px-3 py-1.5 text-xs font-semibold rounded-md transition-colors cursor-pointer ${
              interval === value ? "bg-white text-tennis-green shadow-sm" : "text-gray-500"
            }`}
          >
            {value === "Monthly" ? t("monthly") : t("yearly")}
            {value === "Yearly" && (
              <span className="ml-1.5 text-[10px] font-semibold text-tennis-green">{t("yearlyHint")}</span>
            )}
          </button>
        ))}
      </div>

      <div className="grid gap-3 sm:grid-cols-3" role="radiogroup">
        {overview.plans.map((plan) => {
          const isSelected = selected.plan === plan.plan;
          const net = planPrice(plan, interval);
          return (
            <button
              key={plan.plan}
              type="button"
              role="radio"
              aria-checked={isSelected}
              onClick={() => setSelected(plan)}
              className={`relative text-left rounded-xl border p-4 transition-colors cursor-pointer ${
                isSelected ? "border-tennis-green ring-2 ring-tennis-green/20 bg-tennis-green/[0.03]" : "border-gray-200 hover:border-gray-300"
              }`}
            >
              {plan.plan === suggested.plan && (
                <span className="absolute -top-2 right-3 text-[10px] font-semibold uppercase tracking-wide bg-tennis-lime text-ink px-2 py-0.5 rounded-full">
                  {t("suggested")}
                </span>
              )}
              <p className="text-sm font-bold text-gray-900 flex items-center gap-1.5">
                {isSelected && <Check size={14} className="text-tennis-green" />}
                {plan.name}
              </p>
              <p className="text-lg font-bold text-gray-900 mt-1">
                {formatEuro(net)}
                <span className="text-xs font-normal text-gray-400 ml-1">
                  {interval === "Yearly" ? t("perYear") : t("perMonth")}
                </span>
              </p>
              <p className="text-[11px] text-gray-400">{t("inclVat", { amount: formatEuro(grossAmount(net)) })}</p>
              <p className="text-xs text-gray-600 mt-2">{t("studentLimit", { count: plan.studentLimit })}</p>
            </button>
          );
        })}
      </div>

      {exceedsLimit(selected, overview.activeStudentCount) && (
        <p className="flex gap-2 text-xs text-amber-800 bg-amber-50 border border-amber-200 rounded-lg p-3">
          <AlertTriangle size={14} className="shrink-0 mt-0.5" />
          {t("exceedsWarning", { limit: selected.studentLimit, price: overview.overagePricePerStudent })}
        </p>
      )}

      <fieldset className="space-y-2">
        <legend className="text-xs font-semibold text-gray-700 mb-1">{t("methodTitle")}</legend>
        {(["Mollie", "BankTransfer"] as const).map((value) => {
          const disabled = value === "BankTransfer" && !bankTransferAllowed;
          return (
            <label
              key={value}
              className={`flex items-center gap-2 text-sm ${disabled ? "text-gray-300 cursor-not-allowed" : "text-gray-700 cursor-pointer"}`}
            >
              <input
                type="radio"
                name="billing-method"
                value={value}
                checked={effectiveMethod === value}
                disabled={disabled}
                onChange={() => setMethod(value)}
                className="accent-tennis-green"
              />
              {value === "Mollie" ? t("methodMollie") : t("methodBankTransfer")}
            </label>
          );
        })}
        {!bankTransferAllowed && <p className="text-[11px] text-gray-400">{t("bankTransferYearlyOnly")}</p>}
      </fieldset>

      {overview.profile === null && <p className="text-xs text-amber-700">{t("checkoutNeedsProfile")}</p>}
      {trialRunning && <p className="text-xs text-gray-500">{t("checkoutStartNote")}</p>}

      <button
        type="button"
        onClick={submit}
        disabled={pending || overview.profile === null}
        className={btnPrimary}
      >
        {pending
          ? t("checkoutRedirecting")
          : effectiveMethod === "BankTransfer"
            ? t("checkoutBankTransfer")
            : t("checkoutMollie")}
      </button>
    </div>
  );
}
