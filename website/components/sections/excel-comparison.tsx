import { Check, X } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import {
  COMPARISON,
  COMPARISON_HEADING,
  COMPARISON_KICKER,
  COMPARISON_SUB,
} from "@/content/comparison";

export function ExcelComparison() {
  return (
    <section
      id="excel-vs-coachos"
      className="border-b border-rule bg-paper"
    >
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

        <div className="mt-12 space-y-3">
          {COMPARISON.map((row) => (
            <ComparisonCard key={row.task} row={row} />
          ))}
        </div>

        <p className="mt-10 max-w-2xl text-sm text-ink-3">
          Excel werkt prima tot je rond de 50 leerlingen zit. Daarboven worden
          de kleine pijnpunten het seizoen lang het echte werk. CoachOS
          vervangt die routine zonder dat je nieuwe kennis moet opbouwen — de
          taken blijven dezelfde, alleen de uitvoering verschuift naar het
          systeem.
        </p>
      </div>
    </section>
  );
}

function ComparisonCard({
  row,
}: {
  row: (typeof COMPARISON)[number];
}) {
  return (
    <div className="overflow-hidden rounded-xl border border-rule">
      {/* Task heading bar */}
      <div className="border-b border-rule bg-canvas px-5 py-3">
        <h3 className="text-sm font-bold tracking-tight text-ink md:text-base">
          {row.task}
        </h3>
      </div>

      <div className="grid md:grid-cols-2">
        {/* Excel side */}
        <div className="border-b border-rule bg-paper px-5 py-5 md:border-b-0 md:border-r">
          <Mono className="inline-flex items-center gap-1.5 text-[10px] tracking-[0.18em] text-ink-3">
            <X className="h-3 w-3 text-urgent" strokeWidth={3} />
            EXCEL
          </Mono>
          <p className="mt-2 text-sm leading-relaxed text-ink-2">{row.excel}</p>
        </div>

        {/* CoachOS side */}
        <div className="bg-tennis-lime/10 px-5 py-5">
          <Mono className="inline-flex items-center gap-1.5 text-[10px] tracking-[0.18em] text-tennis-green">
            <Check className="h-3 w-3 text-tennis-green" strokeWidth={3} />
            COACHOS
          </Mono>
          <p className="mt-2 text-sm leading-relaxed text-ink">
            {row.coachos}
          </p>
        </div>
      </div>
    </div>
  );
}
