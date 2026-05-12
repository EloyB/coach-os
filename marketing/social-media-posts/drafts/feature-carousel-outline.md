# Feature Carousel Outline — "Je lessenseizoen, in 4 schermen"

A 6-slide Instagram carousel walking through the 4 core CoachOS features as a sequential user journey. Hand this to Claude Code as a spec for the Remotion animations; the captions and posting checklist are for you.

---

## TL;DR

| Slide | Type | What it is | Source |
|-------|------|------------|--------|
| 1 | Static PNG | Hook card — "4 schermen. 1 lessenseizoen." | Brand SVG (I can build this) |
| 2 | MP4 (≈5s) | Animation — Trainer maakt lessenreeks aan | Claude Code + Remotion |
| 3 | MP4 (≈5s) | Animation — Trainer publiceert inschrijfformulier | Claude Code + Remotion |
| 4 | MP4 (≈5s) | Animation — Speler schrijft zich in via magic-link | Claude Code + Remotion |
| 5 | MP4 (≈5s) | Animation — Auto-planner berekent rooster | Claude Code + Remotion |
| 6 | Static PNG | CTA card — "Wees bij de eerste 5" + DM ons | Brand SVG (I can build this) |

**Format for all slides:** 1080×1080 (square). Same dimensions across all 6 = consistent in feed. Each video slide is a single non-looping clip.

---

## Slide-by-slide spec

### Slide 1 — Hook (static)

**Purpose:** Stop the scroll. Make people want to swipe.

**Visual concept:**
- Standard CoachOS dark card system (matches launch + week-1 cards)
- Hero text, two lines stacked:
  - `4 schermen.`
  - `1 lessenseizoen.`
- Subhero (lime, smaller): `Swipe door →`
- Bottom-right slide indicator: `1 / 6`
- Lime accent strip at bottom

**Why this hook:** Mirrors the "1 middag · 0 Excel · 1 tool" rhythm from your existing cards. Promises completion (whole flow in 4 swipes) which drives the swipe action.

---

### Slide 2 — Animation: Trainer maakt lessenreeks aan

**Purpose:** Show how fast the setup is. Counter the "this will be complicated to set up" objection.

**Storyboard (5 sec):**

| Time | What happens on screen |
|------|------------------------|
| 0.0–0.7s | Title bar slides in: "1. Maak je lessenreeks aan" (lime mono on dark) |
| 0.7–1.5s | Form panel fades in — empty fields visible (Naam, Periode, Aantal weken, Banen, Trainers) |
| 1.5–3.0s | Cursor types "Voorjaarsreeks 2026" in name field; date picker animates (1 mrt → 30 mei); numbers tick up: weken=12, banen=8, trainers=4 |
| 3.0–4.0s | "Aanmaken" button highlights → click → button compresses |
| 4.0–5.0s | Success state: lessenreeks card appears, lime checkmark animates in, stat appears: "Klaar in 30 seconden." |

**What Claude Code should pull from your codebase:** Your actual lessenreeks creation form component. Real form layout, real field labels, real date picker. The animation just simulates the user typing/clicking.

**Mock data to use:**
- Lessenreeks naam: `Voorjaarsreeks 2026`
- Start: `1 maart 2026`
- Einde: `30 mei 2026`
- Weken: `12`
- Banen: `8`
- Trainers: `4`

---

### Slide 3 — Animation: Trainer publiceert inschrijfformulier

**Purpose:** Show that the enrollment form is auto-generated and shareable as a single link.

**Storyboard (5 sec):**

| Time | What happens on screen |
|------|------------------------|
| 0.0–0.7s | Title bar: "2. Publiceer het inschrijfformulier" |
| 0.7–2.0s | Form builder view fades in — form fields appear in cascade (Naam, Niveau, Beschikbaarheden, Voorkeuren) |
| 2.0–3.0s | Preview pane slides in from right showing the live form |
| 3.0–3.7s | "Publiceer" button highlights → click → page transitions |
| 3.7–5.0s | Public link appears centered: `coach-os.be/r/voorjaar-2026` with a "Kopieer link" button. Subtle "✓ Gekopieerd" confirmation animates in. Stat: "Klaar in 1 minuut." |

