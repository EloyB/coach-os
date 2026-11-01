import Link from "next/link";
import { ArrowRight, Clock } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { WhitePage } from "@/components/site/white-page";
import { BentoPageHero } from "@/components/sections/bento-page-hero";
import { BentoContact } from "@/components/sections/bento-contact";
import { formatDateNL, POSTS_BY_DATE } from "@/content/blog";

const SITE_URL = "https://coach-os.be";

const POST_TONES = [
  {
    card: "bg-[#eef1e0] text-ink",
    body: "text-ink-2",
    faint: "text-ink-3",
    link: "text-tennis-green",
  },
  {
    card: "bg-[#2d5a16] text-white",
    body: "text-white/75",
    faint: "text-white/60",
    link: "text-tennis-lime",
  },
  {
    card: "bg-[#1f3d0e] text-white",
    body: "text-white/75",
    faint: "text-white/60",
    link: "text-tennis-lime",
  },
] as const;

export function BlogIndex() {
  return (
    <WhitePage>
      <BlogIndexJsonLd />
      <SiteNav />
      <main>
        <BentoPageHero
          kicker="BLOG"
          title="Gidsen, achtergronden en meningen over lessenplanning"
          lead="Hoe je een seizoen plant zonder Excel, wat de GDPR-eisen zijn voor een tennis- of padelclub, en waarom inschrijven zonder account gebruiksvriendelijker is. Praktische gidsen voor clubs en trainers."
          titleClassName="text-3xl md:text-5xl"
        />

        <section>
          <div className="mx-auto max-w-6xl px-6 py-12 md:py-16">
            <div className="grid gap-4 md:grid-cols-2">
              {POSTS_BY_DATE.map((p, i) => {
                const tone = POST_TONES[i % POST_TONES.length];
                return (
                  <Link
                    key={p.slug}
                    href={`/blog/${p.slug}`}
                    className={`group flex flex-col gap-4 rounded-3xl p-7 transition-transform hover:-translate-y-0.5 md:p-8 ${tone.card}`}
                  >
                    <Mono
                      className={`text-[10px] tracking-[0.18em] ${tone.faint}`}
                    >
                      {p.category}
                    </Mono>
                    <h2 className="text-2xl font-bold leading-tight tracking-tight">
                      {p.title}
                    </h2>
                    <p className={`text-sm leading-relaxed ${tone.body}`}>
                      {p.lead.slice(0, 200)}
                      {p.lead.length > 200 ? "…" : ""}
                    </p>
                    <div className="mt-auto flex items-center justify-between gap-4 pt-3">
                      <div
                        className={`flex items-center gap-3 text-xs ${tone.faint}`}
                      >
                        <span>{formatDateNL(p.publishedAt)}</span>
                        <span className="inline-flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {p.readMinutes} min
                        </span>
                      </div>
                      <span
                        className={`inline-flex items-center gap-1 text-sm font-semibold ${tone.link}`}
                      >
                        Lees verder
                        <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
                      </span>
                    </div>
                  </Link>
                );
              })}
            </div>
          </div>
        </section>

        <BentoContact />
      </main>
      <SiteFooter />
    </WhitePage>
  );
}

function BlogIndexJsonLd() {
  const blog = {
    "@context": "https://schema.org",
    "@type": "Blog",
    "@id": `${SITE_URL}/blog`,
    name: "CoachOS Blog",
    description:
      "Gidsen en achtergronden over lessenplanning voor tennis- en padelclubs.",
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
