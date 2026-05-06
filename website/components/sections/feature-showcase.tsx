import { Mono } from "@/components/ui/mono";
import { ShowcaseRow } from "@/components/sections/showcase-row";
import { SHOWCASE, SHOWCASE_HEADING, SHOWCASE_SUB } from "@/content/showcase";

export function FeatureShowcase() {
  return (
    <section id="showcase" className="border-b border-rule bg-paper">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-28">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            HOE HET ERUITZIET
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {SHOWCASE_HEADING}
          </h2>
          <p className="mt-3 text-base text-ink-2">{SHOWCASE_SUB}</p>
        </div>

        <div className="mt-16 space-y-24 md:mt-20 md:space-y-32">
          {SHOWCASE.map((item, i) => (
            <ShowcaseRow key={item.id} item={item} reverse={i % 2 === 1} />
          ))}
        </div>
      </div>
    </section>
  );
}
