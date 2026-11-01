import type { Metadata } from "next";
import { BlogIndex } from "@/components/sections/blog-index";
import { ALL_POSTS } from "@/content/blog";
import { pageMetadata } from "@/lib/seo";

export const metadata: Metadata = pageMetadata({
  path: "/blog",
  title: "Blog",
  description:
    "Gidsen en achtergronden over lessenplanning voor tennis- en padelclubs: seizoensplanning, inschrijvingen en kampen.",
  socialDescription:
    "Praktische gidsen over lessenplanning voor tennis- en padelclubs.",
  // Zolang er geen artikels zijn blijft de pagina uit de zoekresultaten en uit
  // de sitemap. Zodra er een artikel in ALL_POSTS staat, is hij weer indexeerbaar.
  noindex: ALL_POSTS.length === 0,
});

export default function BlogPage() {
  return <BlogIndex />;
}
