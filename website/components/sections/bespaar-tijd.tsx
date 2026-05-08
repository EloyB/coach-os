import { CourtPattern } from "@/components/ui/court-pattern";
import { Mono } from "@/components/ui/mono";
import { BENEFITS } from "@/content/benefits";

export function BespaarTijd() {
  return (
    <section className="border-b border-rule">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="relative overflow-hidden rounded-2xl bg-ink p-10 text-white md:p-16">
          <div className="absolute inset-0">
            <CourtPattern />
          </div>
          <div className="relative max-w-3xl">
            <Mono className="text-[11px] tracking-[0.18em] text-tennis-lime">
              BESPAAR TIJD
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              {BENEFITS.heading}
            </h2>
            <p className="mt-4 text-base leading-relaxed text-white/80 md:text-lg">
              {BENEFITS.sub}
            </p>
          </div>

          <div className="relative mt-10 grid gap-6 md:grid-cols-3 md:gap-10">
            {BENEFITS.stats.map((stat) => (
              <div
                key={stat.label}
                className="border-t border-white/15 pt-5 md:border-l md:border-t-0 md:pl-6 md:pt-0 md:first:border-l-0 md:first:pl-0"
              >
                <Mono className="block text-3xl font-extrabold tracking-tight text-tennis-lime md:text-4xl">
                  {stat.value}
                </Mono>
                <Mono className="mt-3 block text-[11px] font-bold uppercase tracking-[0.18em] text-white/60">
                  {stat.label}
                </Mono>
              </div>
            ))}
          </div>

          <p className="relative mt-10 max-w-3xl text-sm text-white/60">
            {BENEFITS.closing}
          </p>
        </div>
      </div>
    </section>
  );
}
