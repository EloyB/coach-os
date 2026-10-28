import { ArrowRight, Check, X } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { FaqItem } from "@/components/sections/faq-item";
import { FinalCta } from "@/components/sections/final-cta";
import Link from "next/link";
import { ALL_PERSONAS, type Persona } from "@/content/personas";
import { PRICING_VISIBLE } from "@/content/pricing";

const SITE_URL = "https://coach-os.be";

export function PersonaPage({ persona }: { persona: Persona }) {
  return (
    <>
      <PersonaPageJsonLd persona={persona} />
      <SiteNav />
      <main>
        <PersonaHero persona={persona} />
        <PersonaPainSolution persona={persona} />
        <PersonaStory persona={persona} />
        <PersonaRelated persona={persona} />
        <PersonaFaq persona={persona} />
        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function PersonaHero({ persona }: { persona: Persona }) {
  return (
    <section className="border-b border-rule">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
          {persona.kicker}
        </Mono>
        <h1 className="mt-3 max-w-3xl text-4xl font-bold leading-[1.05] tracking-tight md:text-5xl">
          {persona.h1}
        </h1>
        <p className="mt-5 max-w-3xl text-lg leading-relaxed text-ink-2">
          {persona.lead}
        </p>

        <div className="mt-8 flex flex-wrap items-center gap-3">
          <a
            href="#contact"
            className="inline-flex h-11 items-center gap-2 rounded-md bg-tennis-green px-5 text-sm font-semibold text-paper transition-colors hover:bg-tennis-green/90"
          >
            Boek een demo
            <ArrowRight className="h-4 w-4" />
          </a>
        </div>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function PersonaPainSolution({ persona }: { persona: Persona }) {
  return (
    <section className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="grid gap-12 lg:grid-cols-2 lg:gap-16">
          {/* Pains column */}
          <div>
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              WAT JE NU HERKENT
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              Het probleem in jouw seizoen
            </h2>
            <ul className="mt-8 space-y-5">
              {persona.pains.map((p) => (
                <li
                  key={p.title}
                  className="flex gap-4 rounded-xl border border-rule bg-paper p-5"
                >
                  <span className="mt-0.5 inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-urgent/10 text-urgent">
                    <X className="h-4 w-4" strokeWidth={2.5} />
                  </span>
                  <div>
                    <h3 className="text-base font-bold tracking-tight">
                      {p.title}
                    </h3>
                    <p className="mt-1 text-sm leading-relaxed text-ink-2">
                      {p.body}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          </div>

          {/* Solutions column */}
          <div>
            <Mono className="text-[11px] tracking-[0.18em] text-tennis-green">
              HOE COACHOS DAT OPLOST
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              Wat de tool ervoor doet
            </h2>
            <ul className="mt-8 space-y-5">
              {persona.solutions.map((s) => (
                <li
                  key={s.title}
                  className="flex gap-4 rounded-xl border border-tennis-green/20 bg-paper p-5"
                >
                  <span className="mt-0.5 inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-tennis-green text-tennis-lime">
                    <Check className="h-4 w-4" strokeWidth={2.5} />
                  </span>
                  <div>
                    <h3 className="text-base font-bold tracking-tight">
                      {s.title}
                    </h3>
                    <p className="mt-1 text-sm leading-relaxed text-ink-2">
                      {s.body}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────

/** Uitgebreide, unieke inhoud per doelgroep: voorbeeld, inschrijven via een link, enzovoort. */
function PersonaStory({ persona }: { persona: Persona }) {
  if (persona.sections.length === 0) return null;
  return (
    <section className="border-b border-rule">
      <div className="mx-auto max-w-3xl space-y-14 px-6 py-20 md:py-24">
        {persona.sections.map((section) => (
          <div key={section.heading}>
            <h2 className="text-2xl font-bold tracking-tight md:text-3xl">
              {section.heading}
            </h2>
            {section.paragraphs.map((text) => (
              <p
                key={text}
                className="mt-4 text-base leading-relaxed text-ink-2"
              >
                {text}
              </p>
            ))}
            {section.bullets ? (
              <ul className="mt-5 space-y-3">
                {section.bullets.map((b) => (
                  <li key={b} className="flex items-start gap-3 text-base">
                    <Check
                      className="mt-1 h-4 w-4 flex-shrink-0 text-tennis-green"
                      strokeWidth={2.5}
                    />
                    <span className="leading-relaxed text-ink-2">{b}</span>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        ))}
      </div>
    </section>
  );
}

/** Links in de body naar de andere doelgroeppagina's en de prijzen (niet enkel via nav/footer). */
function PersonaRelated({ persona }: { persona: Persona }) {
  const others = ALL_PERSONAS.filter((p) => p.slug !== persona.slug);
  const linkClass =
    "font-semibold text-tennis-green underline-offset-4 hover:underline";

  return (
    <section className="border-b border-rule">
      <div className="mx-auto max-w-3xl px-6 py-12 md:py-14">
        <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
          OOK BEKIJKEN
        </Mono>
        <p className="mt-3 text-base leading-relaxed text-ink-2">
          CoachOS werkt ook voor andere doelgroepen:{" "}
          {others.map((p, i) => (
            <span key={p.slug}>
              {i > 0 ? " en " : null}
              <Link href={`/${p.slug}`} className={linkClass}>
                {p.linkLabel}
              </Link>
            </span>
          ))}
          .
          {PRICING_VISIBLE ? (
            <>
              {" "}
              Benieuwd wat het kost? Bekijk de{" "}
              <Link href="/prijzen" className={linkClass}>
                prijzen vanaf €25 per maand
              </Link>
              .
            </>
          ) : null}
        </p>
      </div>
    </section>
  );
}

function PersonaFaq({ persona }: { persona: Persona }) {
  return (
    <section id="faq" className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-3xl px-6 py-20 md:py-24">
        <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
          VRAGEN
        </Mono>
        <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
          Veelgestelde vragen
        </h2>

        <div className="mt-10 rounded-xl border border-rule bg-paper px-6 md:px-8">
          {persona.faq.map((entry) => (
            <FaqItem key={entry.q} {...entry} />
          ))}
        </div>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// JSON-LD: BreadcrumbList + FAQPage + Service
// ─────────────────────────────────────────────────────────────────────────

function PersonaPageJsonLd({ persona }: { persona: Persona }) {
  const pageUrl = `${SITE_URL}/${persona.slug}`;

  const breadcrumb = {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: [
      {
        "@type": "ListItem",
        position: 1,
        name: "Home",
        item: SITE_URL,
      },
      {
        "@type": "ListItem",
        position: 2,
        name: persona.navLabel,
        item: pageUrl,
      },
    ],
  };

  const faqPage = {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    "@id": `${pageUrl}#faq`,
    mainEntity: persona.faq.map((entry) => ({
      "@type": "Question",
      name: entry.q,
      acceptedAnswer: { "@type": "Answer", text: entry.a },
    })),
  };

  const service = {
    "@context": "https://schema.org",
    "@type": "Service",
    "@id": `${pageUrl}#service`,
    name: persona.h1,
    description: persona.lead,
    provider: { "@id": `${SITE_URL}/#organization` },
    audience: {
      "@type": "Audience",
      audienceType: persona.navLabel,
    },
    inLanguage: "nl",
  };

  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{
        __html: JSON.stringify([breadcrumb, faqPage, service]),
      }}
    />
  );
}
