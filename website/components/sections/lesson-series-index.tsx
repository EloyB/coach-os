import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { FinalCta } from "@/components/sections/final-cta";
import { LessonSeriesGrid } from "@/components/sections/lesson-series-grid";
import {
  LESSON_SERIES,
  LESSON_SERIES_DISCLAIMER,
  LESSON_SERIES_HEADING,
  LESSON_SERIES_SUB,
} from "@/content/lesson-series";

export function LessonSeriesIndex() {
  return (
    <>
      <SiteNav />
      <main>
        <section className="border-b border-rule">
          <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              LESSEN
            </Mono>
            <h1 className="mt-3 max-w-3xl text-4xl font-bold leading-[1.05] tracking-tight md:text-5xl">
              {LESSON_SERIES_HEADING}
            </h1>
            <p className="mt-5 max-w-2xl text-lg leading-relaxed text-ink-2">
              {LESSON_SERIES_SUB}
            </p>

            <p className="mt-8 inline-flex items-center rounded-md border border-warn/30 bg-warn/10 px-3 py-1.5 text-xs font-medium text-ink-2">
              <span className="mr-2 inline-block h-1.5 w-1.5 rounded-full bg-warn" />
              {LESSON_SERIES_DISCLAIMER}
            </p>
          </div>
        </section>

        <section className="border-b border-rule bg-canvas">
          <div className="mx-auto max-w-6xl px-6 py-16 md:py-20">
            <LessonSeriesGrid series={LESSON_SERIES} />
          </div>
        </section>

        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}
