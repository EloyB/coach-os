"use client";

import { useState } from "react";
import { ArrowRight, Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import {
  type Billing,
  type PricingTier,
  PRICING_HEADING,
  PRICING_ONBOARDING_NOTE,
  PRICING_SUB,
  PRICING_TIERS,
  PRICING_VAT_NOTE,
} from "@/content/pricing";

/**
 * Tarieven als bento-kaarten naast elkaar. Geen kaart wordt uitgelicht: de tint loopt
 * van licht naar donker mee met de grootte van het plan.
 */
export function BentoPricing({
  hideHeader = false,
  hideCompareLink = false,
}: {
  /** Verberg het tekstblok boven de kaarten, bv. op /prijzen waar de hero dat al dekt. */
  hideHeader?: boolean;
  /** Verberg de link naar /prijzen, bv. op die pagina zelf. */
  hideCompareLink?: boolean;
} = {}) {
  const [billing, setBilling] = useState<Billing>("yearly");

  return (
    <section id="prijzen" className="scroll-mt-16">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        {hideHeader ? null : (
          <div className="max-w-2xl">
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              TARIEVEN
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              {PRICING_HEADING}
            </h2>
            <p className="mt-3 text-base text-ink-2">{PRICING_SUB}</p>
            <p className="mt-4 flex items-start gap-2 rounded-xl bg-[#eef1e0] px-4 py-3 text-xs font-medium text-ink-2">
              <span className="mt-1 inline-block h-1.5 w-1.5 shrink-0 rounded-full bg-tennis-green" />
              {PRICING_ONBOARDING_NOTE}
            </p>
          </div>
        )}

        <div className={cn("flex items-center gap-3", !hideHeader && "mt-10")}>
          <BillingToggle billing={billing} onChange={setBilling} />
          <span className="text-xs font-medium text-ink-3">
            Jaarlijks = 2 maanden gratis
          </span>
        </div>

        <div className="mt-8 grid gap-4 md:grid-cols-3">
          {PRICING_TIERS.map((tier, i) => (
            <TierCard
              key={tier.id}
              tier={tier}
              billing={billing}
              tone={TONES[i] ?? TONES[0]}
            />
          ))}
        </div>

        <p className="mt-6 text-xs text-ink-3">{PRICING_VAT_NOTE}</p>

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

const TONES = [
  {
    card: "bg-[#eef1e0] text-ink",
    muted: "text-ink-2",
    faint: "text-ink-3",
    badge: "bg-tennis-green/10 text-tennis-green",
    check: "bg-tennis-green text-tennis-lime",
    button: "bg-ink text-white hover:bg-ink/90",
  },
  {
    card: "bg-[#2d5a16] text-white",
    muted: "text-white/80",
    faint: "text-white/60",
    badge: "bg-tennis-lime/25 text-tennis-lime",
    check: "bg-tennis-lime text-tennis-green",
    button: "bg-tennis-lime text-ink hover:bg-tennis-lime/90",
  },
  {
    card: "bg-[#1f3d0e] text-white",
    muted: "text-white/80",
    faint: "text-white/60",
    badge: "bg-tennis-lime/25 text-tennis-lime",
    check: "bg-tennis-lime text-tennis-green",
    button: "bg-tennis-lime text-ink hover:bg-tennis-lime/90",
  },
] as const;

type Tone = (typeof TONES)[number];

function TierCard({
  tier,
  billing,
  tone,
}: {
  tier: PricingTier;
  billing: Billing;
  tone: Tone;
}) {
  const price = billing === "yearly" ? tier.priceYearly : tier.priceMonthly;
  const suffix = billing === "yearly" ? "/jaar" : "/maand";

  return (
    <article className={cn("flex flex-col rounded-3xl p-7 md:p-8", tone.card)}>
      <h3 className="text-2xl font-bold tracking-tight">{tier.name}</h3>
      <p className={cn("mt-2 text-base font-medium", tone.muted)}>
        {tier.studentLimit}
      </p>

      <div className="mt-7 flex items-baseline gap-1">
        <span className="text-5xl font-bold tracking-tight">€{price}</span>
        <span className={cn("text-sm font-medium", tone.faint)}>{suffix}</span>
      </div>
      <p className={cn("mt-1 text-xs font-medium", tone.faint)}>
        {billing === "yearly"
          ? `of €${tier.priceMonthly} per maand`
          : `of €${tier.priceYearly} per jaar`}
      </p>

      <div className="mt-3 min-h-[22px]">
        {billing === "yearly" ? (
          <span
            className={cn(
              "inline-flex whitespace-nowrap rounded-full px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide",
              tone.badge,
            )}
          >
            2 maanden gratis
          </span>
        ) : null}
      </div>

      <a
        href={tier.cta.href}
        className={cn(
          "mt-6 inline-flex h-11 items-center justify-center gap-2 rounded-xl px-5 text-sm font-semibold transition-colors",
          tone.button,
        )}
      >
        {tier.cta.label}
        <ArrowRight className="h-4 w-4" />
      </a>
      <p className={cn("mt-3 text-center text-[11px]", tone.faint)}>
        Persoonlijke opstart · 30 dagen gratis
      </p>

      <ul className="mt-8 space-y-3">
        {tier.features.map((feature) => (
          <li key={feature} className={cn("flex gap-3 text-sm", tone.muted)}>
            <span
              className={cn(
                "mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full",
                tone.check,
              )}
            >
              <Check className="h-3 w-3" strokeWidth={3} />
            </span>
            {feature}
          </li>
        ))}
      </ul>
    </article>
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
      className="inline-flex rounded-xl bg-[#eef1e0] p-1"
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
              "rounded-lg px-4 py-1.5 text-sm font-semibold transition-colors",
              active
                ? "bg-tennis-green text-white"
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
