import type { MetadataRoute } from "next";
import { PRICING_VISIBLE } from "@/content/pricing";
import { ALL_PERSONAS } from "@/content/personas";
import { ALL_POSTS } from "@/content/blog";

/**
 * Bewust zonder `lastModified` voor de vaste pagina's: er is geen betrouwbare
 * wijzigingsdatum per pagina (een buildtijdstip zou elke URL bij elke deploy als
 * gewijzigd markeren). Blogartikels hebben wel een echte datum in hun content.
 */
export default function sitemap(): MetadataRoute.Sitemap {
  const base = "https://coach-os.be";
  return [
    { url: `${base}/`, changeFrequency: "monthly", priority: 1 },
    ...ALL_PERSONAS.map((p) => ({
      url: `${base}/${p.slug}`,
      changeFrequency: "monthly" as const,
      priority: 0.8,
    })),
    // /blog staat pas in de sitemap als er artikels zijn (anders is de pagina noindex).
    ...(ALL_POSTS.length > 0
      ? [{ url: `${base}/blog`, changeFrequency: "weekly" as const, priority: 0.7 }]
      : []),
    ...ALL_POSTS.map((p) => ({
      url: `${base}/blog/${p.slug}`,
      lastModified: new Date(p.updatedAt ?? p.publishedAt),
      changeFrequency: "yearly" as const,
      priority: 0.6,
    })),
    ...(PRICING_VISIBLE
      ? [
          {
            url: `${base}/prijzen`,
            changeFrequency: "monthly" as const,
            priority: 0.8,
          },
        ]
      : []),
    { url: `${base}/privacy`, changeFrequency: "yearly", priority: 0.3 },
    { url: `${base}/voorwaarden`, changeFrequency: "yearly", priority: 0.3 },
  ];
}
