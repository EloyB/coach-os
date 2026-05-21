# CoachOS — SEO + GEO Strategy

> Living document. Phase 1 ships in branch `onepager-website-setup`. Phases 2-4 are roadmap.

## Domain & market direction

**Primary domain: `coach-os.be`** (Belgium TLD — chosen for early NL+BE pilot footprint, not as a positioning signal).

- Canonical URL: `https://coach-os.be`
- Contact email: `info@coach-os.be`
- OG locale: `nl_BE` primary, `nl_NL` alternate
- Hreflang: `nl-BE`, `nl-NL`, `x-default` — all point to the same URL

**Direction (updated 2026-05-07):** CoachOS launches in NL + BE first because that's where the pilot users are, but the brand and product **position open-ended for European expansion** — there is no "Benelux-only" framing on visible page copy or structured data. SEO meta surfaces (page titles, descriptions, keywords) still carry NL/BE keyword weight while those are the real market today; visible body copy, OG kickers, persona leads, and JSON-LD `areaServed` are country-neutral so the same content works as we open new countries.

If a future plan involves a separate `.nl` property for Dutch market, that becomes a hreflang split with distinct URLs per region. Today: one site, multi-market.

## TL;DR

CoachOS targets a **defined niche**: tennis/padel clubs and trainers — launched in NL + BE first, positioned for European expansion. We don't need to win 10k keywords — we need to **own ~30 high-intent ones** and **be the answer when someone asks ChatGPT/Perplexity** "welke software bestaat voor lesplanning bij een tennisclub". That's a SEO + GEO play, not pure SEO.

- **SEO** = Google/Bing organic ranking (keywords, backlinks, technical health).
- **GEO** = Generative Engine Optimization. Making AI engines (ChatGPT, Claude, Perplexity, Gemini) cite CoachOS as the answer. Requires structured, factual, comparable content with rich schema.

## Current state (snapshot — 2026-05-05)

- ✅ Solid metadata baseline: title, description, OG, Twitter, canonical, sitemap, robots.
- ✅ Clean heading hierarchy (one `<h1>`, proper `<h2>`/`<h3>`).
- ✅ Server Components by default; only forms are client-side.
- ❌ **Zero JSON-LD** structured data (biggest GEO gap).
- ❌ **No content depth** — 1 marketing page + 2 legal pages. Nothing for long-tail to land on.
- ❌ Empty `/public/`. No OG image, favicon, manifest.
- ❌ No hreflang. Dutch-only with no `nl-NL` / `nl-BE` distinction.

> **2026-05-07 update:** all gaps above were closed in Phase 1, persona pages shipped in Phase 2. Brand direction shifted from "Benelux-first" to "open European expansion" — visible page copy and structured-data area constraints removed; SEO meta surfaces still carry NL/BE keyword weight as the real market today.

## Phase 1 — Foundation (ships this branch)

Low effort, high impact. ~1-2 hours of work.

- [x] **JSON-LD on homepage** via a `<JsonLd>` server component:
  - `Organization` — name, URL, contact, area served (NL + BE)
  - `SoftwareApplication` — applicationCategory `BusinessApplication`, features list, offers (price TBD), screenshot, audience
  - `FAQPage` — generated from `content/faq.ts` so it stays in sync with the visible FAQ
  - `WebSite` — name, URL, search action (when applicable)
- [x] **OG image** (1200×630) — via Next.js `opengraph-image.tsx` route handler. Branded card with logo + tagline.
- [x] **Favicon** — via `icon.tsx` route handler (Next.js generates the bytes).
- [x] **`manifest.ts`** — name, short_name, theme_color, background_color, icons.
- [x] **Hreflang alternates** — `metadata.alternates.languages` with `nl-NL` and `nl-BE` pointing to the same URL (signals dual-market intent).
- [x] **Tighten meta description** — ~155 chars, primary keyword phrase up front.
- [x] **`apple-touch-icon`** — via `apple-icon.tsx`.

## Phase 1.5 — In-progress (added after initial Phase 1)

