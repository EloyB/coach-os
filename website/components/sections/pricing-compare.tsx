import { Check, Minus } from "lucide-react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import { PRICING_COMPARE, PRICING_TIERS } from "@/content/pricing";

export function PricingCompare() {
  return (
    <section className="border-b border-rule bg-paper">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            VERGELIJKING
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            Welk abonnement past bij jouw school?
          </h2>
          <p className="mt-3 text-base text-ink-2">
            Alle limieten en functies per tier op één plek.
          </p>
        </div>

        <div className="mt-10 overflow-x-auto rounded-2xl border border-rule">
          <table className="w-full min-w-[640px] border-collapse text-sm">
            <thead>
              <tr className="border-b border-rule bg-canvas">
                <th
                  scope="col"
                  className="w-2/5 px-6 py-4 text-left text-xs font-semibold uppercase tracking-[0.12em] text-ink-3"
                >
                  Functie
                </th>
                {PRICING_TIERS.map((tier) => (
                  <th
                    key={tier.id}
                    scope="col"
                    className={cn(
                      "px-6 py-4 text-left text-sm font-bold tracking-tight",
                      tier.featured && "bg-tennis-green/5 text-tennis-green",
                    )}
                  >
                    {tier.name}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {PRICING_COMPARE.map((group) => (
                <CompareGroup key={group.label} group={group} />
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}

function CompareGroup({
  group,
}: {
  group: (typeof PRICING_COMPARE)[number];
}) {
  return (
    <>
      <tr>
        <th
          scope="colgroup"
          colSpan={1 + PRICING_TIERS.length}
          className="border-b border-t border-rule bg-canvas/60 px-6 py-2.5 text-left text-[11px] font-semibold uppercase tracking-[0.18em] text-ink-3"
        >
          {group.label}
        </th>
      </tr>
      {group.rows.map((row) => (
        <tr key={row.feature} className="border-b border-rule last:border-b-0">
          <th
            scope="row"
            className="px-6 py-4 text-left font-medium text-ink"
          >
            {row.feature}
          </th>
          {PRICING_TIERS.map((tier) => {
            const value = row.values[tier.id];
            return (
              <td
                key={tier.id}
                className={cn(
                  "px-6 py-4",
                  tier.featured && "bg-tennis-green/5",
                )}
              >
                <CompareCell value={value} />
              </td>
            );
          })}
        </tr>
      ))}
    </>
  );
}

function CompareCell({ value }: { value: boolean | string | undefined }) {
  if (value === true) {
    return (
      <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-tennis-lime/30 text-tennis-green">
        <Check className="h-3.5 w-3.5" strokeWidth={3} />
      </span>
    );
  }
  if (value === false || value === undefined) {
    return (
      <span className="inline-flex h-6 w-6 items-center justify-center text-ink-3">
        <Minus className="h-4 w-4" />
      </span>
    );
  }
  return <span className="text-sm text-ink-2">{value}</span>;
}
