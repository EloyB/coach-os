import type { Metadata } from "next";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { Hero } from "@/components/sections/hero";
import { ProductScreenshot } from "@/components/sections/product-screenshot";
import { VoorWie } from "@/components/sections/voor-wie";
import { HoeHetWerkt } from "@/components/sections/hoe-het-werkt";
import { FeatureShowcase } from "@/components/sections/feature-showcase";
import { FeatureGrid } from "@/components/sections/feature-grid";
import { ExcelComparison } from "@/components/sections/excel-comparison";
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
        <Hero />
        <ProductScreenshot />
        <VoorWie />
        <HoeHetWerkt />
        <FeatureShowcase />
        <FeatureGrid />
        <ExcelComparison />
        <BespaarTijd />
        {PRICING_VISIBLE ? <Pricing /> : null}
        <Faq />
        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}
