import type { MetadataRoute } from "next";
import { PRICING_VISIBLE } from "@/content/pricing";
import { ALL_PERSONAS } from "@/content/personas";
import { ALL_POSTS } from "@/content/blog";

export default function sitemap(): MetadataRoute.Sitemap {
  const base = "https://coach-os.be";
  const now = new Date();
  return [
    { url: `${base}/`, lastModified: now, changeFrequency: "monthly", priority: 1 },
    ...ALL_PERSONAS.map((p) => ({
      url: `${base}/${p.slug}`,
      lastModified: now,
      changeFrequency: "monthly" as const,
      priority: 0.8,
    })),
    { url: `${base}/blog`, lastModified: now, changeFrequency: "weekly", priority: 0.7 },
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
            lastModified: now,
            changeFrequency: "monthly" as const,
            priority: 0.8,
          },
        ]
      : []),
    { url: `${base}/privacy`, lastModified: now, changeFrequency: "yearly", priority: 0.3 },
    { url: `${base}/voorwaarden`, lastModified: now, changeFrequency: "yearly", priority: 0.3 },
  ];
}
