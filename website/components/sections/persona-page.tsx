import { ArrowRight, Check, X } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { WhitePage } from "@/components/site/white-page";
import { BentoPageHero } from "@/components/sections/bento-page-hero";
import { BentoFaq } from "@/components/sections/bento-faq";
import { BentoContact } from "@/components/sections/bento-contact";
import Link from "next/link";
import { ALL_PERSONAS, type Persona } from "@/content/personas";
import { PRICING_VISIBLE } from "@/content/pricing";

const SITE_URL = "https://coach-os.be";

export function PersonaPage({ persona }: { persona: Persona }) {
  return (
    <WhitePage>
      <PersonaPageJsonLd persona={persona} />
      <SiteNav />
      <main>
        <PersonaHero persona={persona} />
        <PersonaPainSolution persona={persona} />
        <PersonaStory persona={persona} />
        <PersonaRelated persona={persona} />
        <BentoFaq entries={persona.faq} heading="Veelgestelde vragen" />
        <BentoContact />
      </main>
      <SiteFooter />
    </WhitePage>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function PersonaHero({ persona }: { persona: Persona }) {
  return (
    <BentoPageHero
      kicker={persona.kicker}
      title={persona.h1}
      lead={persona.lead}
    >
      <a
        href="#contact"
        className="mt-8 inline-flex h-11 items-center gap-2 rounded-lg bg-tennis-lime px-5 text-sm font-semibold text-ink transition-colors hover:bg-tennis-lime/90"
      >
        Boek een demo
        <ArrowRight className="h-4 w-4" />
      </a>
    </BentoPageHero>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function PersonaPainSolution({ persona }: { persona: Persona }) {
  return (
    <section>
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="grid gap-4 lg:grid-cols-2">
          {/* Pains column */}
          <div>
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              WAT JE NU HERKENT
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              Het probleem in jouw seizoen
            </h2>
            <ul className="mt-8 space-y-4">
              {persona.pains.map((p) => (
                <li
                  key={p.title}
                  className="flex gap-4 rounded-3xl bg-[#eef1e0] p-6"
                >
                  <span className="mt-0.5 inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-white text-urgent">
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
            <ul className="mt-8 space-y-4">
              {persona.solutions.map((s) => (
                <li
                  key={s.title}
                  className="flex gap-4 rounded-3xl bg-[#1f3d0e] p-6 text-white"
                >
                  <span className="mt-0.5 inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-tennis-lime text-tennis-green">
                    <Check className="h-4 w-4" strokeWidth={2.5} />
                  </span>
                  <div>
                    <h3 className="text-base font-bold tracking-tight">
                      {s.title}
                    </h3>
                    <p className="mt-1 text-sm leading-relaxed text-white/75">
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
    <section>
      <div className="mx-auto max-w-4xl space-y-4 px-6 pb-20 md:pb-24">
        {persona.sections.map((section, i) => (
          <div
            key={section.heading}
            className={`rounded-3xl p-8 md:p-10 ${i % 2 === 0 ? "bg-[#eef1e0]" : "bg-[#2d5a16] text-white"}`}
          >
            <h2 className="text-2xl font-bold tracking-tight md:text-3xl">
              {section.heading}
            </h2>
            {section.paragraphs.map((text) => (
              <p
                key={text}
                className={`mt-4 text-base leading-relaxed ${i % 2 === 0 ? "text-ink-2" : "text-white/80"}`}
              >
                {text}
              </p>
            ))}
            {section.bullets ? (
              <ul className="mt-6 space-y-2.5">
                {section.bullets.map((b) => (
                  <li
                    key={b}
                    className={`flex items-start gap-3 rounded-2xl px-4 py-3 text-base ${i % 2 === 0 ? "bg-white text-ink-2" : "bg-white/10 text-white/90"}`}
                  >
                    <Check
                      className={`mt-1 h-4 w-4 shrink-0 ${i % 2 === 0 ? "text-tennis-green" : "text-tennis-lime"}`}
                      strokeWidth={2.5}
                    />
                    <span className="leading-relaxed">{b}</span>
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
    <section>
      <div className="mx-auto max-w-4xl px-6 pb-12">
        <div className="rounded-3xl bg-[#eef1e0] p-8 md:p-10">
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
