import { FAQ } from "@/content/faq";
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
      "Lesplanningsysteem voor tennis- en padelclubs in Nederland en België. Lesreeksen, anonieme inschrijvingen, automatische scheduling en magic-link bevestigingen.",
    areaServed: [
      { "@type": "Country", name: "Netherlands" },
      { "@type": "Country", name: "Belgium" },
    ],
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
      "Lesplanningsysteem voor tennis- en padelclubs. Beheer lesreeksen, ontvang anonieme inschrijvingen, plan automatisch en bevestig leerlingen via magic links.",
    inLanguage: "nl",
    publisher: { "@id": `${SITE_URL}/#organization` },
    audience: {
      "@type": "Audience",
      audienceType: "Tennis- en padelclubs, hoofdtrainers en zelfstandige coaches in de Benelux",
    },
    featureList: FEATURES.map((f) => `${f.title} — ${f.body}`),
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
