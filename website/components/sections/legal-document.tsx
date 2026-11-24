import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { WhitePage } from "@/components/site/white-page";
import { BentoPageHero } from "@/components/sections/bento-page-hero";
import { loadLegalDocument, type LegalSlug } from "@/lib/legal";

/** Toont één juridisch document uit `docs/legal` (markdown, gelezen bij de build). */
export function LegalDocumentPage({ slug }: { slug: LegalSlug }) {
  const doc = loadLegalDocument(slug);
  const lead = doc.effectiveFrom
    ? `In werking vanaf ${doc.effectiveFrom}${doc.version ? ` · versie ${doc.version}` : ""}`
    : undefined;

  return (
    <WhitePage>
      <SiteNav />
      <main>
        <BentoPageHero
          kicker="JURIDISCH"
          title={doc.title}
          lead={lead}
          titleClassName="break-words text-2xl sm:text-3xl md:text-4xl"
        />
        <article className="mx-auto max-w-3xl px-6 py-12 md:py-16">
          <div
            className="legal-prose"
            // Vertrouwde inhoud: markdown uit onze eigen repository, bij de build omgezet.
            dangerouslySetInnerHTML={{ __html: doc.html }}
          />
        </article>
      </main>
      <SiteFooter />
    </WhitePage>
  );
}