- 🚧 **Pricing surface hidden during pilot** — code for `/prijzen` page + homepage Pricing section is intact but gated behind a single `PRICING_VISIBLE` flag in `website/content/pricing.ts`. Currently `false` while pilot pricing is being negotiated. When tarifering is finalised: flip the flag (and update the placeholder numbers in `PRICING_TIERS`). Hidden surface affects: homepage section render, `/prijzen` page (returns 404), Prijzen nav link, sitemap entry.
- ⏳ **`SoftwareApplication.offers`** in homepage JSON-LD is deliberately omitted while pricing is placeholder. Adding fake numbers to structured data would get them cached by Google/AI engines and cause embarrassment when real pricing lands. Wire in `offers` (priceCurrency `EUR`, one `Offer` per tier) once prices are final and `PRICING_VISIBLE = true` — see `website/components/site/json-ld.tsx`.
- ⏳ **Pricing-specific keywords** to monitor in Search Console once pricing is public: `prijs lesplanning tennisclub`, `kost lesplanning software`, `tarieven trainersplanning`, `gratis proefperiode tennisclub software`.

## Phase 2 — Content depth (the actual moat)

This is where most B2B SaaS landing pages stop and lose ranking + AI-citation share. Each new page is one more surface that can rank, get linked, and get quoted by AI.

**Persona pages** (one H1 each, own FAQ + schema):

- [x] `/voor-tennisclubs` — tennis club-specific pains (Excel chaos, KNLTB/Tennis Vlaanderen rapportering, jeugd vs volwassenen).
- [x] `/voor-padelclubs` — padel-specific (snelle groei, niveauverschillen, banen die les + vrij gebruik combineren, mobile-first).
- [x] `/voor-trainers` — zelfstandige coaches (Excel-routine, oneindig veel mails, geen admin-support).

Shipped state: each page has unique H1, Wikipedia-style lead paragraph (AI-quoteable), pains/solutions paired in two columns, local-SEO city ribbon, persona-specific FAQ, and `BreadcrumbList` + `FAQPage` + `Service` JSON-LD. Homepage `VoorWie` cards now link into the three pages. Sitemap updated.

Next under content depth:

**Blog / resources** — 5-10 cornerstone articles in Dutch:

- [x] "Hoe plan je een lesseizoen voor je tennisclub" — ranks for the literal query
- [x] "Anonieme inschrijving: AVG-conform leerlingen onboarden"
- [ ] "GDPR voor sportverenigingen: wat moet je regelen voor lesinschrijvingen"
- [ ] "Wat kost lesplanning-software in 2026" — comparison-style, cites pricing (write when `PRICING_VISIBLE` flips on)
- [ ] "Lesplanning in Excel: waarom het schaalt tot ~50 leden en wat daarna"
- [ ] "Magic-link bevestigingen: waarom leerlingen geen account meer hoeven"

**Blog infra shipped (2026-05-07):** `/blog` index + `/blog/[slug]` static-prerendered routes; structured-content posts in `content/blog/posts/`; `BlogPosting` + `BreadcrumbList` + optional `FAQPage` JSON-LD per article; `Blog` aggregate schema on the index; footer link in the Product column; sitemap auto-iterates `ALL_POSTS`. Each post has lead paragraph (Wikipedia-style opener for AI quoting), tagged sections with paragraphs/bullets/callouts, optional FAQ, and related-post linking. Articles pull double duty: rank long-tail in Google + get cited verbatim by ChatGPT/Perplexity.

**Comparison pages** (later, when competitors are confirmed):

- `/vs/[competitor]` per competitor. High-intent, low-competition for niche players.
- One "alternatives to X" page per competitor.

**Why this works**: Long-tail queries are where small SaaS wins. AI engines also pull verbatim from these pages — a well-structured comparison page becomes the AI's answer.

## Phase 3 — GEO-specific tactics

AI engines cite content that is:

1. **Factually structured.** ✅ Lead paragraphs across the homepage, persona pages, and blog posts all open Wikipedia-style. Definition first, no buried lede.
2. **Listed and comparable.** ✅ "Excel vs CoachOS" comparison section shipped on the homepage (2026-05-07). Six recurring lesplanning-taken, each with a side-by-side before/after card. AI-quoteable copy, inline visual contrast (✗/✓), no schema needed — the structured prose itself is the GEO play. _Note: positioned between FeatureGrid and BespaarTijd as a homepage section. If it clutters the page, candidate to lift into a dedicated `/excel-vs-coachos` blog post or comparison page._
3. **FAQ-rich.** ✅ Homepage FAQ expanded from 7 → 17 questions (2026-05-07). Each persona page has its own 4-Q FAQ; each blog post has 4-5 Q. Cumulative `FAQPage` schema across the site is now substantial.
4. **Mentioned elsewhere.** Operational outreach, not code:
   - Capterra NL, GetApp, G2, Software Advice
   - Emerce, Frankwatching guest posts
   - Tennisnet, KNLTB / Tennis Vlaanderen partner directories
   - Padel-specific media (Padelmagazine, etc.)

