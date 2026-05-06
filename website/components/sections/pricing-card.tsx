import { ArrowRight, Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import type { PricingTier } from "@/content/pricing";

interface PricingCardProps {
  tier: PricingTier;
}

export function PricingCard({ tier }: PricingCardProps) {
  const featured = tier.featured ?? false;

  return (
    <div
      className={cn(
        "relative flex flex-col rounded-2xl border p-8 transition-colors",
        featured
          ? "border-tennis-green bg-tennis-green text-paper shadow-[0_30px_80px_-30px_rgba(45,80,22,0.45)]"
          : "border-rule bg-paper text-ink hover:border-ink/20",
      )}
    >
      {featured ? (
        <span className="absolute -top-3 left-8 inline-flex items-center rounded-full bg-tennis-lime px-3 py-1 text-[11px] font-bold uppercase tracking-[0.18em] text-ink">
          Populair
        </span>
      ) : null}

      <div className="flex items-baseline justify-between gap-3">
        <h3 className={cn("text-xl font-bold tracking-tight", featured && "text-paper")}>
          {tier.name}
        </h3>
      </div>
      <p
        className={cn(
          "mt-2 text-sm leading-relaxed",
          featured ? "text-paper/80" : "text-ink-2",
        )}
      >
        {tier.tagline}
      </p>

      <div className="mt-7 flex items-baseline gap-1">
        {tier.priceMonthly !== null ? (
          <>
            <span className="text-4xl font-bold tracking-tight">
              €{tier.priceMonthly}
            </span>
            <span
              className={cn(
                "text-sm font-medium",
                featured ? "text-paper/70" : "text-ink-3",
              )}
            >
              {tier.priceSuffix}
            </span>
          </>
        ) : (
          <span className="text-3xl font-bold tracking-tight">
            {tier.priceSuffix}
          </span>
        )}
      </div>
      <Mono
        className={cn(
          "mt-1 text-[11px] tracking-tight",
          featured ? "text-paper/60" : "text-ink-3",
        )}
      >
        {tier.priceHelper}
      </Mono>

      <a
        href={tier.cta.href}
        className={cn(
          "mt-6 inline-flex h-11 items-center justify-center gap-2 rounded-md px-5 text-sm font-semibold transition-colors",
          featured
            ? "bg-tennis-lime text-ink hover:bg-tennis-lime/90"
            : "bg-ink text-paper hover:bg-ink/90",
        )}
      >
        {tier.cta.label}
        <ArrowRight className="h-4 w-4" />
      </a>

      <ul className="mt-8 space-y-3">
        {tier.features.map((f) => (
          <li
            key={f}
            className={cn(
              "flex gap-3 text-sm",
              featured ? "text-paper/90" : "text-ink-2",
            )}
          >
            <span
              className={cn(
                "mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full",
                featured
                  ? "bg-tennis-lime/30 text-tennis-lime"
                  : "bg-tennis-lime/30 text-tennis-green",
              )}
            >
              <Check className="h-3 w-3" strokeWidth={3} />
            </span>
            <span>{f}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
