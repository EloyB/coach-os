import { Mono } from "@/components/ui/mono";
import { FAQ, FAQ_HEADING } from "@/content/faq";
import { FaqItem } from "@/components/sections/faq-item";

export function Faq() {
  return (
    <section id="faq" className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-3xl px-6 py-20 md:py-24">
        <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
          VRAGEN
        </Mono>
        <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
          {FAQ_HEADING}
        </h2>

        <div className="mt-10 rounded-xl border border-rule bg-paper px-6 md:px-8">
          {FAQ.map((entry) => (
            <FaqItem key={entry.q} {...entry} />
          ))}
        </div>
      </div>
    </section>
  );
}
