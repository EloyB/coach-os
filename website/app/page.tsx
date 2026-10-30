import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { HomepageJsonLd } from "@/components/site/json-ld";
import { BentoHero } from "@/components/sections/bento-hero";
import { BentoFeatures } from "@/components/sections/bento-features";
import { BentoAudiences } from "@/components/sections/bento-audiences";
import { BentoSteps } from "@/components/sections/bento-steps";
import { BentoComparison } from "@/components/sections/bento-comparison";
import { BentoPricing } from "@/components/sections/bento-pricing";
import { BentoFaq } from "@/components/sections/bento-faq";
import { BentoContact } from "@/components/sections/bento-contact";
import { PRICING_VISIBLE } from "@/content/pricing";

export default function Page() {
  return (
    // Witte pagina: de footer wordt hier overschreven, de bento-secties zijn zelf transparant.
    <div className="bg-white [&_footer]:border-t-0 [&_footer]:bg-white [&_header]:bg-white/85">
      <HomepageJsonLd />
      <SiteNav />
      <main>
        <BentoHero />
        <BentoFeatures />
        <BentoAudiences />
        <BentoSteps />
        <BentoComparison />
        {PRICING_VISIBLE ? <BentoPricing /> : null}
        <BentoFaq />
        <BentoContact />
      </main>
      <SiteFooter />
    </div>
  );
}
