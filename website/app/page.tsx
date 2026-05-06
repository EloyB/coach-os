import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { PilotBanner } from "@/components/site/pilot-banner";
import { HomepageJsonLd } from "@/components/site/json-ld";
import { Hero } from "@/components/sections/hero";
import { VoorWie } from "@/components/sections/voor-wie";
import { HoeHetWerkt } from "@/components/sections/hoe-het-werkt";
import { FeatureShowcase } from "@/components/sections/feature-showcase";
import { FeatureGrid } from "@/components/sections/feature-grid";
import { BespaarTijd } from "@/components/sections/bespaar-tijd";
import { Pricing } from "@/components/sections/pricing";
import { PilotProgram } from "@/components/sections/pilot-program";
import { Faq } from "@/components/sections/faq";
import { FinalCta } from "@/components/sections/final-cta";
import { PRICING_VISIBLE } from "@/content/pricing";

export default function Page() {
  return (
    <>
      <HomepageJsonLd />
      <PilotBanner />
      <SiteNav />
      <main>
        <Hero />
        <VoorWie />
        <HoeHetWerkt />
        <FeatureShowcase />
        <FeatureGrid />
        <BespaarTijd />
        {PRICING_VISIBLE ? <Pricing /> : null}
        <PilotProgram />
        <Faq />
        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}
