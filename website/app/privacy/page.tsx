import type { Metadata } from "next";
import Link from "next/link";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { WhitePage } from "@/components/site/white-page";
import { BentoPageHero } from "@/components/sections/bento-page-hero";

export const metadata: Metadata = {
  title: "Privacy",
  description: "Privacyverklaring van CoachOS.",
};

export default function PrivacyPage() {
  return (
    <WhitePage>
      <SiteNav />
      <main>
        <BentoPageHero
          kicker="JURIDISCH"
          title="Privacyverklaring"
          lead="Laatst bijgewerkt: nog te bepalen."
          titleClassName="text-3xl md:text-4xl"
        />
        <article className="mx-auto max-w-3xl px-6 py-12 md:py-16">
          <div className="space-y-6 text-base leading-relaxed text-ink-2">
            <p>
              CoachOS is in pre-launch. Een definitieve privacyverklaring volgt
              vóór algemene beschikbaarheid en zal voldoen aan de GDPR. We
              werken uitsluitend met dataverwerkers binnen de Europese Unie en
              verzamelen alleen wat strikt nodig is om lessen te plannen.
            </p>
            <p>
              Vragen over gegevensverwerking?{" "}
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
