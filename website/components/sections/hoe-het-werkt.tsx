import { Mono } from "@/components/ui/mono";
import { STEPS, STEPS_HEADING, STEPS_SUB } from "@/content/steps";
import { StepCard } from "@/components/sections/step-card";

export function HoeHetWerkt() {
  return (
    <section id="hoe-het-werkt" className="border-b border-rule">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            HOE HET WERKT
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {STEPS_HEADING}
          </h2>
          <p className="mt-3 text-base text-ink-2">{STEPS_SUB}</p>
        </div>

        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {STEPS.map((step) => (
            <StepCard key={step.num} {...step} />
          ))}
        </div>
      </div>
    </section>
  );
}