**What Claude Code should pull:** Your actual form builder UI + the public registration page preview component.

**Mock data:**
- Public URL: `coach-os.be/r/voorjaar-2026` (or whatever your real URL pattern is)
- Form fields: whatever defaults your app actually generates

---

### Slide 4 — Animation: Speler schrijft zich in (magic-link)

**Purpose:** Show the student-side experience — frictionless, no account required.

**Storyboard (5 sec):**

| Time | What happens on screen |
|------|------------------------|
| 0.0–0.7s | Title bar: "3. Spelers schrijven zich in" |
| 0.7–1.5s | Phone mockup appears center-screen, WhatsApp/email open showing the shared link |
| 1.5–2.2s | Tap animation on the link → mini transition → registration form opens in mobile browser |
| 2.2–3.5s | Quick fill: name auto-types (e.g., "Sven Janssens"), niveau dropdown opens and selects "Gevorderd", availability toggles (Ma + Wo + Vr highlight) |
| 3.5–4.2s | "Inschrijven" button → click → loading spinner brief |
| 4.2–5.0s | Success state: "✓ Inschrijving bevestigd" with subtitle "We sturen je een mail zodra de planning klaar is." Lime checkmark, no account creation step shown anywhere. |

**Critical detail to emphasize:** No "Maak account" or "Wachtwoord" step exists in this flow. The whole point is showing the absence. Make sure the student flow visibly skips any auth step.

**What Claude Code should pull:** Your actual student registration page rendered at mobile viewport width inside a phone mockup frame.

**Mock student:**
- Naam: `Sven Janssens`
- Niveau: `Gevorderd`
- Beschikbaarheden: `Maandag, Woensdag, Vrijdag`

---

### Slide 5 — Animation: Auto-planner

**Purpose:** The hero feature. The "look at this magic moment" payoff.

**Storyboard (5 sec):**

| Time | What happens on screen |
|------|------------------------|
| 0.0–0.7s | Title bar: "4. CoachOS plant het rooster" |
| 0.7–1.5s | Sidebar shows player roster (16 names with level color dots). Empty schedule grid (4 banen × 4 tijdslots) appears center. |
| 1.5–2.2s | "Genereer planning" button highlights → click → "Planning berekenen…" shimmer effect briefly |
| 2.2–4.2s | Cells fill in cascade: each cell receives 1–2 player names + a level color stripe, lime tint deepens as the cell "locks in." Order: top-left first, snake-fill across the grid. |
| 4.2–5.0s | Lime success badge springs in bottom: "✓ Planning klaar in 0,8 sec" with the time number animating from 0.0 to 0.8 |

**What Claude Code should pull:** Your actual schedule grid component + player list component. If your real planner takes longer than 0.8 sec, use the fastest realistic number — but don't lie. If it's actually 3 sec, say "Klaar in 3 sec."

**Mock data:**
- 16 spelers across 3 niveaus (use Sven, Marit, Tom, Eva, Lars, Lieve, Wout, Anke, Bart, Ine, Joris, Mila, Ruben, Sara, Toon, Ella)
- Color code: lime = beginner, blue = gevorderd, coral = expert (or whatever your app uses)

---

### Slide 6 — CTA (static)

**Purpose:** Convert. Drive DMs for the pilot offer.

**Visual concept:**
- Standard dark card system
- Top-right slide indicator: `6 / 6`
- Hero text: `Wees bij de eerste 5.`
- Sub-tagline (lime): `1 maand gratis · levenslang 25% korting`
- Mono CTA bottom-left (lime): `DM ons →`
- Big URL bottom-right (white): `coach-os.be`
- Lime accent strip at bottom

**Why this CTA:** Continues the launch-post promo through the carousel — every post supports the active campaign instead of having competing CTAs.

---

## Captions

### Instagram caption

```
4 schermen.
1 lessenseizoen.

Zo werkt CoachOS in vier stappen:

→ Maak je lessenreeks aan (30 sec)
→ Publiceer het inschrijfformulier (1 min)
→ Spelers schrijven zich in via een link (geen accounts)
→ De auto-planner berekent het rooster (0,8 sec)

Wat je vroeger drie weekenden Excel kostte, doe je nu in één middag.

Voor tennis- en padeltrainers in BE & NL.

We zoeken nog 5 testers. 1 maand gratis, levenslang 25% korting nadien. DM ons of mail naar info@coach-os.be.
```

