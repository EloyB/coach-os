import { ArrowUpRight, Mail, Phone } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { CTA_SECTION } from "@/content/cta";
import { PILOT, PILOT_AVAILABLE_SEATS } from "@/content/pilot";

export function FinalCta() {
  const { call, email } = CTA_SECTION.tiles;

  return (
    <section id="contact" className="border-b border-rule">
      <div className="mx-auto max-w-5xl px-6 py-20 md:py-28">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            CONTACT
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {CTA_SECTION.heading}
          </h2>
          <p className="mt-3 text-base text-ink-2">{CTA_SECTION.sub}</p>
          {PILOT_AVAILABLE_SEATS > 0 ? (
            <p className="mt-4 inline-flex items-center gap-2 rounded-full border border-tennis-green/20 bg-tennis-green/5 px-3 py-1.5 text-xs font-semibold text-tennis-green">
              <span className="inline-block h-1.5 w-1.5 rounded-full bg-tennis-green" />
              Pilot-fase · nog {PILOT_AVAILABLE_SEATS} van {PILOT.totalSeats}{" "}
              plekken — gratis tijdens pilot, lifetime korting
            </p>
          ) : null}
        </div>

        <div className="mt-10 grid gap-4 md:grid-cols-2">
          <ContactTile
            icon={Phone}
            label={call.label}
            value={call.value}
            href={call.href}
            hint={call.hint}
          />
          <ContactTile
            icon={Mail}
            label={email.label}
            value={email.value}
            href={email.href}
            hint={email.hint}
          />
        </div>
      </div>
    </section>
  );
}

interface ContactTileProps {
  icon: LucideIcon;
  label: string;
  value: string;
  href: string;
  hint: string;
}

function ContactTile({
  icon: Icon,
  label,
  value,
  href,
  hint,
}: ContactTileProps) {
  return (
    <a
      href={href}
      className="group relative flex flex-col gap-6 rounded-2xl border border-rule bg-paper p-7 transition-colors hover:border-tennis-green hover:bg-tennis-green/5 md:p-9"
    >
      <div className="flex items-start justify-between">
        <span className="inline-flex h-11 w-11 items-center justify-center rounded-lg bg-tennis-green text-tennis-lime">
          <Icon className="h-5 w-5" strokeWidth={2.2} />
        </span>
        <ArrowUpRight
          className="h-5 w-5 text-ink-3 transition-all group-hover:-translate-y-0.5 group-hover:translate-x-0.5 group-hover:text-tennis-green"
          strokeWidth={2.2}
        />
      </div>

      <div>
        <Mono className="text-[10px] tracking-[0.18em] text-ink-3">
          {label.toUpperCase()}
        </Mono>
        <div className="mt-1 break-all text-xl font-bold tracking-tight text-ink md:text-2xl">
          {value}
        </div>
      </div>

      <div className="text-sm text-ink-3">{hint}</div>
    </a>
  );
}
