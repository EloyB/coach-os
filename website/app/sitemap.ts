import type { MetadataRoute } from "next";
import { PRICING_VISIBLE } from "@/content/pricing";

export default function sitemap(): MetadataRoute.Sitemap {
  const base = "https://coach-os.be";
  const now = new Date();
  return [
    { url: `${base}/`, lastModified: now, changeFrequency: "monthly", priority: 1 },
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
