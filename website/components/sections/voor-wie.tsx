import { Check } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { AUDIENCES, AUDIENCES_HEADING, AUDIENCES_SUB } from "@/content/audiences";

export function VoorWie() {
  return (
    <section id="voor-wie" className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            VOOR WIE
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {AUDIENCES_HEADING}
          </h2>
          <p className="mt-3 text-base text-ink-2">{AUDIENCES_SUB}</p>
        </div>

        <div className="mt-12 grid gap-5 md:grid-cols-2">
          {AUDIENCES.map((audience) => {
            const Icon = audience.icon;
            return (
              <div
                key={audience.title}
                className="group rounded-xl border border-rule bg-paper p-7 transition-colors hover:border-ink/20"
              >
                <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-tennis-lime/15 text-tennis-green">
                  <Icon className="h-5 w-5" strokeWidth={2.2} />
                </div>
                <h3 className="mt-5 text-xl font-bold tracking-tight">
                  {audience.title}
                </h3>
                <p className="mt-2 text-sm text-ink-2">{audience.body}</p>
                <ul className="mt-5 space-y-2.5">
                  {audience.bullets.map((b) => (
                    <li key={b} className="flex items-start gap-2.5 text-sm">
                      <Check
                        className="mt-0.5 h-4 w-4 flex-shrink-0 text-tennis-green"
                        strokeWidth={2.5}
                      />
                      <span className="text-ink-2">{b}</span>
                    </li>
                  ))}
                </ul>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
