# CoachOS — SEO + GEO Strategy

> Living document. Phase 1 ships in branch `onepager-website-setup`. Phases 2-4 are roadmap.

## Domain

**Primary domain: `coach-os.be`** (Belgium TLD, Benelux-first positioning).

- Canonical URL: `https://coach-os.be`
- Contact email: `hallo@coach-os.be`
- OG locale: `nl_BE` primary, `nl_NL` alternate
- Hreflang: `nl-BE`, `nl-NL`, `x-default` — all point to the same URL

If a future plan involves a separate `.nl` property for Dutch market, that becomes a hreflang split with distinct URLs per region. Today: one site, two markets.

## TL;DR

CoachOS targets a **small, defined market**: tennis/padel clubs in NL + BE. We don't need to win 10k keywords — we need to **own ~30 high-intent ones** and **be the answer when someone asks ChatGPT/Perplexity** "welke software bestaat voor lesplanning bij een tennisclub". That's a SEO + GEO play, not pure SEO.

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

- ✅ **Pricing page** (`/prijzen`) shipped with placeholder tiers + comparison table + pricing-specific FAQ. Page-level JSON-LD: `BreadcrumbList` + `FAQPage`.
- ⏳ **`SoftwareApplication.offers`** in homepage JSON-LD is deliberately omitted while pricing is placeholder. Adding fake numbers to structured data would get them cached by Google/AI engines and cause embarrassment when real pricing lands. Wire in `offers` (priceCurrency `EUR`, one `Offer` per tier) once prices are final — see `website/components/site/json-ld.tsx`.
- ⏳ **Pricing-specific keywords** to monitor in Search Console once live: `prijs lesplanning tennisclub`, `kost lesplanning software`, `tarieven trainersplanning`, `gratis proefperiode tennisclub software`.

## Phase 2 — Content depth (the actual moat)

This is where most B2B SaaS landing pages stop and lose ranking + AI-citation share. Each new page is one more surface that can rank, get linked, and get quoted by AI.

**Persona pages** (one H1 each, own FAQ + schema):

- `/voor-tennisclubs` — pain points and benefits specific to tennis clubs (multiple courts, seasonal series, federation reporting).
- `/voor-padelclubs` — padel-specific (rapid growth, often shared with tennis, mixed-level groups).
- `/voor-trainers` — for the independent coach audience already in `content/audiences.ts`.

**Blog / resources** — 5-10 cornerstone articles in Dutch:

- "Hoe plan je een lesseizoen voor je tennisclub" — ranks for the literal query
- "GDPR voor sportverenigingen: wat moet je regelen voor lesinschrijvingen"
- "Wat kost lesplanning-software in 2026" — comparison-style, cites pricing
- "Lesplanning in Excel: waarom het schaalt tot ~50 leden en wat daarna"
- "Magic-link bevestigingen: waarom leerlingen geen account meer hoeven"
- "Anonieme inschrijving: AVG-conform leerlingen onboarden"

**Comparison pages** (later, when competitors are confirmed):

- `/vs/[competitor]` per competitor. High-intent, low-competition for niche players.
- One "alternatives to X" page per competitor.

**Why this works**: Long-tail queries are where small SaaS wins. AI engines also pull verbatim from these pages — a well-structured comparison page becomes the AI's answer.

## Phase 3 — GEO-specific tactics

AI engines cite content that is:

1. **Factually structured.** Lead paragraphs that read like Wikipedia: *"CoachOS is een lesplanningsysteem voor tennis- en padelclubs in Nederland en België. Het ondersteunt lesreeksen, anonieme inschrijvingen, automatische scheduling en magic-link bevestigingen."* Don't bury the definition.
2. **Listed and comparable.** Bullet lists of features, pricing tables, "wel/niet" comparisons. AI loves clean, parseable structure.
3. **FAQ-rich.** Expand FAQ from 7 to 15-20 questions covering pricing, GDPR, integrations, migrations, multi-club, federation reporting.
4. **Mentioned elsewhere.** AI engines weigh third-party mentions heavily:
   - Capterra NL, GetApp, G2, Software Advice
   - Emerce, Frankwatching guest posts
   - Tennisnet, KNLTB / Tennis Vlaanderen partner directories
   - Padel-specific media (Padelmagazine, etc.)

**Concrete GEO checklist for each new page:**

- One-sentence definition in the first paragraph
- Bulleted feature/benefit lists (not just prose)
- A comparison or "wel/niet" block where it fits
- Inline FAQ with `FAQPage` schema
- `BreadcrumbList` schema
- Internal link to homepage and at least one persona page

## Phase 4 — Local SEO

- `Organization` schema with address + region
- Mention concrete cities/regions naturally in audience pages: "tennisclubs in Amsterdam, Antwerpen, Rotterdam, Gent, Den Haag, Brussel"
- Once live: Google Business Profile (NL or BE entity)
- KvK / KBO listing visible on legal pages

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
