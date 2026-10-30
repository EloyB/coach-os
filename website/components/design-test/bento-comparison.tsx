import { Check, X } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import {
  COMPARISON,
  COMPARISON_HEADING,
  COMPARISON_KICKER,
  COMPARISON_SUB,
  type ComparisonRow,
} from "@/content/comparison";

/** Excel vs CoachOS als bento: per taak een kaart met "Excel" en "CoachOS" als twee vlakken. */
export function BentoComparison() {
  return (
    <section id="excel-vs-coachos" className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            {COMPARISON_KICKER}
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {COMPARISON_HEADING}
          </h2>
          <p className="mt-3 text-base text-ink-2">{COMPARISON_SUB}</p>
        </div>

        <div className="mt-12 grid gap-4 lg:grid-cols-6">
          {COMPARISON.map((row, i) => (
            <ComparisonCard
              key={row.task}
              row={row}
              className={i < 2 ? "lg:col-span-3" : "lg:col-span-2"}
            />
          ))}
        </div>

        <p className="mt-10 max-w-2xl text-sm text-ink-3">
          Excel werkt prima tot je rond de 50 leerlingen zit. Daarboven worden
          de kleine pijnpunten het seizoen lang het echte werk. CoachOS vervangt
          die routine zonder dat je nieuwe kennis moet opbouwen. De taken
          blijven dezelfde, alleen de uitvoering verschuift naar het systeem.
        </p>
      </div>
    </section>
  );
}

function ComparisonCard({
  row,
  className = "",
}: {
  row: ComparisonRow;
  className?: string;
}) {
  return (
    <article
      className={`flex flex-col rounded-3xl bg-[#e4e8d2] p-3 ${className}`}
    >
      <h3 className="px-4 pb-3 pt-4 text-lg font-bold leading-snug tracking-tight text-ink md:text-xl">
        {row.task}
      </h3>

      <div className="rounded-2xl bg-white p-5">
        <Mono className="inline-flex items-center gap-1.5 text-[10px] tracking-[0.18em] text-ink-3">
          <X className="h-3 w-3 text-urgent" strokeWidth={3} />
          EXCEL
        </Mono>
        <p className="mt-2 text-sm leading-relaxed text-ink-2">{row.excel}</p>
      </div>

      <div className="mt-2 flex-1 rounded-2xl bg-[#1f3d0e] p-5 text-white">
        <Mono className="inline-flex items-center gap-1.5 text-[10px] tracking-[0.18em] text-tennis-lime">
          <Check className="h-3 w-3" strokeWidth={3} />
          COACHOS
        </Mono>
        <p className="mt-2 text-sm leading-relaxed text-white/90">
          {row.coachos}
        </p>
      </div>
    </article>
  );
}
