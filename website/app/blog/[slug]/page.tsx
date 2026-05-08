import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogArticle } from "@/components/sections/blog-article";
import { ALL_POSTS, getPostBySlug } from "@/content/blog";

const SITE_URL = "https://coach-os.be";

interface PageParams {
  params: Promise<{ slug: string }>;
}

export function generateStaticParams() {
  return ALL_POSTS.map((p) => ({ slug: p.slug }));
}

export async function generateMetadata({
  params,
}: PageParams): Promise<Metadata> {
  const { slug } = await params;
  const post = getPostBySlug(slug);
  if (!post) return {};

  const pageUrl = `${SITE_URL}/blog/${post.slug}`;

  return {
    title: post.metaTitle,
    description: post.metaDescription,
    keywords: post.tags,
    alternates: {
      canonical: pageUrl,
      languages: {
        "nl-BE": pageUrl,
        "nl-NL": pageUrl,
        "x-default": pageUrl,
      },
    },
    openGraph: {
      type: "article",
      locale: "nl_BE",
      alternateLocale: ["nl_NL"],
      url: pageUrl,
      title: post.metaTitle,
      description: post.metaDescription,
      siteName: "CoachOS",
      publishedTime: post.publishedAt,
      ...(post.updatedAt ? { modifiedTime: post.updatedAt } : {}),
    },
  };
}

export default async function BlogPostPage({ params }: PageParams) {
  const { slug } = await params;
  const post = getPostBySlug(slug);
  if (!post) notFound();
  return <BlogArticle post={post} />;
}
