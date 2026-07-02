import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { FaqItem } from "@/components/sections/faq-item";
import { Pricing } from "@/components/sections/pricing";
import { PricingCompare } from "@/components/sections/pricing-compare";
import { FinalCta } from "@/components/sections/final-cta";
import {
  PRICING_FAQ,
  PRICING_FAQ_HEADING,
  PRICING_VISIBLE,
} from "@/content/pricing";

const SITE_URL = "https://coach-os.be";
const PAGE_URL = `${SITE_URL}/prijzen`;

export const metadata: Metadata = {
  title: "Prijzen — lessenplanning vanaf €35/maand",
  description:
    "Tarieven voor CoachOS — lessenplanning voor tennis- en padelscholen. Starter, School en Federatie. Maandelijks opzegbaar, geen verborgen kosten per leerling.",
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
    title: "Prijzen — CoachOS",
    description:
      "Tarieven voor CoachOS, lessenplanning voor tennis- en padelscholen. Vanaf €35/maand. Maandelijks opzegbaar.",
    siteName: "CoachOS",
  },
};

function PricingPageJsonLd() {
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
        name: "Prijzen",
        item: PAGE_URL,
      },
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
              Eerlijke tarieven voor lessenplanning — geen verborgen kosten per
              leerling.
            </h1>
            <p className="mt-5 max-w-2xl text-lg leading-relaxed text-ink-2">
              Drie abonnementen voor zelfstandige coaches, scholen en federaties.
              Maandelijks opzegbaar, alle features inbegrepen op elk niveau.
            </p>
          </div>
        </section>

        <Pricing hideCompareLink />
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
