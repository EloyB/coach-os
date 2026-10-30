import type { Metadata } from "next";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { BentoHero } from "@/components/design-test/bento-hero";
import { BentoFeatures } from "@/components/design-test/bento-features";
import { BentoAudiences } from "@/components/design-test/bento-audiences";
import { BentoSteps } from "@/components/design-test/bento-steps";
import { BentoComparison } from "@/components/design-test/bento-comparison";
import { BespaarTijd } from "@/components/sections/bespaar-tijd";
import { Pricing } from "@/components/sections/pricing";
import { Faq } from "@/components/sections/faq";
import { FinalCta } from "@/components/sections/final-cta";
import { PRICING_VISIBLE } from "@/content/pricing";

/** Testpagina voor een nieuw homepage-ontwerp: niet indexeerbaar en niet in de sitemap. */
export const metadata: Metadata = {
  title: "Design-test",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <>
      <SiteNav />
      <main>
        <BentoHero />
        <BentoFeatures />
        <BentoAudiences />
        <BentoSteps />
        <BentoComparison />
        <BespaarTijd />
        {PRICING_VISIBLE ? <Pricing /> : null}
        <Faq />
        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}
