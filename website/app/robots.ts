import type { MetadataRoute } from "next";

/**
 * Common disallow paths shared across all rule blocks. `/api/` is server-only,
 * `/bedankt` is a post-submit thank-you page that shouldn't get indexed.
 */
const DISALLOW = ["/api/", "/bedankt"];

/**
 * AI-engine user-agents that we explicitly allow. Some bots default-deny if
 * not named (notably `Google-Extended`); spelling them out removes that risk
 * and signals to operators that AI crawling is intentional, not accidental.
 *
 * - GPTBot / ChatGPT-User / OAI-SearchBot, OpenAI
 * - ClaudeBot / anthropic-ai, Anthropic
 * - PerplexityBot / Perplexity-User, Perplexity
 * - Google-Extended, Google's separate AI training opt-out toggle
 * - CCBot, Common Crawl (feeds many open-source LLMs)
 * - Applebot-Extended, Apple Intelligence
 * - Bytespider, ByteDance / Doubao
 * - DuckAssistBot, DuckDuckGo
 */
const AI_USER_AGENTS = [
  "GPTBot",
  "ChatGPT-User",
  "OAI-SearchBot",
  "ClaudeBot",
  "anthropic-ai",
  "PerplexityBot",
  "Perplexity-User",
  "Google-Extended",
  "CCBot",
  "Applebot-Extended",
  "Bytespider",
  "DuckAssistBot",
];

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
        disallow: DISALLOW,
      },
      {
        userAgent: AI_USER_AGENTS,
        allow: "/",
        disallow: DISALLOW,
      },
    ],
    sitemap: "https://coach-os.be/sitemap.xml",
  };
}