**Hashtags (add at end of caption or in first comment):**

```
#padelclub #tennisclub #padelbelgie #vlaanderen #tennisleraar #tennisschool #lesplanning #padeltrainer #saasnl #buildinpublic #padel #tennis
```

### LinkedIn caption (CoachOS company page)

```
Een lessenseizoen plannen kostte vroeger drie weekenden in Excel.

Met CoachOS doe je het in één middag — in vier stappen:

→ Maak je lessenreeks aan (30 sec)
→ Publiceer het inschrijfformulier (1 min)
→ Spelers schrijven zich in via één link, zonder account (magic-link)
→ De auto-planner berekent het rooster in 0,8 seconden

In de carousel hieronder zie je alle vier in actie.

Voor tennis- en padeltrainers in BE & NL.

We zoeken nog 5 testers. 1 maand gratis, levenslang 25% korting nadien. DM ons of mail naar info@coach-os.be.

#lesplanning #padelbelgie #tennisnederland #saas #buildinpublic
```

**First comment (LinkedIn, auto-post):**

```
→ coach-os.be
```

---

## Posting checklist

**Suggested timing:** Friday 07:30 CET (Instagram), 08:30 CET (LinkedIn) — matches the existing posting rhythm.

**Order of operations:**

1. Claude Code renders the 4 MP4 animations (slides 2–5). Output each as `out/feature-N-name.mp4`.
2. I (or you) generate the 2 static PNG cards (slides 1 + 6). Tell me when you're ready and I'll deliver them.
3. AirDrop or transfer all 6 files to your phone in order.
4. **Instagram:** Open IG → tap `+` → Post → tap multi-select icon → select all 6 in order → swipe to "Edit" → confirm preview → Next → paste caption → Share.
5. **LinkedIn:** Open LinkedIn web → New post → Add document/media → upload all 6 in order → paste caption → post → add the first comment immediately.
6. Have your DM template ready (see launch-instagram.md notes).

**Quality gates before posting:**

- [ ] All 4 MP4s play correctly (no black frames, no glitches)
- [ ] All 6 slides feel visually consistent (same brand, same dimensions, smooth flow when swiping)
- [ ] Caption code blocks copy-paste cleanly
- [ ] Bio link points to coach-os.be (or pilot signup page)
- [ ] You're available for the next 2 hours after posting to handle DMs

---

## Notes for Claude Code

When you (Eloy) hand this off, tell Claude Code:

1. **Use the existing components from the CoachOS codebase wherever possible.** Don't recreate UI — pull the real `LessonSeriesForm`, `RegistrationForm`, `Schedule`, etc. components and animate them with Remotion's `interpolate()` and `spring()`.
2. **Brand tokens are in the existing project.** Use the same Tailwind config (lime `#D0FF14`, dark `#161513`, Inter font) so the animations match the brand exactly.
3. **Each composition is 1080×1080, 30fps, 150 frames (5 sec).**
4. **Focus on a single clear cause-and-effect per clip** — type → typed text appears, click → state changes. Don't try to show everything; show the *moment* that matters.
5. The Remotion scaffold I created (in `animations/`) can be referenced for structure, but Claude Code should rebuild inside the actual app codebase since it has access to the real components.
6. **Test render at 1x before final.** Render at half resolution first to iterate fast on timing, then bump to 1080×1080 once everything feels right.

---

## Future iterations

Once this carousel is out, possible follow-ups:

- **Single-feature deep-dives.** Each of the 4 features could be its own dedicated post later (more detail, longer video, more context). Use this overview carousel as the "trailer" and the deep-dives as the "feature films."
- **Customer story versions.** Once you have pilot users, swap the mock data (Sven Janssens, etc.) for real club names and quotes — same template, much higher credibility.
- **Localized variants.** When you open NL pilots, render a copy of each animation with NL-specific data (Dutch names, Dutch club names) for that market segment.
