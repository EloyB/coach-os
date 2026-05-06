import { ArrowRight } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { PricingCard } from "@/components/sections/pricing-card";
import {
  PRICING_DISCLAIMER,
  PRICING_HEADING,
  PRICING_SUB,
  PRICING_TIERS,
} from "@/content/pricing";

interface PricingProps {
  /** Hide the "Bekijk volledige prijslijst" link (e.g. when rendered ON the /prijzen page itself). */
  hideCompareLink?: boolean;
}

export function Pricing({ hideCompareLink = false }: PricingProps) {
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
          <p className="mt-4 inline-flex items-center rounded-md border border-warn/30 bg-warn/10 px-3 py-1.5 text-xs font-medium text-ink-2">
            <span className="mr-2 inline-block h-1.5 w-1.5 rounded-full bg-warn" />
            {PRICING_DISCLAIMER}
          </p>
        </div>

        <div className="mt-12 grid gap-6 lg:grid-cols-3">
          {PRICING_TIERS.map((tier) => (
            <PricingCard key={tier.id} tier={tier} />
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
