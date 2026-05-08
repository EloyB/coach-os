import type { BlogPost } from "@/content/blog/types";
import { HOE_PLAN_JE_LESSEIZOEN_TENNISCLUB } from "@/content/blog/posts/hoe-plan-je-lesseizoen-tennisclub";
import { ANONIEME_INSCHRIJVING_AVG } from "@/content/blog/posts/anonieme-inschrijving-avg";

/** All published blog posts. New posts: import + add here. */
export const ALL_POSTS: BlogPost[] = [
  HOE_PLAN_JE_LESSEIZOEN_TENNISCLUB,
  ANONIEME_INSCHRIJVING_AVG,
];

/** Posts sorted newest-first by publishedAt — used on the index page. */
export const POSTS_BY_DATE: BlogPost[] = [...ALL_POSTS].sort((a, b) =>
  b.publishedAt.localeCompare(a.publishedAt),
);

export function getPostBySlug(slug: string): BlogPost | undefined {
  return ALL_POSTS.find((p) => p.slug === slug);
}

export function getRelatedPosts(post: BlogPost): BlogPost[] {
  if (!post.related?.length) return [];
  return post.related
    .map((slug) => getPostBySlug(slug))
    .filter((p): p is BlogPost => Boolean(p));
}

/** Format ISO date as nl-BE display: "7 mei 2026". */
export function formatDateNL(iso: string): string {
  return new Date(iso).toLocaleDateString("nl-BE", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

export type { BlogPost, BlogSection } from "@/content/blog/types";
