import { ArrowUpRight, Plus } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { FAQ, FAQ_HEADING, type FaqEntry } from "@/content/faq";

/** FAQ als bento: een kaart met de kop links en de vragen als losse kaarten rechts. */
export function BentoFaq({
  heading = FAQ_HEADING,
  entries = FAQ,
}: {
  heading?: string;
  entries?: readonly FaqEntry[];
} = {}) {
  return (
    <section id="faq" className="scroll-mt-16">
      <div className="mx-auto grid max-w-6xl gap-4 px-6 py-20 md:py-24 lg:grid-cols-[0.8fr_1.2fr]">
        <div className="rounded-3xl bg-[#1f3d0e] p-8 text-white md:p-10 lg:sticky lg:top-24 lg:self-start">
          <Mono className="text-[11px] tracking-[0.18em] text-tennis-lime">
            VRAGEN
          </Mono>
          <h2 className="mt-4 text-3xl font-bold leading-[1.1] tracking-tight md:text-4xl">
            {heading}
          </h2>
          <p className="mt-4 text-sm leading-relaxed text-white/70">
            Staat je vraag er niet bij? Stuur ons een bericht, we antwoorden
            binnen één werkdag.
          </p>
          <a
            href="#contact"
            className="mt-8 inline-flex items-center gap-1.5 text-sm font-semibold text-tennis-lime"
          >
            Neem contact op
            <ArrowUpRight className="h-4 w-4" />
          </a>
        </div>

        <div className="space-y-3">
          {entries.map((entry) => (
            <details
              key={entry.q}
              className="group rounded-2xl bg-[#eef1e0] px-6 transition-colors open:bg-[#e4e8d2]"
            >
              <summary className="flex cursor-pointer list-none items-center justify-between gap-4 py-5 text-left [&::-webkit-details-marker]:hidden">
                <h3 className="text-base font-semibold tracking-tight text-ink md:text-lg">
                  {entry.q}
                </h3>
                <span className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-white text-tennis-green">
                  <Plus
                    className="h-4 w-4 transition-transform duration-200 group-open:rotate-45"
                    strokeWidth={2.2}
                  />
                </span>
              </summary>
              <div className="pb-6 pr-12 text-sm leading-relaxed text-ink-2 md:text-base">
                {entry.a}
              </div>
            </details>
          ))}
        </div>
      </div>
    </section>
  );
}
