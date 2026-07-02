"use client";

import { useState } from "react";
import { ArrowRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import {
  PricingCard,
  type BillingPeriod,
} from "@/components/sections/pricing-card";
import {
  ANNUAL_MONTHLY_DISCOUNT,
  PRICING_HEADING,
  PRICING_SUB,
  PRICING_TIERS,
} from "@/content/pricing";

interface PricingProps {
  /** Hide the "Bekijk volledige prijslijst" link (e.g. when rendered ON the /prijzen page itself). */
  hideCompareLink?: boolean;
}

const PERIODS: { value: BillingPeriod; label: string; hint?: string }[] = [
  { value: "monthly", label: "Maandelijks" },
  { value: "yearly", label: "Jaarlijks", hint: `−€${ANNUAL_MONTHLY_DISCOUNT}/mnd` },
];

export function Pricing({ hideCompareLink = false }: PricingProps) {
  const [billing, setBilling] = useState<BillingPeriod>("monthly");

  return (
    <section id="prijzen" className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-28">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            TARIEVEN
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {PRICING_HEADING}
          </h2>
          <p className="mt-3 text-base text-ink-2">{PRICING_SUB}</p>
        </div>

        <div
          role="tablist"
          aria-label="Facturatieperiode"
          className="mt-8 inline-flex items-center gap-1 rounded-full border border-rule bg-paper p-1"
        >
          {PERIODS.map((period) => {
            const active = billing === period.value;
            return (
              <button
                key={period.value}
                type="button"
                role="tab"
                aria-selected={active}
                onClick={() => setBilling(period.value)}
                className={cn(
                  "inline-flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-semibold transition-colors",
                  active
                    ? "bg-tennis-green text-paper"
                    : "text-ink-2 hover:text-ink",
                )}
              >
                {period.label}
                {period.hint ? (
                  <span
                    className={cn(
                      "rounded-full px-1.5 py-0.5 text-[10px] font-bold",
                      active
                        ? "bg-tennis-lime text-ink"
                        : "bg-tennis-lime/30 text-tennis-green",
                    )}
                  >
                    {period.hint}
                  </span>
                ) : null}
              </button>
            );
          })}
        </div>

        <div className="mt-12 grid gap-6 lg:grid-cols-3">
          {PRICING_TIERS.map((tier) => (
            <PricingCard key={tier.id} tier={tier} billing={billing} />
          ))}
        </div>

        {hideCompareLink ? null : (
          <div className="mt-10 flex flex-wrap items-center justify-between gap-4 border-t border-rule pt-8">
            <p className="text-sm text-ink-2">
              Wil je per feature vergelijken? De volledige tarieven-pagina toont
              limieten, betalingen en ondersteuning per abonnement.
            </p>
            <a
              href="/prijzen"
              className="inline-flex items-center gap-2 text-sm font-semibold text-ink hover:text-tennis-green"
            >
              Bekijk volledige prijslijst
              <ArrowRight className="h-4 w-4" />
            </a>
          </div>
        )}
      </div>
    </section>
  );
}
