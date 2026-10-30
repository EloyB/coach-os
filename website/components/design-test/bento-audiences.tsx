import Link from "next/link";
import { ArrowRight, Check } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { AUDIENCES, AUDIENCES_SUB, type Audience } from "@/content/audiences";

/** "Voor wie" als bento: drie grote kaarten die elk naar hun persona-pagina leiden. */
export function BentoAudiences() {
  const [tennis, padel, trainers] = AUDIENCES;
  return (
    <section
      id="voor-wie"
      className="scroll-mt-16 border-b border-rule bg-white"
    >
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            VOOR WIE
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            Voor alle tennis- en padeltrainers
          </h2>
          <p className="mt-3 text-base text-ink-2">{AUDIENCES_SUB}</p>
        </div>

        <div className="mt-12 grid gap-4 lg:grid-cols-6">
          <AudienceCard
            audience={tennis}
            tone="dark"
            className="lg:col-span-3"
          />
          <AudienceCard
            audience={padel}
            tone="green"
            className="lg:col-span-3"
          />
          <AudienceCard
            audience={trainers}
            tone="pale"
            className="lg:col-span-6"
            wide
          />
        </div>
      </div>
    </section>
  );
}

const TONES = {
  dark: {
    card: "bg-[#1f3d0e] text-white",
    body: "text-white/70",
    chip: "bg-white/10 text-white/90",
    check: "text-tennis-lime",
    link: "text-tennis-lime",
  },
  green: {
    card: "bg-[#2d5a16] text-white",
    body: "text-white/70",
    chip: "bg-white/10 text-white/90",
    check: "text-tennis-lime",
    link: "text-tennis-lime",
  },
  pale: {
    card: "bg-[#eef1e0] text-ink",
    body: "text-ink-2",
    chip: "bg-white text-ink-2",
    check: "text-tennis-green",
    link: "text-tennis-green",
  },
} as const;

function AudienceCard({
  audience,
  tone,
  className = "",
  wide = false,
}: {
  audience: Audience;
  tone: keyof typeof TONES;
  className?: string;
  wide?: boolean;
}) {
  const Icon = audience.icon;
  const t = TONES[tone];
  return (
    <article
      className={`group relative overflow-hidden rounded-3xl p-7 transition-transform hover:-translate-y-0.5 md:p-8 ${t.card} ${className}`}
    >
      <div
        className={`flex h-full flex-col gap-8 ${wide ? "md:flex-row md:items-center md:justify-between" : ""}`}
      >
        <div className={wide ? "max-w-md" : ""}>
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-tennis-lime text-tennis-green">
            <Icon className="h-5 w-5" strokeWidth={2.2} />
          </div>
          <h3 className="mt-6 text-2xl font-bold leading-[1.1] tracking-tight md:text-3xl">
            {/* De uitgerekte ::after maakt de hele kaart klikbaar. */}
            <Link
              href={`/${audience.slug}`}
              className="after:absolute after:inset-0"
            >
              {audience.title}
            </Link>
          </h3>
          <p className={`mt-3 text-sm leading-relaxed ${t.body}`}>
            {audience.body}
          </p>
          <span
            aria-hidden="true"
            className={`mt-6 inline-flex items-center gap-1.5 text-sm font-semibold ${t.link}`}
          >
            Lees meer
            <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
          </span>
        </div>

        <ul className={`space-y-2.5 ${wide ? "md:w-80" : "mt-auto"}`}>
          {audience.bullets.map((bullet) => (
            <li
              key={bullet}
              className={`flex items-center gap-2.5 rounded-xl px-4 py-3 text-sm ${t.chip}`}
            >
              <Check
                className={`h-4 w-4 shrink-0 ${t.check}`}
                strokeWidth={2.5}
              />
              {bullet}
            </li>
          ))}
        </ul>
      </div>
    </article>
  );
}
