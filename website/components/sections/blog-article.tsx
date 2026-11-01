import Link from "next/link";
import {
  ArrowLeft,
  ArrowRight,
  Clock,
  Info,
  Lightbulb,
  AlertTriangle,
} from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { WhitePage } from "@/components/site/white-page";
import { BentoPageHero } from "@/components/sections/bento-page-hero";
import { BentoFaq } from "@/components/sections/bento-faq";
import { BentoContact } from "@/components/sections/bento-contact";
import { PRICING_VISIBLE } from "@/content/pricing";
import {
  formatDateNL,
  getRelatedPosts,
  type BlogPost,
  type BlogSection,
} from "@/content/blog";

const SITE_URL = "https://coach-os.be";

export function BlogArticle({ post }: { post: BlogPost }) {
  const related = getRelatedPosts(post);

  return (
    <WhitePage>
      <BlogArticleJsonLd post={post} />
      <SiteNav />
      <main>
        <ArticleHeader post={post} />
        <ArticleBody post={post} />
        {post.faq && post.faq.length > 0 ? (
          <BentoFaq entries={post.faq} heading="Veelgestelde vragen" />
        ) : null}
        {related.length > 0 ? <RelatedPosts posts={related} /> : null}
        <ExploreLinks />
        <BentoContact />
      </main>
      <SiteFooter />
    </WhitePage>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function ArticleHeader({ post }: { post: BlogPost }) {
  return (
    <BentoPageHero
      narrow
      kicker={post.category}
      title={post.title}
      lead={post.lead}
      titleClassName="text-3xl md:text-5xl"
    >
      <div className="mt-7 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-white/60">
        <Link
          href="/blog"
          className="inline-flex items-center gap-1.5 text-tennis-lime transition-colors hover:text-white"
        >
          <ArrowLeft className="h-4 w-4" />
          Alle artikelen
        </Link>
        <span>{formatDateNL(post.publishedAt)}</span>
        <span className="inline-flex items-center gap-1.5">
          <Clock className="h-3.5 w-3.5" />
          {post.readMinutes} min lezen
        </span>
        {post.tags.length > 0 ? (
          <span className="inline-flex flex-wrap items-center gap-1.5">
            {post.tags.slice(0, 3).map((t) => (
              <Mono
                key={t}
                className="rounded-full bg-white/10 px-2.5 py-0.5 text-[10px] tracking-[0.08em] text-white/80"
              >
                {t}
              </Mono>
            ))}
          </span>
        ) : null}
      </div>
    </BentoPageHero>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function ArticleBody({ post }: { post: BlogPost }) {
  return (
    <section>
      <div className="mx-auto max-w-3xl px-6 py-16 md:py-20">
        <article className="space-y-12">
          {post.sections.map((section) => (
            <ArticleSection key={section.heading} section={section} />
          ))}
        </article>
      </div>
    </section>
  );
}

function ArticleSection({ section }: { section: BlogSection }) {
  return (
    <div>
      <h2 className="text-2xl font-bold tracking-tight md:text-3xl">
        {section.heading}
      </h2>
      <div className="mt-5 space-y-4">
        {section.paragraphs.map((p, i) => (
          <p
            key={i}
            className="text-base leading-relaxed text-ink-2 md:text-lg"
          >
            {p}
          </p>
        ))}
      </div>
      {section.bullets && section.bullets.length > 0 ? (
        <ul className="mt-6 space-y-2.5 rounded-3xl bg-[#eef1e0] p-6 md:p-7">
          {section.bullets.map((b, i) => (
            <li
              key={i}
              className="flex items-start gap-3 text-sm leading-relaxed text-ink-2 md:text-base"
            >
              <span className="mt-2.5 inline-block h-1.5 w-1.5 shrink-0 rounded-full bg-tennis-green" />
              <span>{b}</span>
            </li>
          ))}
        </ul>
      ) : null}
      {section.callout ? <Callout {...section.callout} /> : null}
    </div>
  );
}

function Callout({
  tone,
  text,
}: {
  tone: "info" | "tip" | "warn";
  text: string;
}) {
  const styles =
    tone === "tip"
      ? {
          bg: "bg-tennis-lime/25",
          icon: "text-tennis-green",
          Icon: Lightbulb,
          label: "TIP",
        }
      : tone === "warn"
        ? {
            bg: "bg-amber-100",
            icon: "text-warn",
            Icon: AlertTriangle,
            label: "LET OP",
          }
        : {
            bg: "bg-[#eef1e0]",
            icon: "text-ink-2",
            Icon: Info,
            label: "INFO",
          };
  const Icon = styles.Icon;
  return (
    <div className={`mt-6 flex items-start gap-4 rounded-3xl p-6 ${styles.bg}`}>
      <Icon
        className={`mt-0.5 h-5 w-5 shrink-0 ${styles.icon}`}
        strokeWidth={2.2}
      />
      <div>
        <Mono className={`text-[10px] tracking-[0.18em] ${styles.icon}`}>
          {styles.label}
        </Mono>
        <p className="mt-1 text-sm leading-relaxed text-ink md:text-base">
          {text}
        </p>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────

/** Interne links naar de doelgroeppagina's en de prijzen, onderaan elk artikel. */
function ExploreLinks() {
  const linkClass =
    "font-semibold text-tennis-green underline-offset-4 hover:underline";
  return (
    <section>
      <div className="mx-auto max-w-5xl px-6 pb-4">
        <div className="rounded-3xl bg-[#eef1e0] p-8 md:p-10">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            OOK BEKIJKEN
          </Mono>
          <p className="mt-3 text-base leading-relaxed text-ink-2">
            Lees hoe CoachOS de lessenplanning regelt voor{" "}
            <Link href="/voor-tennisclubs" className={linkClass}>
              tennisscholen
            </Link>
            ,{" "}
            <Link href="/voor-padelclubs" className={linkClass}>
              padelscholen
            </Link>{" "}
            en{" "}
            <Link href="/voor-trainers" className={linkClass}>
              trainers
            </Link>
            .
            {PRICING_VISIBLE ? (
              <>
                {" "}
                Benieuwd wat het kost? Bekijk de{" "}
                <Link href="/prijzen" className={linkClass}>
                  prijzen
                </Link>
                .
              </>
            ) : null}
          </p>
        </div>
      </div>
    </section>
  );
}

function RelatedPosts({ posts }: { posts: BlogPost[] }) {
  return (
    <section>
      <div className="mx-auto max-w-5xl px-6 py-16 md:py-20">
        <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
          MEER LEZEN
        </Mono>
        <h2 className="mt-3 text-2xl font-bold tracking-tight md:text-3xl">
          Verwante artikelen
        </h2>
        <div className="mt-8 grid gap-4 md:grid-cols-2">
          {posts.map((p) => (
            <Link
              key={p.slug}
              href={`/blog/${p.slug}`}
              className="group flex flex-col gap-3 rounded-3xl bg-[#eef1e0] p-7 transition-transform hover:-translate-y-0.5"
            >
              <Mono className="text-[10px] tracking-[0.18em] text-ink-3">
                {p.category}
              </Mono>
              <h3 className="text-lg font-bold tracking-tight">{p.title}</h3>
              <p className="text-sm text-ink-2">{p.lead.slice(0, 140)}…</p>
              <span className="mt-auto inline-flex items-center gap-1 text-xs font-semibold text-tennis-green">
                Lees verder
                <ArrowRight className="h-3 w-3 transition-transform group-hover:translate-x-0.5" />
              </span>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────
// JSON-LD: BlogPosting + BreadcrumbList + (FAQPage if present)
// ─────────────────────────────────────────────────────────────────────────

function BlogArticleJsonLd({ post }: { post: BlogPost }) {
  const pageUrl = `${SITE_URL}/blog/${post.slug}`;

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
      { "@type": "ListItem", position: 3, name: post.title, item: pageUrl },
    ],
  };

  const blogPosting = {
    "@context": "https://schema.org",
    "@type": "BlogPosting",
    "@id": pageUrl,
    headline: post.title,
    description: post.lead,
    datePublished: post.publishedAt,
    ...(post.updatedAt ? { dateModified: post.updatedAt } : {}),
    inLanguage: "nl",
    keywords: post.tags.join(", "),
    mainEntityOfPage: { "@type": "WebPage", "@id": pageUrl },
    author: { "@id": `${SITE_URL}/#organization` },
    publisher: { "@id": `${SITE_URL}/#organization` },
  };

  const schemas: object[] = [breadcrumb, blogPosting];

  if (post.faq && post.faq.length > 0) {
    schemas.push({
      "@context": "https://schema.org",
      "@type": "FAQPage",
      "@id": `${pageUrl}#faq`,
      mainEntity: post.faq.map((entry) => ({
        "@type": "Question",
        name: entry.q,
        acceptedAnswer: { "@type": "Answer", text: entry.a },
      })),
    });
  }

  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(schemas) }}
    />
  );
}
