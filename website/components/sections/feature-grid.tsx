import { Mono } from "@/components/ui/mono";
import { FEATURES, FEATURES_HEADING, FEATURES_SUB } from "@/content/features";
import { FeatureCard } from "@/components/sections/feature-card";

export function FeatureGrid() {
  return (
    <section id="features" className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            FEATURES
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {FEATURES_HEADING}
          </h2>
          <p className="mt-3 text-base text-ink-2">{FEATURES_SUB}</p>
        </div>

        <div className="mt-12 grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {FEATURES.map((feature) => (
            <FeatureCard key={feature.title} {...feature} />
          ))}
        </div>
      </div>
    </section>
  );
}
