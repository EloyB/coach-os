import { ArrowRight } from "lucide-react";
import { InkHeroCard } from "@/components/ui/ink-hero-card";
import { StatStrip } from "@/components/ui/stat-strip";
import { Mono } from "@/components/ui/mono";
import { HERO } from "@/content/hero";
import { LessonWeekMock } from "@/components/sections/lesson-week-mock";

export function Hero() {
  return (
    <section className="border-b border-rule">
      <div className="mx-auto grid max-w-6xl gap-8 px-6 py-16 md:py-24 lg:grid-cols-[1.05fr_0.95fr] lg:gap-10">
        <InkHeroCard
          showCourtLines
          courtLinesOpacity={0.06}
          className="flex flex-col p-8 md:p-12"
        >
          <Mono className="text-[11px] tracking-[0.18em] text-tennis-lime">
            {HERO.eyebrow}
          </Mono>
          <h1 className="mt-5 text-4xl font-bold leading-[1.05] tracking-tight md:text-5xl">
            {HERO.heading}
          </h1>
          <p className="mt-5 text-lg font-semibold text-tennis-lime">
            {HERO.tagline}
          </p>
          <p className="mt-3 max-w-xl text-base leading-relaxed text-white/80">
            {HERO.body}
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-3">
            <a
              href={HERO.primaryCta.href}
              className="inline-flex h-11 items-center gap-2 rounded-md bg-tennis-lime px-5 text-sm font-semibold text-ink transition-colors hover:bg-tennis-lime/90"
            >
              {HERO.primaryCta.label}
              <ArrowRight className="h-4 w-4" />
            </a>
            <a
              href={HERO.secondaryCta.href}
              className="inline-flex h-11 items-center rounded-md border border-white/20 px-5 text-sm font-semibold text-white transition-colors hover:bg-white/[.06]"
            >
              {HERO.secondaryCta.label}
            </a>
          </div>

          <div className="mt-10 lg:mt-auto lg:pt-10">
            <StatStrip items={[...HERO.proof.items]} />
          </div>
        </InkHeroCard>

        <div className="hidden lg:block">
          <LessonWeekMock />
        </div>
      </div>
    </section>
  );
}
