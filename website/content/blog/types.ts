export interface BlogPost {
  slug: string;
  /** Visible page H1. Keep ~50-70 chars for SEO sweet spot. */
  title: string;
  /** SEO meta title — usually `${title} · CoachOS Blog`. */
  metaTitle: string;
  /** SEO meta description — ~155 chars, primary keyword up front. */
  metaDescription: string;
  /** ISO date — drives sortOrder + datePublished schema. */
  publishedAt: string;
  /** ISO date if the article has been substantially revised. */
  updatedAt?: string;
  /** Manual estimate based on word count. ~250 words/min. */
  readMinutes: number;
  /** Visible category kicker (e.g., "GIDS · LESPLANNING"). */
  category: string;
  /** SEO keywords + used as tag chips. */
  tags: string[];
  /**
   * Lead paragraph. Written like a Wikipedia opener — factual, definition-first,
   * quoteable by AI engines. Stands alone as a TL;DR.
   */
  lead: string;
  sections: BlogSection[];
  /** Optional FAQ at the bottom of the article — fed into FAQPage schema. */
  faq?: { q: string; a: string }[];
  /** Slugs of other posts to recommend at the end. */
  related?: string[];
}

export interface BlogSection {
  /** H2 heading inside the article body. */
  heading: string;
  /** Body paragraphs — each renders as its own `<p>`. */
  paragraphs: string[];
  /** Optional unordered bullet list rendered after the paragraphs. */
  bullets?: string[];
  /** Optional emphasized callout box. */
  callout?: { tone: "info" | "tip" | "warn"; text: string };
}
