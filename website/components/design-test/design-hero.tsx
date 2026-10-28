import Image from "next/image";
import { ArrowRight } from "lucide-react";
import { HERO } from "@/content/hero";

/** Hero met serif-kop op een zachte achtergrond; het dashboard loopt door tot onder de rand. */
export function DesignHero() {
  return (
    <section className="relative overflow-hidden bg-[linear-gradient(to_bottom,#e4f3b8_0%,#f1f6dc_45%,#fdfcf9_100%)] pt-36 md:pt-44">
      <div className="mx-auto max-w-4xl px-6 text-center">
        <span className="inline-flex items-center gap-2 rounded-full border border-white/80 bg-white/70 px-4 py-1.5 text-sm text-ink-2 shadow-sm backdrop-blur">
          <span className="h-2 w-2 rounded-full bg-tennis-green" />
          Voor tennis- en padelclubs in de Benelux
        </span>

        <h1 className="mt-8 text-5xl leading-[1.02] text-ink md:text-7xl">
          Lessenplanning,
          <br />
          eindelijk <em className="italic text-tennis-green">eenvoudig</em>
        </h1>

        <p className="mx-auto mt-7 max-w-2xl text-lg leading-relaxed text-ink-2">
          {HERO.body}
        </p>

        <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
          <a
            href={HERO.primaryCta.href}
            className="inline-flex h-12 items-center gap-2 rounded-xl bg-tennis-green px-6 text-sm font-medium text-white ring-2 ring-tennis-lime/70 transition-colors hover:bg-tennis-green/90"
          >
            {HERO.primaryCta.label}
            <ArrowRight className="h-4 w-4" />
          </a>
          <a
            href="#hoe-het-werkt"
            className="inline-flex h-12 items-center rounded-xl bg-white px-6 text-sm font-medium text-ink shadow-sm ring-1 ring-black/5 transition-colors hover:bg-white/80"
          >
            Hoe het werkt
          </a>
        </div>
      </div>

      {/* Alleen de bovenkant van de laptop; de rest valt buiten de sectie. */}
      <div className="mx-auto mt-16 max-w-5xl px-6">
        <div className="relative max-h-[22rem] overflow-hidden rounded-t-[1.25rem] border border-ink/80 bg-ink p-[0.6rem] pt-5 shadow-[0_30px_80px_-20px_rgba(22,21,19,0.4)] md:max-h-[34rem] md:p-3 md:pt-6">
          <span className="absolute left-1/2 top-1.5 h-1 w-1 -translate-x-1/2 rounded-full bg-white/25 md:top-2" />
          <div className="overflow-hidden rounded-t-md bg-paper">
            <Image
              src="/screenshots/dashboard.png"
              alt="Het CoachOS-dashboard met de lessen van vandaag en een maandkalender waarin lessenreeksen en kampen staan."
              width={2400}
              height={1359}
              quality={90}
              priority
              sizes="(min-width: 1024px) 960px, 100vw"
              className="block h-auto w-full"
            />
          </div>
        </div>
      </div>
    </section>
  );
}
