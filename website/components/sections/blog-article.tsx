import Link from "next/link";
import { ArrowLeft, ArrowRight, Clock, Info, Lightbulb, AlertTriangle } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { FaqItem } from "@/components/sections/faq-item";
import { FinalCta } from "@/components/sections/final-cta";
import { formatDateNL, getRelatedPosts, type BlogPost, type BlogSection } from "@/content/blog";

const SITE_URL = "https://coach-os.be";

export function BlogArticle({ post }: { post: BlogPost }) {
  const related = getRelatedPosts(post);

  return (
    <>
      <BlogArticleJsonLd post={post} />
      <SiteNav />
      <main>
        <ArticleHeader post={post} />
        <ArticleBody post={post} />
        {post.faq && post.faq.length > 0 ? (
          <ArticleFaq faq={post.faq} />
        ) : null}
        {related.length > 0 ? <RelatedPosts posts={related} /> : null}
        <FinalCta />
      </main>
      <SiteFooter />
    </>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function ArticleHeader({ post }: { post: BlogPost }) {
  return (
    <section className="border-b border-rule">
      <div className="mx-auto max-w-3xl px-6 py-16 md:py-20">
        <Link
          href="/blog"
          className="inline-flex items-center gap-1.5 text-sm text-ink-3 transition-colors hover:text-ink"
        >
          <ArrowLeft className="h-4 w-4" />
          Alle artikelen
        </Link>

        <Mono className="mt-8 block text-[11px] tracking-[0.18em] text-ink-3">
          {post.category}
        </Mono>
        <h1 className="mt-3 text-3xl font-bold leading-[1.1] tracking-tight md:text-5xl">
          {post.title}
        </h1>
        <p className="mt-5 text-lg leading-relaxed text-ink-2">{post.lead}</p>

        <div className="mt-7 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-ink-3">
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
                  className="rounded-full border border-rule px-2 py-0.5 text-[10px] tracking-[0.08em]"
                >
                  {t}
                </Mono>
              ))}
            </span>
          ) : null}
        </div>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function ArticleBody({ post }: { post: BlogPost }) {
  return (
    <section className="border-b border-rule bg-canvas">
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
          <p key={i} className="text-base leading-relaxed text-ink-2 md:text-lg">
            {p}
          </p>
        ))}
      </div>
      {section.bullets && section.bullets.length > 0 ? (
        <ul className="mt-6 space-y-2.5 rounded-xl border border-rule bg-paper p-6">
          {section.bullets.map((b, i) => (
            <li key={i} className="flex items-start gap-3 text-sm leading-relaxed text-ink-2 md:text-base">
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

function Callout({ tone, text }: { tone: "info" | "tip" | "warn"; text: string }) {
  const styles =
    tone === "tip"
      ? {
          bg: "bg-tennis-lime/15 border-tennis-green/20",
          icon: "text-tennis-green",
          Icon: Lightbulb,
          label: "TIP",
        }
      : tone === "warn"
        ? {
            bg: "bg-warn/10 border-warn/30",
            icon: "text-warn",
            Icon: AlertTriangle,
            label: "LET OP",
          }
        : {
            bg: "bg-paper border-rule",
            icon: "text-ink-2",
            Icon: Info,
            label: "INFO",
          };
  const Icon = styles.Icon;
  return (
    <div className={`mt-6 flex items-start gap-4 rounded-xl border p-5 ${styles.bg}`}>
      <Icon className={`mt-0.5 h-5 w-5 shrink-0 ${styles.icon}`} strokeWidth={2.2} />
      <div>
        <Mono className={`text-[10px] tracking-[0.18em] ${styles.icon}`}>
          {styles.label}
        </Mono>
        <p className="mt-1 text-sm leading-relaxed text-ink md:text-base">{text}</p>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function ArticleFaq({ faq }: { faq: NonNullable<BlogPost["faq"]> }) {
  return (
    <section className="border-b border-rule">
      <div className="mx-auto max-w-3xl px-6 py-16 md:py-20">
        <Mono className="text-[11px] tracking-[0.18em] text-ink-3">VRAGEN</Mono>
        <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
          Veelgestelde vragen
        </h2>
        <div className="mt-8 rounded-xl border border-rule bg-paper px-6 md:px-8">
          {faq.map((entry) => (
            <FaqItem key={entry.q} {...entry} />
          ))}
        </div>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────

function RelatedPosts({ posts }: { posts: BlogPost[] }) {
  return (
    <section className="border-b border-rule bg-canvas">
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
              className="group flex flex-col gap-3 rounded-xl border border-rule bg-paper p-6 transition-colors hover:border-tennis-green/40"
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
      { "@type": "ListItem", position: 2, name: "Blog", item: `${SITE_URL}/blog` },
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
