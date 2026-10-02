"use client";

import { useState } from "react";
import { ArrowRight, Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import { PricingCard } from "@/components/sections/pricing-card";
import {
  type Billing,
  PRICING_HEADING,
  PRICING_INCLUDED_NOTE,
  PRICING_SUB,
  PRICING_TIERS,
  PRICING_TRIAL,
  PRICING_VAT_NOTE,
} from "@/content/pricing";

interface PricingProps {
  /** Verberg de "Bekijk volledige prijslijst"-link (bv. op de /prijzen-pagina zelf). */
  hideCompareLink?: boolean;
}

export function Pricing({ hideCompareLink = false }: PricingProps) {
  const [billing, setBilling] = useState<Billing>("yearly");

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
          <p className="mt-4 inline-flex items-center rounded-md border border-tennis-green/25 bg-tennis-green/5 px-3 py-1.5 text-xs font-medium text-ink-2">
            <span className="mr-2 inline-block h-1.5 w-1.5 rounded-full bg-tennis-green" />
            {PRICING_TRIAL}
          </p>
        </div>

        {/* Maand / jaar toggle */}
        <div className="mt-10 flex items-center gap-3">
          <BillingToggle billing={billing} onChange={setBilling} />
          <span className="text-xs font-medium text-ink-3">
            Jaarlijks = 2 maanden gratis
          </span>
        </div>

        <div className="mt-8 grid gap-6 md:grid-cols-3">
          {PRICING_TIERS.map((tier) => (
            <PricingCard key={tier.id} tier={tier} billing={billing} />
          ))}
        </div>

        <div className="mt-6 flex items-start gap-3 rounded-xl border border-tennis-green/20 bg-tennis-green/5 px-5 py-4">
          <span className="mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-tennis-green text-tennis-lime">
            <Check className="h-3 w-3" strokeWidth={3} />
          </span>
          <p className="text-sm font-medium text-ink-2">
            {PRICING_INCLUDED_NOTE}
          </p>
        </div>

        <p className="mt-4 text-xs text-ink-3">{PRICING_VAT_NOTE}</p>

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

function BillingToggle({
  billing,
  onChange,
}: {
  billing: Billing;
  onChange: (b: Billing) => void;
}) {
  return (
    <div
      role="tablist"
      aria-label="Facturatieperiode"
      className="inline-flex rounded-lg border border-rule bg-paper p-1"
    >
      {(
        [
          { id: "monthly", label: "Maandelijks" },
          { id: "yearly", label: "Jaarlijks" },
        ] as const
      ).map((opt) => {
        const active = billing === opt.id;
        return (
          <button
            key={opt.id}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() => onChange(opt.id)}
            className={cn(
              "rounded-md px-4 py-1.5 text-sm font-semibold transition-colors",
              active
                ? "bg-tennis-green text-paper"
                : "text-ink-2 hover:text-ink",
            )}
          >
            {opt.label}
          </button>
        );
      })}
    </div>
  );
}
