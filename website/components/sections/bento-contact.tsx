import { ArrowUpRight, Mail, Phone } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";
import { CTA_SECTION } from "@/content/cta";

/** Contact als bento: een grote kaart met de oproep en twee klikbare tegels voor bellen en mailen. */
export function BentoContact() {
  const { call, email } = CTA_SECTION.tiles;

  return (
    <section id="contact" className="scroll-mt-16">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="grid gap-4 lg:grid-cols-3 lg:grid-rows-2">
          <div className="relative overflow-hidden rounded-3xl bg-[#1f3d0e] p-8 text-white md:p-12 lg:col-span-2 lg:row-span-2">
            <CourtLines opacity={0.07} />
            <div className="relative flex h-full flex-col justify-between gap-12">
              <div>
                <Mono className="text-[11px] tracking-[0.18em] text-tennis-lime">
                  CONTACT
                </Mono>
                <h2 className="mt-4 max-w-xl text-3xl font-bold leading-[1.1] tracking-tight md:text-5xl">
                  {CTA_SECTION.heading}
                </h2>
                <p className="mt-5 max-w-md text-base leading-relaxed text-white/75">
                  {CTA_SECTION.sub}
                </p>
              </div>
              <a
                href={email.href}
                className="inline-flex h-11 w-fit items-center gap-2 rounded-lg bg-tennis-lime px-5 text-sm font-semibold text-ink transition-colors hover:bg-tennis-lime/90"
              >
                Boek een demo
                <ArrowUpRight className="h-4 w-4" />
              </a>
            </div>
          </div>

          <ContactTile
            icon={Phone}
            label={call.label}
            value={call.value}
            href={call.href}
            hint={call.hint}
            className="bg-tennis-lime text-ink"
            muted="text-ink/70"
            iconClass="bg-tennis-green text-tennis-lime"
          />
          <ContactTile
            icon={Mail}
            label={email.label}
            value={email.value}
            href={email.href}
            hint={email.hint}
            className="bg-[#eef1e0] text-ink"
            muted="text-ink-3"
            iconClass="bg-tennis-green text-tennis-lime"
          />
        </div>
      </div>
    </section>
  );
}

function ContactTile({
  icon: Icon,
  label,
  value,
  href,
  hint,
  className,
  muted,
  iconClass,
}: {
  icon: LucideIcon;
  label: string;
  value: string;
  href: string;
  hint: string;
  className: string;
  muted: string;
  iconClass: string;
}) {
  return (
    <a
      href={href}
      className={`group flex min-h-[13rem] flex-col justify-between gap-6 rounded-3xl p-7 transition-transform hover:-translate-y-0.5 md:p-8 ${className}`}
    >
      <div className="flex items-start justify-between">
        <span
          className={`inline-flex h-11 w-11 items-center justify-center rounded-xl ${iconClass}`}
        >
          <Icon className="h-5 w-5" strokeWidth={2.2} />
        </span>
        <ArrowUpRight
          className="h-5 w-5 transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5"
          strokeWidth={2.2}
        />
      </div>
      <div>
        <Mono className={`text-[10px] tracking-[0.18em] ${muted}`}>
          {label.toUpperCase()}
        </Mono>
        <div className="mt-1 break-all text-xl font-bold tracking-tight md:text-2xl">
          {value}
        </div>
        <div className={`mt-2 text-sm ${muted}`}>{hint}</div>
      </div>
    </a>
  );
}
