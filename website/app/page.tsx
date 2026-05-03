import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { Hero } from "@/components/sections/hero";
import { VoorWie } from "@/components/sections/voor-wie";
import { HoeHetWerkt } from "@/components/sections/hoe-het-werkt";
import { FeatureGrid } from "@/components/sections/feature-grid";
import { BespaarTijd } from "@/components/sections/bespaar-tijd";
import { Faq } from "@/components/sections/faq";
import { FinalCta } from "@/components/sections/final-cta";

export default function Page() {
  return (
    <>
      <SiteNav />
      <main>
        <Hero />
        <VoorWie />
        <HoeHetWerkt />
        <FeatureGrid />
        <BespaarTijd />
        <Faq />
        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}
