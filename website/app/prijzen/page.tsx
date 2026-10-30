import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { CreditCard, MapPin, ShieldCheck } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { pageMetadata } from "@/lib/seo";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { WhitePage } from "@/components/site/white-page";
import { CourtLines } from "@/components/ui/court-lines";
import { BentoPricing } from "@/components/sections/bento-pricing";
import { BentoFaq } from "@/components/sections/bento-faq";
import { BentoContact } from "@/components/sections/bento-contact";
import { PricingCompare } from "@/components/sections/pricing-compare";
import {
  PRICING_EXPLAINER,
  PRICING_EXPLAINER_HEADING,
  PRICING_FAQ,
  PRICING_FAQ_HEADING,
  PRICING_ONBOARDING_NOTE,
  PRICING_TIERS,
  PRICING_VISIBLE,
} from "@/content/pricing";

const SITE_URL = "https://coach-os.be";
const PAGE_URL = `${SITE_URL}/prijzen`;

export const metadata: Metadata = pageMetadata({
  path: "/prijzen",
  title: "Prijzen: lesplanning vanaf €25 per maand",
  description:
    "Tarieven voor CoachOS, lesplanning voor tennis- en padelscholen en -clubs. Je betaalt per actieve leerling, met onbeperkt trainers. 30 dagen gratis proberen, maandelijks opzegbaar.",
  socialDescription:
    "Tarieven voor CoachOS, lesplanning voor tennis- en padelscholen en -clubs. Vanaf €25 per maand, per actieve leerling. 30 dagen gratis proberen.",
});

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
  const from = PRICING_TIERS[0];
  return (
    <WhitePage>
      <PricingPageJsonLd />
      <SiteNav />
      <main>
        <section>
          <div className="mx-auto grid max-w-6xl gap-4 px-6 py-16 md:py-24 lg:grid-cols-3">
            <div className="relative overflow-hidden rounded-3xl bg-[#1f3d0e] p-8 text-white md:p-12 lg:col-span-2">
              <CourtLines opacity={0.07} />
              <div className="relative">
                <Mono className="text-[11px] tracking-[0.18em] text-tennis-lime">
                  TARIEVEN
                </Mono>
                <h1 className="mt-5 max-w-3xl text-4xl font-bold leading-[1.05] tracking-tight md:text-5xl">
                  Lesplanning vanaf €25 per maand
                </h1>
                <p className="mt-5 max-w-2xl text-lg leading-relaxed text-white/75">
                  Drie abonnementen op basis van je aantal actieve leerlingen,
                  met een onbeperkt aantal trainers. Alle functies inbegrepen op
                  elk niveau, maandelijks opzegbaar of jaarlijks met 2 maanden
                  gratis.
                </p>
                <p className="mt-6 flex max-w-2xl items-start gap-2 rounded-xl bg-white/10 px-4 py-3 text-sm font-medium text-white/85">
                  <span className="mt-1.5 inline-block h-1.5 w-1.5 shrink-0 rounded-full bg-tennis-lime" />
                  {PRICING_ONBOARDING_NOTE}
                </p>
              </div>
            </div>

            <div className="flex min-h-[14rem] flex-col justify-between rounded-3xl bg-tennis-lime p-7 text-ink md:p-8">
              <Mono className="text-[11px] tracking-[0.18em] text-tennis-green">
                VANAF
              </Mono>
              <div>
                <p className="text-5xl font-bold tracking-tight md:text-6xl">
                  €{from.priceMonthly}
                  <span className="text-xl font-semibold text-tennis-green">
                    {" "}
                    /maand
                  </span>
                </p>
                <p className="mt-2 text-sm font-medium text-tennis-green">
                  {from.studentLimit}
                </p>
                <p className="mt-4 text-sm text-ink/70">
                  30 dagen gratis proberen
                </p>
              </div>
            </div>
          </div>
        </section>

        <BentoPricing hideHeader hideCompareLink />

        {/* Vertrouwen */}
        <section>
          <div className="mx-auto max-w-6xl px-6 pb-4">
            <div className="grid gap-4 md:grid-cols-3">
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
        <section>
          <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              UITLEG
            </Mono>
            <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
              {PRICING_EXPLAINER_HEADING}
            </h2>
            <div className="mt-10 grid gap-4 md:grid-cols-3">
              {PRICING_EXPLAINER.map((item, i) => (
                <div
                  key={item.title}
                  className={`rounded-3xl p-7 md:p-8 ${EXPLAINER_TONES[i % EXPLAINER_TONES.length]}`}
                >
                  <span className="flex h-10 w-10 items-center justify-center rounded-full bg-tennis-lime text-base font-bold text-tennis-green">
                    {i + 1}
                  </span>
                  <h3 className="mt-6 text-xl font-bold leading-tight tracking-tight md:text-2xl">
                    {item.title}
                  </h3>
                  <p className="mt-3 text-sm leading-relaxed text-white/75">
                    {item.body}
                  </p>
                </div>
              ))}
            </div>
          </div>
        </section>

        <PricingCompare />

        <BentoFaq heading={PRICING_FAQ_HEADING} entries={PRICING_FAQ} />

        <BentoContact />
      </main>
      <SiteFooter />
    </WhitePage>
  );
}

const EXPLAINER_TONES = [
  "bg-[#2d5a16] text-white",
  "bg-[#1f3d0e] text-white",
  "bg-[#3b6b1f] text-white",
];

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
    <div className="rounded-3xl bg-[#eef1e0] p-7">
      <span className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-tennis-green text-tennis-lime">
        <Icon className="h-5 w-5" strokeWidth={2.2} />
      </span>
      <h3 className="mt-5 text-lg font-bold tracking-tight text-ink">
        {title}
      </h3>
      <p className="mt-1.5 text-sm leading-relaxed text-ink-2">{body}</p>
    </div>
  );
}
