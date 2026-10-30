import { ArrowRight } from "lucide-react";
import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";
import { BENEFITS } from "@/content/benefits";
import { HERO } from "@/content/hero";

/** Hero als mini-bento: grote kaart met de kop, rechts een planningsfragment en een cijfer. */
export function BentoHero() {
  const [time, excel] = [BENEFITS.stats[0], BENEFITS.stats[1]];
  return (
    <section>
      <div className="mx-auto grid max-w-6xl gap-4 px-6 py-16 md:py-24 lg:grid-cols-3 lg:grid-rows-2">
        <div className="relative overflow-hidden rounded-3xl bg-[#1f3d0e] p-8 text-white md:p-12 lg:col-span-2 lg:row-span-2">
          <CourtLines opacity={0.07} />
          <div className="relative flex h-full flex-col">
            <Mono className="text-[11px] tracking-[0.18em] text-tennis-lime">
              {HERO.eyebrow}
            </Mono>
            <h1 className="mt-5 text-4xl font-bold leading-[1.05] tracking-tight md:text-5xl">
              {HERO.heading}
            </h1>
            <p className="mt-5 text-lg font-semibold text-tennis-lime">
              {HERO.tagline}
            </p>
            <p className="mt-3 max-w-xl text-base leading-relaxed text-white/75">
              {HERO.body}
            </p>
            <div className="mt-auto pt-10">
              <a
                href={HERO.primaryCta.href}
                className="inline-flex h-11 items-center gap-2 rounded-lg bg-tennis-lime px-5 text-sm font-semibold text-ink transition-colors hover:bg-tennis-lime/90"
              >
                {HERO.primaryCta.label}
                <ArrowRight className="h-4 w-4" />
              </a>
            </div>
          </div>
        </div>

        <PlanningFragmentCard />

        <div className="relative flex min-h-[13rem] flex-col justify-between overflow-hidden rounded-3xl bg-tennis-lime p-7 text-ink md:p-8">
          <Mono className="text-[11px] tracking-[0.18em] text-tennis-green">
            BESPAAR TIJD
          </Mono>
          <div>
            <p className="text-5xl font-bold tracking-tight md:text-6xl">
              {time.value}
            </p>
            <p className="mt-1 text-sm font-medium text-tennis-green">
              {time.label}
            </p>
            <p className="mt-4 text-sm text-ink/70">
              {excel.value} {excel.label.toLowerCase()}
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

function PlanningFragmentCard() {
  const slots = [
    { time: "18:00", name: "Beginners", on: true },
    { time: "14:00", name: "Mini's", on: false },
  ];
  return (
    <div className="relative min-h-[16rem] overflow-hidden rounded-3xl bg-[#eef1e0] p-7 md:p-8">
      <p className="relative z-10 text-2xl font-bold leading-[1.1] tracking-tight">
        Eén klik en het is gepland
      </p>
      <div
        aria-hidden
        className="absolute -bottom-5 left-6 right-6 rounded-2xl bg-white p-4 shadow-xl"
      >
        <div className="grid grid-cols-2 gap-2">
          {slots.map((slot) => (
            <div
              key={slot.name}
              className={`rounded-xl p-3 ${slot.on ? "bg-tennis-lime" : "bg-[#f3f2ee]"}`}
            >
              <Mono className="text-[11px] font-semibold text-ink">
                {slot.time}
              </Mono>
              <p className="text-xs text-ink-2">{slot.name}</p>
              <div className="mt-2 flex -space-x-1.5">
                <span className="h-5 w-5 rounded-full border-2 border-white bg-violet-500" />
                <span className="h-5 w-5 rounded-full border-2 border-white bg-sky-500" />
              </div>
            </div>
          ))}
        </div>
        <div className="mt-3 flex items-center justify-between rounded-xl bg-ink px-4 py-2.5">
          <span className="text-xs text-white/70">Voorkeuren matchen</span>
          <span className="rounded-lg bg-tennis-lime px-3 py-1 text-xs font-semibold text-ink">
            Plannen
          </span>
        </div>
      </div>
    </div>
  );
}
