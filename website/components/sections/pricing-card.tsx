import { ArrowRight, Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import type { Billing, PricingTier } from "@/content/pricing";

interface PricingCardProps {
  tier: PricingTier;
  billing: Billing;
}

export function PricingCard({ tier, billing }: PricingCardProps) {
  const featured = tier.featured ?? false;
  const price = billing === "yearly" ? tier.priceYearly : tier.priceMonthly;
  const suffix = billing === "yearly" ? "/jaar" : "/maand";

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

      <h3
        className={cn(
          "text-xl font-bold tracking-tight",
          featured && "text-paper",
        )}
      >
        {tier.name}
      </h3>
      <p
        className={cn(
          "mt-2 text-sm leading-relaxed",
          featured ? "text-paper/80" : "text-ink-2",
        )}
      >
        {tier.tagline}
      </p>

      <div className="mt-7 flex items-baseline gap-1">
        <span className="text-4xl font-bold tracking-tight">€{price}</span>
        <span
          className={cn(
            "text-sm font-medium",
            featured ? "text-paper/70" : "text-ink-3",
          )}
        >
          {suffix}
        </span>
      </div>

      {/* Leerling-limiet, met het jaar-voordeel op een eigen regel eronder */}
      <Mono
        className={cn(
          "mt-1 block text-[11px] tracking-tight",
          featured ? "text-paper/60" : "text-ink-3",
        )}
      >
        {tier.studentLimit}
      </Mono>
      <div className="mt-2 min-h-[22px]">
        {billing === "yearly" ? (
          <span
            className={cn(
              "inline-flex whitespace-nowrap rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide",
              featured
                ? "bg-tennis-lime/25 text-tennis-lime"
                : "bg-tennis-green/10 text-tennis-green",
            )}
          >
            2 maanden gratis
          </span>
        ) : null}
      </div>

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

      <p
        className={cn(
          "mt-3 text-center text-[11px]",
          featured ? "text-paper/60" : "text-ink-3",
        )}
      >
        30 dagen gratis · geen betaalgegevens
      </p>

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
