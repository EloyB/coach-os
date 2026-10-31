import type { Metadata } from "next";
import Link from "next/link";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { WhitePage } from "@/components/site/white-page";
import { BentoPageHero } from "@/components/sections/bento-page-hero";

export const metadata: Metadata = {
  title: "Voorwaarden",
  description: "Algemene voorwaarden van CoachOS.",
};

export default function VoorwaardenPage() {
  return (
    <WhitePage>
      <SiteNav />
      <main>
        <BentoPageHero
          kicker="JURIDISCH"
          title="Algemene voorwaarden"
          lead="Laatst bijgewerkt: nog te bepalen."
          titleClassName="text-3xl md:text-4xl"
        />
        <article className="mx-auto max-w-3xl px-6 py-12 md:py-16">
          <div className="space-y-6 text-base leading-relaxed text-ink-2">
            <p>
              CoachOS is in pre-launch. De definitieve algemene voorwaarden
              worden gepubliceerd vóór algemene beschikbaarheid.
            </p>
            <p>
              Specifieke vragen over een toekomstige overeenkomst?{" "}
              <Link
                href="/#contact"
                className="font-semibold text-ink underline underline-offset-4"
              >
                Neem contact op
              </Link>
              .
            </p>
          </div>
        </article>
      </main>
      <SiteFooter />
    </WhitePage>
  );
}
