import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { CreditCard, MapPin, ShieldCheck } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { FaqItem } from "@/components/sections/faq-item";
import { Pricing } from "@/components/sections/pricing";
import { PricingCompare } from "@/components/sections/pricing-compare";
import { FinalCta } from "@/components/sections/final-cta";
import {
  PRICING_EXPLAINER,
  PRICING_EXPLAINER_HEADING,
  PRICING_FAQ,
  PRICING_FAQ_HEADING,
  PRICING_TRIAL,
  PRICING_VISIBLE,
} from "@/content/pricing";

const SITE_URL = "https://coach-os.be";
const PAGE_URL = `${SITE_URL}/prijzen`;

export const metadata: Metadata = {
  title: "Prijzen: lessenplanning vanaf €25/maand",
  description:
    "Tarieven voor CoachOS, lessenplanning voor tennis- en padelclubs. Je betaalt per actieve leerling, met onbeperkt trainers. 30 dagen gratis proberen, maandelijks opzegbaar.",
  alternates: {
    canonical: PAGE_URL,
    languages: {
      "nl-BE": PAGE_URL,
      "nl-NL": PAGE_URL,
      "x-default": PAGE_URL,
    },
  },
  openGraph: {
    type: "website",
    locale: "nl_BE",
    alternateLocale: ["nl_NL"],
    url: PAGE_URL,
    title: "Prijzen · CoachOS",
    description:
      "Tarieven voor CoachOS, lessenplanning voor tennis- en padelclubs. Vanaf €25/maand, per actieve leerling. 30 dagen gratis proberen.",
    siteName: "CoachOS",
  },
};

function PricingPageJsonLd() {
  const breadcrumb = {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: [
      { "@type": "ListItem", position: 1, name: "Home", item: SITE_URL },
      { "@type": "ListItem", position: 2, name: "Prijzen", item: PAGE_URL },
    ],
  };

  const faq = {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    "@id": `${PAGE_URL}#faq`,
    mainEntity: PRICING_FAQ.map((entry) => ({
      "@type": "Question",
      name: entry.q,
      acceptedAnswer: { "@type": "Answer", text: entry.a },
    })),
  };

  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify([breadcrumb, faq]) }}
    />
  );
}

export default function PrijzenPage() {
  if (!PRICING_VISIBLE) {
    notFound();
  }
  return (
    <>
      <PricingPageJsonLd />
      <SiteNav />
      <main>
        <section className="border-b border-rule">
          <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              TARIEVEN
            </Mono>
            <h1 className="mt-3 max-w-3xl text-4xl font-bold leading-[1.05] tracking-tight md:text-5xl">
              Betaal per leerling, niet per trainer.
            </h1>
            <p className="mt-5 max-w-2xl text-lg leading-relaxed text-ink-2">
              Drie abonnementen op basis van je aantal actieve leerlingen, met
              een onbeperkt aantal trainers. Alle functies inbegrepen op elk
              niveau, maandelijks opzegbaar of jaarlijks met 2 maanden gratis.
            </p>
            <p className="mt-6 inline-flex items-center rounded-md border border-tennis-green/25 bg-tennis-green/5 px-3 py-1.5 text-xs font-medium text-ink-2">
              <span className="mr-2 inline-block h-1.5 w-1.5 rounded-full bg-tennis-green" />
              {PRICING_TRIAL}
            </p>
          </div>
        </section>

        <Pricing hideCompareLink />

        {/* Vertrouwen */}
        <section className="border-b border-rule bg-paper">
          <div className="mx-auto max-w-6xl px-6 py-12 md:py-14">
            <div className="grid gap-8 sm:grid-cols-3">
              <TrustItem
                icon={ShieldCheck}
                title="GDPR-conform"
                body="Je data blijft binnen de EU. Geen tracking, geen verkoop aan derden."
              />
              <TrustItem
                icon={CreditCard}
                title="Veilig betalen"
                body="Bancontact, iDEAL of overschrijving via Mollie. Geen verborgen kosten."
              />
              <TrustItem
                icon={MapPin}
                title="Gemaakt in België"
                body="Voor tennis- en padelclubs, met Nederlandstalige support."
              />
            </div>
          </div>
        </section>

        {/* Hoe werkt de prijs? */}
        <section className="border-b border-rule bg-canvas">
          <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              UITLEG
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              {PRICING_EXPLAINER_HEADING}
            </h2>
            <div className="mt-10 grid gap-6 md:grid-cols-3">
              {PRICING_EXPLAINER.map((item, i) => (
                <div
                  key={item.title}
                  className="rounded-xl border border-rule bg-paper p-6"
                >
                  <Mono className="text-[11px] text-ink-3">
                    0{i + 1}
                  </Mono>
                  <h3 className="mt-3 text-lg font-bold tracking-tight">
                    {item.title}
                  </h3>
                  <p className="mt-2 text-sm leading-relaxed text-ink-2">
                    {item.body}
                  </p>
                </div>
              ))}
            </div>
          </div>
        </section>

        <PricingCompare />

        <section className="border-b border-rule bg-canvas">
          <div className="mx-auto max-w-3xl px-6 py-20 md:py-24">
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              VRAGEN
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              {PRICING_FAQ_HEADING}
            </h2>

            <div className="mt-10 rounded-xl border border-rule bg-paper px-6 md:px-8">
              {PRICING_FAQ.map((entry) => (
                <FaqItem key={entry.q} {...entry} />
              ))}
            </div>
          </div>
        </section>

        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}

function TrustItem({
  icon: Icon,
  title,
  body,
}: {
  icon: LucideIcon;
  title: string;
  body: string;
}) {
  return (
    <div className="flex gap-4">
      <span className="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-lg bg-tennis-green text-tennis-lime">
        <Icon className="h-5 w-5" strokeWidth={2.2} />
      </span>
      <div>
        <h3 className="text-base font-bold tracking-tight text-ink">{title}</h3>
        <p className="mt-1 text-sm leading-relaxed text-ink-2">{body}</p>
      </div>
    </div>
  );
}
