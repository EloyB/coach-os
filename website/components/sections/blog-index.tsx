import Link from "next/link";
import { ArrowRight, Clock } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { FinalCta } from "@/components/sections/final-cta";
import { formatDateNL, POSTS_BY_DATE } from "@/content/blog";

const SITE_URL = "https://coach-os.be";

export function BlogIndex() {
  return (
    <>
      <BlogIndexJsonLd />
      <SiteNav />
      <main>
        <section className="border-b border-rule">
          <div className="mx-auto max-w-5xl px-6 py-20 md:py-24">
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              BLOG
            </Mono>
            <h1 className="mt-3 text-4xl font-bold leading-[1.05] tracking-tight md:text-5xl">
              Gidsen, achtergronden en meningen over lessenplanning
            </h1>
            <p className="mt-5 max-w-2xl text-lg leading-relaxed text-ink-2">
              Hoe je een seizoen plant zonder Excel, wat de AVG-eisen zijn voor
              een tennisschool, en waarom magic-link bevestigingen
              gebruiksvriendelijker zijn dan accounts. Praktische gidsen voor
              scholen en trainers.
            </p>
          </div>
        </section>

        <section className="border-b border-rule bg-canvas">
          <div className="mx-auto max-w-5xl px-6 py-16 md:py-20">
            <div className="grid gap-5 md:grid-cols-2">
              {POSTS_BY_DATE.map((p) => (
                <Link
                  key={p.slug}
                  href={`/blog/${p.slug}`}
                  className="group flex flex-col gap-4 rounded-xl border border-rule bg-paper p-7 transition-colors hover:border-tennis-green/40"
                >
                  <Mono className="text-[10px] tracking-[0.18em] text-ink-3">
                    {p.category}
                  </Mono>
                  <h2 className="text-xl font-bold leading-tight tracking-tight">
                    {p.title}
                  </h2>
                  <p className="text-sm leading-relaxed text-ink-2">
                    {p.lead.slice(0, 200)}
                    {p.lead.length > 200 ? "…" : ""}
                  </p>
                  <div className="mt-auto flex items-center justify-between gap-4 pt-3">
                    <div className="flex items-center gap-3 text-xs text-ink-3">
                      <span>{formatDateNL(p.publishedAt)}</span>
                      <span className="inline-flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        {p.readMinutes} min
                      </span>
                    </div>
                    <span className="inline-flex items-center gap-1 text-xs font-semibold text-tennis-green">
                      Lees verder
                      <ArrowRight className="h-3 w-3 transition-transform group-hover:translate-x-0.5" />
                    </span>
                  </div>
                </Link>
              ))}
            </div>
          </div>
        </section>

        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}

function BlogIndexJsonLd() {
  const blog = {
    "@context": "https://schema.org",
    "@type": "Blog",
    "@id": `${SITE_URL}/blog`,
    name: "CoachOS Blog",
    description:
      "Gidsen en achtergronden over lessenplanning voor tennis- en padelscholen.",
    inLanguage: "nl",
    publisher: { "@id": `${SITE_URL}/#organization` },
    blogPost: POSTS_BY_DATE.map((p) => ({
      "@type": "BlogPosting",
      "@id": `${SITE_URL}/blog/${p.slug}`,
      headline: p.title,
      datePublished: p.publishedAt,
      ...(p.updatedAt ? { dateModified: p.updatedAt } : {}),
    })),
  };

  const breadcrumb = {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: [
      { "@type": "ListItem", position: 1, name: "Home", item: SITE_URL },
      {
        "@type": "ListItem",
        position: 2,
        name: "Blog",
        item: `${SITE_URL}/blog`,
      },
    ],
  };

  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify([breadcrumb, blog]) }}
    />
  );
}
