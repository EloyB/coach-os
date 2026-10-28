import type { Metadata } from "next";
import { Instrument_Serif } from "next/font/google";
import { SiteFooter } from "@/components/site/site-footer";
import { FloatingNav } from "@/components/design-test/floating-nav";
import { DesignHero } from "@/components/design-test/design-hero";
import { BentoFeatures } from "@/components/design-test/bento-features";
import { VoorWie } from "@/components/sections/voor-wie";
import { HoeHetWerkt } from "@/components/sections/hoe-het-werkt";
import { ExcelComparison } from "@/components/sections/excel-comparison";
import { BespaarTijd } from "@/components/sections/bespaar-tijd";
import { Pricing } from "@/components/sections/pricing";
import { Faq } from "@/components/sections/faq";
import { FinalCta } from "@/components/sections/final-cta";
import { PRICING_VISIBLE } from "@/content/pricing";

const display = Instrument_Serif({
  variable: "--font-display",
  subsets: ["latin"],
  weight: "400",
  style: ["normal", "italic"],
});

/** Testpagina voor een nieuw homepage-ontwerp: niet indexeerbaar en niet in de sitemap. */
export const metadata: Metadata = {
  title: "Design-test",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <div className={`${display.variable} design-test`}>
      <FloatingNav />
      <main>
        <DesignHero />
        <BentoFeatures />
        <VoorWie />
        <HoeHetWerkt />
        <ExcelComparison />
        <BespaarTijd />
        {PRICING_VISIBLE ? <Pricing /> : null}
        <Faq />
        <FinalCta />
      </main>
      <SiteFooter />
    </div>
  );
}
