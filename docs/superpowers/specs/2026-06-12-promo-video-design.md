# CoachOS app-promo-video: ontwerp

**Datum:** 2026-06-12
**Status:** Ontwerp (goedgekeurd in gesprek, secties 1 t/m 3)

## Doel

Een nieuwe, ambitieuze app-promo-video ("Dribbble-niveau": zwevende UI-kaarten,
3D-tilt, parallax, kinetic typography, spring-physics) die de vier kernfeatures
van CoachOS toont. De bestaande 30s-video (`marketing/video/out/coachos.mp4`)
blijft staan; dit is een nieuwe productie die hem in de praktijk vervangt.

## Kanalen en vormen

- **Website (coach-os.be)** en **LinkedIn:** 16:9 master, 1920×1080.
- **Instagram Reels:** 9:16 versie, 1080×1920.
- **Zonder geluid te volgen** (autoplay/feed wordt muted bekeken). Muziek is een
  optionele laag achteraf, geen drager. Audio is buiten scope voor deze versie.
- Lengte: **~35 seconden** (1050 frames @ 30fps), punchy tempo.

## Features in de video (alle vier, bevestigd)

1. Lessenreeks opzetten + één deellink
2. Inschrijven zonder account (magic-link, telefoon)
3. Auto-planner (heromoment)
4. Betalingen via Bancontact en iDEAL

## Storyboard (~35s)

| # | Tijd | Scène | Inhoud |
|---|------|-------|--------|
| 1 | 0–4s | Hook (kinetic type) | Witte woorden knallen gestapeld in beeld: "Drie weekenden." / "Excel." / "WhatsApp." / "Betaalverzoekjes." met lichte kantel/schud (chaos). Smash-cut, alles veegt weg: **"Of één middag."** met lime punt. |
| 2 | 4–10s | Lessenreeks + één link | Zwevende app-kaart (3D-tilt) waarin de wizard zich snel invult (naam, prijs, tijdsloten cascaden). Tweede kaartje floept eruit: de deellink met kopieer-tik. Headline: "Zet je reeks op. Deel één link." |
| 3 | 10–16s | Inschrijven zonder account | PhoneFrame zweeft in met parallax. WhatsApp-bubbel met link → tap → formulier vult zich → succes-vinkje veert op. Headline: "Spelers schrijven zich in. Zonder account." |
| 4 | 16–24s | Auto-planner (langste scène) | Leeg weekrooster → "Plan lessen" ingedrukt → 12 spelers cascaden kleurgecodeerd in het grid, conflicten lossen zichtbaar op, stat-badge veert in ("12 spelers · 0 conflicten"). Subtiele camera-zoom. Headline: "De planner doet de puzzel." |
| 5 | 24–30s | Betalingen | Betaaloverzicht: rijen springen van "open" naar "betaald" (lime vinkjes ratelen binnen), iDEAL- en Bancontact-badges zweven ernaast. Headline: "Betaald via Bancontact en iDEAL. Zonder achternajagen." |
| 6 | 30–35s | Afsluiter | Wipe naar donker podium. Monogram veert in, dan: **"1 middag · 0 Excel · 1 tool."** en `coach-os.be`, lime accentstrip onderaan. |

Per scène: headline groot in Inter 800 (links in 16:9, boven in 9:16), de
UI-demo als zwevende kaart ernaast/eronder. Copy volgt `voice.md`: geen
em-dashes, gewone spreektaal, concrete getallen, Benelux-neutraal.

## Visueel systeem

- **Podium:** donker `#161513` met court-line-patroon op ~7% opacity, consistent
  met de social cards.
- **Zwevende kaarten:** `perspective`-tilt (±4 à 6 graden), diepe zachte schaduw,
  trage "adem"-float van enkele pixels, parallax tussen achtergrond, kaart en
  headline bij scènewissels.
- **Motion:** spring-physics (damping ~13, stiffness ~130) en bezier-easing,
  geen lineaire fades. Scènewissels zijn snelle slides/wipes met overshoot,
  geen crossfades.
- **Typografie:** kinetic type in Inter 800 wit met lime leestekens; mono-tags
  in JetBrains Mono lime met letter-spacing per scène ("01 · LESSENREEKS").
- **Kleur:** lime is het enige accent. Niveau-kleuren (lime/blauw/koraal) alleen
  binnen het auto-planner-grid.

## Technische opzet

- **Locatie:** bestaand Remotion 4-project `marketing/video/`. De oude MainVideo
  en scenes blijven onaangeroerd.
- **Structuur:** nieuwe map `src/promo/` met:
  - per scène een bestand: `HookScene`, `SeriesScene`, `EnrollScene`,
    `PlannerScene`, `PaymentsScene`, `OutroScene`
  - gedeelde componenten: `FloatingCard` (tilt/float/schaduw), `KineticText`
    (woord-voor-woord spring-entry), scène-tag
  - `layout.ts`: alle posities/maten per formaat (16:9 vs 9:16)
  - `mocks/`: geporte UI-mock-componenten uit
    `marketing/social-media-posts/animations/src/` (wizard, planner-grid,
    phone-flow, betaaloverzicht). Twee losse npm-projecten, dus kopiëren in
    plaats van cross-project imports. Tokens gelijk aan `brand.ts`.
- **Composities:** `Promo` (1920×1080) en `PromoVertical` (1080×1920) delen
  dezelfde scènes via een `format`-prop; `layout.ts` regelt de verschillen.
- **Scripts:** `render:promo` → `out/promo.mp4`, `render:promo-vertical` →
  `out/promo-vertical.mp4` (h264). Review via gerenderde stills in het gesprek
  en live via `npm run studio`.

## Buiten scope (bewust)

- Audio/muziek/SFX (kan later via Remotion `<Audio>`).
- Vervangen of verwijderen van de bestaande MainVideo.
- Echte schermopnames van de app (we gebruiken gestileerde UI-mocks).
- Aparte korte cutdowns (teasers per feature); de bestaande losse
  feature-animaties dekken dat al.

## Succescriterium

Twee gerenderde bestanden (`promo.mp4` 16:9 en `promo-vertical.mp4` 9:16) van
~35s die het storyboard volgen, zonder geluid te volgen zijn, on-brand (tokens,
voice) en met de beschreven motion-taal. Eloy keurt het eindresultaat visueel
goed na review van stills/preview.