**AI-engine infrastructure (shipped 2026-05-07):**

- ✅ `/llms.txt` route — emerging convention from [llmstxt.org](https://llmstxt.org); generated dynamically from `ALL_PERSONAS` and `POSTS_BY_DATE` so it auto-updates as content ships. Major AI engines (Anthropic, OpenAI, Perplexity) read this when they encounter the domain.
- ✅ `robots.txt` — explicit allow for 12 AI crawlers (GPTBot, ChatGPT-User, OAI-SearchBot, ClaudeBot, anthropic-ai, PerplexityBot, Perplexity-User, Google-Extended, CCBot, Applebot-Extended, Bytespider, DuckAssistBot). Removes the risk of default-deny on bots that respect named user-agents.

**Concrete GEO checklist for each new page:**

- One-sentence definition in the first paragraph
- Bulleted feature/benefit lists (not just prose)
- A comparison or "wel/niet" block where it fits
- Inline FAQ with `FAQPage` schema
- `BreadcrumbList` schema
- Internal link to homepage and at least one persona page

## Phase 4 — Geographic SEO (per-market playbook)

While the brand positions for European expansion, organic traffic today comes from NL + BE — that's where geo-tactics get applied **first**. The same playbook is then repeatable per new country.

**Today (NL + BE):**

- `Organization` schema with address + region (added once a registered entity exists)
- Once live: Google Business Profile (NL or BE entity)
- KvK / KBO listing visible on legal pages
- City mentions in landing copy where Search Console shows local intent (e.g., "tennisclubs in Amsterdam, Antwerpen, Rotterdam, Gent, Den Haag, Brussel"). _Note (2026-05-07):_ the persona pages currently have **no city ribbon** — removed when the open-market direction was set, since pinning visible copy to specific Benelux cities contradicts the expansion stance. When SEO data justifies it, re-introduce city mentions on a per-page basis (e.g., a Belgium-only landing page or a regional comparison post), not on persona pages that should travel.

**When expanding to a new country:** replicate the same surfaces — local entity in `Organization` schema, country-specific Google Business Profile, regional registry (KvK / KBO / Companies House / etc.), city mentions in country-targeted pages — without changing the persona pages or homepage messaging.

## Keyword targets (working list — refine when search-console data comes in)

**Primary:**

- lesplanning tennisclub
- lesplanning padelclub
- tennisles inschrijven software
- padel lesreeksen
- ledenadministratie tennisclub
- trainersplanning tennis

**Long-tail / problem-aware:**

- hoe plan je tennislessen
- alternatief voor excel tennisclub
- automatische inschrijvingen tennisclub
- AVG-proof leerlingen inschrijven sportclub
- magic link inschrijving sportclub

**Comparison (later):**

- coachos vs [competitor]
- alternatief voor [competitor]
- beste lesplanning software tennis 2026

## Measurement

When the site is live, set up:

- Google Search Console (NL + BE properties)
- Bing Webmaster Tools (Bing feeds Copilot)
- Plausible / Umami (privacy-friendly analytics — fits the AVG positioning)
- Track: AI-referrer traffic (`chatgpt.com`, `perplexity.ai`, `gemini.google.com` user-agents/referers) separately

## Useful references

- [Google Search Central — JSON-LD](https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data)
- [Schema.org SoftwareApplication](https://schema.org/SoftwareApplication)
- [Next.js Metadata API](https://nextjs.org/docs/app/api-reference/functions/generate-metadata)
- [Next.js opengraph-image](https://nextjs.org/docs/app/api-reference/file-conventions/metadata/opengraph-image)
- [hreflang for multi-region](https://developers.google.com/search/docs/specialty/international/localized-versions)
