import type { Metadata } from "next";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { BentoHero } from "@/components/design-test/bento-hero";
import { BentoFeatures } from "@/components/design-test/bento-features";
import { BentoAudiences } from "@/components/design-test/bento-audiences";
import { BentoSteps } from "@/components/design-test/bento-steps";
import { BentoComparison } from "@/components/design-test/bento-comparison";
import { BentoPricing } from "@/components/design-test/bento-pricing";
import { Faq } from "@/components/sections/faq";
import { BentoContact } from "@/components/design-test/bento-contact";
import { PRICING_VISIBLE } from "@/content/pricing";

/** Testpagina voor een nieuw homepage-ontwerp: niet indexeerbaar en niet in de sitemap. */
export const metadata: Metadata = {
  title: "Design-test",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    // Hele pagina wit: de sectie- en footerachtergronden worden hier overschreven.
    <div className="bg-white [&_header]:bg-white/85 [&_footer]:border-t-0 [&_footer]:bg-white [&_section]:border-b-0 [&_section]:bg-white">
      <SiteNav />
      <main>
        <BentoHero />
        <BentoFeatures />
        <BentoAudiences />
        <BentoSteps />
        <BentoComparison />
        {PRICING_VISIBLE ? <BentoPricing /> : null}
        <Faq />
        <BentoContact />
      </main>
      <SiteFooter />
    </div>
  );
}
