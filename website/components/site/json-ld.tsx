import { FAQ } from "@/content/faq";
import { PRICING_TIERS, PRICING_VISIBLE } from "@/content/pricing";
import { FEATURES } from "@/content/features";
import { SITE } from "@/content/meta";

const SITE_URL = "https://coach-os.be";

interface JsonLdProps {
  schema: Record<string, unknown> | Record<string, unknown>[];
}

function JsonLd({ schema }: JsonLdProps) {
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
    />
  );
}

export function HomepageJsonLd() {
  const organization = {
    "@context": "https://schema.org",
    "@type": "Organization",
    "@id": `${SITE_URL}/#organization`,
    name: SITE.name,
    url: SITE_URL,
    logo: `${SITE_URL}/icon`,
    email: SITE.contactEmail,
    description:
      "Lessenplanningsysteem voor tennis- en padelclubs. Lessenreeksen, inschrijvingen zonder account, automatische planning en bevestiging via een link.",
    contactPoint: {
      "@type": "ContactPoint",
      contactType: "customer support",
      email: SITE.contactEmail,
      availableLanguage: ["Dutch", "nl"],
    },
  };

  const website = {
    "@context": "https://schema.org",
    "@type": "WebSite",
    "@id": `${SITE_URL}/#website`,
    url: SITE_URL,
    name: SITE.name,
    description: SITE.short,
    inLanguage: "nl",
    publisher: { "@id": `${SITE_URL}/#organization` },
  };

  const softwareApplication = {
    "@context": "https://schema.org",
    "@type": "SoftwareApplication",
    "@id": `${SITE_URL}/#software`,
    name: SITE.name,
    applicationCategory: "BusinessApplication",
    applicationSubCategory: "Sports Management Software",
    operatingSystem: "Web",
    url: SITE_URL,
    description:
      "Lessenplanningsysteem voor tennis- en padelclubs. Beheer lessenreeksen, ontvang inschrijvingen zonder account, plan automatisch en bevestig leerlingen via een link.",
    inLanguage: "nl",
    publisher: { "@id": `${SITE_URL}/#organization` },
    audience: {
      "@type": "Audience",
      audienceType:
        "Tennis- en padelclubs, hoofdtrainers en zelfstandige coaches",
    },
    featureList: FEATURES.map((f) => `${f.title}: ${f.body}`),
    // Prijzen komen uit dezelfde bron als de /prijzen-pagina. Per maand, excl. btw.
    ...(PRICING_VISIBLE
      ? {
          offers: {
            "@type": "AggregateOffer",
            priceCurrency: "EUR",
            lowPrice: String(Math.min(...PRICING_TIERS.map((t) => t.priceMonthly))),
            highPrice: String(Math.max(...PRICING_TIERS.map((t) => t.priceMonthly))),
            offerCount: PRICING_TIERS.length,
            offers: PRICING_TIERS.map((tier) => ({
              "@type": "Offer",
              name: tier.name,
              description: tier.studentLimit,
              priceCurrency: "EUR",
              price: String(tier.priceMonthly),
              priceSpecification: {
                "@type": "UnitPriceSpecification",
                price: String(tier.priceMonthly),
                priceCurrency: "EUR",
                unitCode: "MON",
                valueAddedTaxIncluded: false,
              },
              url: `${SITE_URL}/prijzen`,
            })),
          },
        }
      : {}),
  };

  const faqPage = {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    "@id": `${SITE_URL}/#faq`,
    mainEntity: FAQ.map((entry) => ({
      "@type": "Question",
      name: entry.q,
      acceptedAnswer: {
        "@type": "Answer",
        text: entry.a,
      },
    })),
  };

  return (
    <JsonLd schema={[organization, website, softwareApplication, faqPage]} />
  );
}
