# SEO-plan en vervolgstappen voor coach-os.be

**Datum:** 1 november 2026
**Status:** voorstel om met de compagnon door te nemen. Niets hiervan is beslist.
**Scope:** de marketingsite in `website/` (de app op `app.coach-os.be` staat op `noindex`).

---

## 1. Waar staan we nu

### Wat al gedaan is (PR #281, #282, #283)

- Titels, metabeschrijvingen en koppen met zoekwoorden voor de homepage, `/prijzen`, `/voor-tennisclubs`, `/voor-padelclubs` en `/voor-trainers`.
- Uitgebreide inhoud per doelgroeppagina, nieuwe FAQ-vragen (betalen, kampen en stages, prijs, tennis en padel samen).
- JSON-LD: `SoftwareApplication` met prijzen (ook op `/prijzen`), `FAQPage`, `BreadcrumbList`, `BlogPosting`.
- Inhoudelijke correcties: "zonder account" in plaats van "anoniem", het algoritme verdeelt op voorkeurstijd en groepsverband (niet op niveau), betalen na bevestiging van de planning, vaste schrijfwijze "lessenplanning" en slogan "Lessenplanning, eindelijk eenvoudig."
- `noindex` voor de hele app (`app.coach-os.be`).
- Twee blogartikelen op een branch (zie sectie 5). Het tweede (GDPR) wordt nog niet gebruikt.

### Wat Search Console toont (stand 31 oktober 2026)

| Meting | Waarde |
|---|---|
| Property | `coach-os.be` (domeineigenschap, dus inclusief subdomeinen) |
| Klikken (3 maanden) | 131, ongeveer 1 tot 2 per dag |
| Geïndexeerde pagina's | 21 |
| Niet geïndexeerde pagina's | 32 |
| Sitemap | `sitemap.xml`, status "Success", 8 pagina's ontdekt |

**Alle zoekopdrachten zijn merknaam:** "coach os" (49 klikken, 72 weergaven), "coachos" (21 klikken, 36 weergaven), "coaching os" (2 klikken). Er is **geen enkele klik op een zoekopdracht zonder merknaam**. We worden dus enkel gevonden door mensen die ons al kennen.

**Waarom 32 pagina's niet geïndexeerd zijn:** meer dan de helft van de geïndexeerde en gecrawlde URL's stond op `app.coach-os.be` (`/`, `/register`, `/forgot-password`, `/student-login`, `/enroll/<id>`, `/camp/<id>`). Dat is opgelost met `noindex` (PR #282). De rest is ruis: iconen, `opengraph-image`, `manifest.webmanifest`, lettertypen, oude 404's (`/uitnodiging`, `/forgot-password`).

**Opmerkingen**
- "coach os" met spatie wordt vaker gezocht dan "coachos". Daarom staat "(ook geschreven als Coach OS)" in de FAQ "Wat is CoachOS?".
- "coachview inloggen" is een verwarring met een ander product.
- Een publieke inschrijfpagina (`/camp/<id>`) kreeg een weergave op "opvang herfstvakantie". Die pagina's staan nu op `noindex`; later bewust herbekijken (eigen titel en beschrijving per kamp).

---

## 2. Concurrent: TrainYou (trainyou.nl)

Bekeken op 31 oktober 2026: homepage, `/prijzen` en sitemap. Rankings en verkeer konden we niet zien (daarvoor is een tool als Ahrefs of Semrush nodig).

**Wat zij hebben**
- Een kleine site van 13 URL's: homepage, `/prijzen`, support, juridische pagina's en een Engelse versie. Geen blog, geen pagina per sport, geen FAQ.
- Title "Rooster, deelnemers en betalingen | TrainYou", H1 "Jouw sport. Jouw rooster." (slogans zonder zoekwoord).
- Prijzenpagina laadt dynamisch ("Laden"): geen prijzen leesbaar voor zoekmachines.
- Product: mobiele apps (iOS en Android), demo zonder aanmelding, aanwezigheid afvinken, ouders die zelf afmelden, één plan zonder pakketten, 30 dagen geld-terug-garantie, termen per sport aanpasbaar, Engelse versie.
- Tennis en padel worden vandaag ondersteund; andere sporten staan als voorbeeld op de site.

**Onze inschatting:** zij worden waarschijnlijk niet gevonden via zoekwoorden uit hun content, maar via mond-tot-mond, de app stores en hun merknaam. Dat is een aanname.

**Waar wij beter zijn:** eigen pagina's per doelgroep met zoekwoordkoppen, leesbare prijzen en offers-markup, uitgebreide FAQ's, definitiezin en `llms.txt`.

**Waar zij beter zijn (product, niet SEO):** native apps, demo zonder aanmelding, aanwezigheidsregistratie, ouderportaal, Engelse versie.

**Let op:** `docs/competition-analysis.md` (februari 2026) stelt dat er geen directe concurrentie is. Dat moet bijgewerkt worden met TrainYou.

---

## 3. Blogartikelen die de SEO kunnen versterken

We zochten op een paar zoekvragen die een club of trainer kan intypen. De zoekresultaten zijn zwak: vooral sjablonen van Jotform en Paperform, Google Forms, PDF's van clubs en KNLTB-documenten, en Engelstalige toernooigidsen. Er staat weinig duidelijke Nederlandstalige uitleg. Dat betekent weinig concurrentie voor goede content. De zoekvolumes kennen we niet; die controleren we met Google Keyword Planner of Ahrefs voor we schrijven.

| # | Artikel (werktitel) | Zoekintentie | Waarom kansrijk |
|---|---|---|---|
| 1 | Inschrijfformulier tennislessen (en padel): voorbeeld en checklist | "inschrijfformulier tennislessen", "padellessen inschrijven formulier" | Resultaten zijn sjablonen en PDF's. Een gids met alle velden (niveau, voorkeurstijden, medeleerlingen, betaalwijze, betalingsverplichting) past precies. Natuurlijke koppeling met de formulierbouwer. |
| 2 | Tennisstage of zomerkamp organiseren: planning, prijs en inschrijvingen | "tennisstage organiseren", "zomerkamp tennisclub" | Resultaten zijn Engelstalig en gaan over toernooien. De kampfunctie bestaat al. |
| 3 | Wat kost lessenplanning-software voor tennisscholen? | "tennisclub software prijs", "lessenplanning software kosten" | Commerciële zoekintentie. Onze prijzen zijn definitief en leesbaar. |
| 4 | Excel of software voor je tennisschool: wanneer stap je over? | "excel lessenplanning", "alternatief voor excel tennisschool" | Spreekt lezers aan die het probleem al kennen. |
| 5 | Een padelschool starten: administratie en lesplanning | "padelschool starten", "padellessen organiseren" | Resultaten zijn vooral opleidingspagina's, geen praktische gids. |
| 6 | CoachOS vs. TrainYou vs. andere tools (eerlijke vergelijking) | "trainyou alternatief", "software tennisschool vergelijken" | Wie je concurrent kent, zoekt vergelijkingen. Noem ook wat zij beter doen. |
| 7 | Betalingen bij tennislessen: Bancontact, iDEAL en cash | "tennislessen betalen online", "betaling tennisschool" | Verbindt met Mollie en de betaling na bevestiging van de planning. |

**Voorgestelde volgorde:** 1, 2, 3, daarna 6. Eén artikel per twee weken bouwt meer op dan tien in één keer.

**Werkwijze per artikel (zoals bij de eerste twee)**
1. Een algemene gids met een vast vakje "Zo doe je dat met CoachOS" onder de relevante stappen (twee tot drie zinnen, enkel bevestigde functies).
2. Eerst voorleggen welke feiten we mogen noemen; geen claims die we niet kunnen aantonen.
3. Pas publiceren na nalezen in de PR.

Nog te bepalen later: een stap-voor-stap handleiding ("zo maak je een lessenreeks aan") hoort eerder in een supportsectie dan op de blog.

---

## 4. Tools voor continue verbetering

### Gratis en nu op te zetten

| Tool | Waarvoor |
|---|---|
| Google Search Console | Al gekoppeld. Maandelijks Performance (Queries en Pages) bekijken. |
| Bing Webmaster Tools | Verifiëren via DNS, sitemap indienen. Voedt ChatGPT-zoek en Copilot. In de code bestaat al `BING_SITE_VERIFICATION`. |
| Ahrefs Webmaster Tools | Gratis voor de eigen site: site-audit (kapotte links, titels, redirects) en backlinks. |
| Cookieloze analytics (Plausible, Umami of Fathom) | Bezoekers en bronnen meten zonder cookiebanner. Privacytekst op `/privacy` meteen bijwerken. |
| PageSpeed Insights en Lighthouse | Core Web Vitals. |
| Rich Results Test | Structured data controleren. |
| Google Keyword Planner en Google Trends | Zoekvolumes en trends. |

### Betaald, pas als we artikelen schrijven

| Tool | Waarvoor |
|---|---|
| Ahrefs of Semrush (ongeveer 30 tot 130 euro per maand; actuele prijs controleren) | Keyword-onderzoek, concurrentie, rankings, analyse van TrainYou en andere tools. Voor één site volstaat een maand abonnement om een keywordlijst te maken. |
| Screaming Frog (gratis tot 500 URL's) | Technische crawl. |

### Maandelijkse routine (ongeveer 30 minuten)

1. Search Console: nieuwe zoekopdrachten, winnende en verliezende pagina's.
2. Nieuwe of aangepaste pagina's: indexering aanvragen, sitemap opnieuw indienen.
3. Ahrefs Webmaster Tools: nieuwe fouten en nieuwe backlinks.
4. AI-zichtbaarheid: testen in ChatGPT en Perplexity of CoachOS genoemd wordt bij "lessenplanning tennisschool". Studio Swyft heeft hiervoor `geo-audit`-skills.

### Backlinks

Zoekmachines vertrouwen sites die door anderen genoemd worden. Realistische kansen: padelgids.nl (lijst padelscholen), partner- en ledenlijsten van Tennis & Padel Vlaanderen en de KNLTB, forums en groepen voor padeltrainers, en gastartikelen. Eén link van een clubsite of directory telt zwaarder dan tien artikelen.

---

## 5. Stand van zaken en open punten

### Pull requests

| PR | Inhoud | Status |
|---|---|---|
| #281 | SEO en bento-herontwerp van de marketingsite | gemerged |
| #282 | `noindex` voor de app | gemerged |
| #283 | Blog met twee artikelen | open (branch `feat/blog-artikelen`) |

Het tweede artikel (`inschrijven-zonder-account-gdpr`) wordt voorlopig niet gebruikt en moet nog beslist worden: publiceren, aanpassen of laten liggen. Het bevat juridische passages (bewaartermijn van 14 maanden, 7 jaar boekhouding, verwerkingsregister) die door een boekhouder of jurist bekeken moeten worden. Het eerste artikel is af en bevat CoachOS-vakjes.

### Open inhoudelijke punten

- **Meerprijs per extra leerling** (boven de limiet van het plan): bedrag nog te bepalen.
- **Btw-regels** (21% voor Belgische klanten, verlegd voor Nederlandse klanten met geldig btw-nummer): laten controleren door de boekhouder.
- **Verwerkersovereenkomst (DPA)**: bestaat er een standaarddocument voor klanten? Zo ja, dan kunnen we dat vermelden.
- **Automatische verwijdering van data**: CoachOS heeft geen bewaartermijn-functie voor zover we in de code vinden. De blog noemt 14 maanden enkel als richtlijn voor clubs.
- **Echte voorbeelden/quotes**: nog geen pilotquote, klantlogo of aantal. Eén echte quote met toestemming zou het "dagen minder werk" onderbouwen.
- **Schermafbeeldingen met alt-tekst**: de grote screenshot is weggehaald; een nieuwe kan bijdragen aan afbeeldingszoek en vertrouwen.
- **Padelspecifieke inhoud**: de padelpagina is grotendeels gelijkaardig aan de tennispagina omdat er geen bevestigde padelspecifieke functies zijn.
- **Publieke inschrijfpagina's (`/enroll`, `/camp`)** staan op `noindex`; later herbekijken met een eigen titel en beschrijving per kamp.
- **`marketing/logo/`** (T-shirtlogo's met de nieuwe slogan) staat nog niet in git.

### Na het mergen van een PR (Search Console)

1. Sitemaps: `sitemap.xml` opnieuw indienen.
2. URL-inspectie: "Request indexing" voor `/`, `/prijzen`, `/voor-tennisclubs`, `/voor-padelclubs`, `/voor-trainers` en voor `/blog` en de artikelen zodra die live staan (maximaal ongeveer 10 aanvragen per dag).
3. Controleer of de live title van de homepage de dubbele punt heeft ("CoachOS: lessenplanning voor tennis- en padelclubs").

---

## 6. Beslissingen voor de compagnon

1. Welke blogartikelen schrijven we, in welke volgorde, en hoe vaak publiceren we?
2. Welke analytics-tool kiezen we (Plausible, Umami of Fathom), en wie beheert dat?
3. Nemen we een betaald abonnement op Ahrefs of Semrush (eenmalig voor keyword-onderzoek of doorlopend)?
4. Willen we een eerlijke vergelijkingspagina met TrainYou (en andere tools)?
5. Wie benadert directories en partners voor backlinks (padelgids.nl, federaties)?
6. Wat doen we met het GDPR-artikel (publiceren, aanpassen na juridisch nalezen, of laten liggen)?
7. Willen we productfeatures waarin TrainYou voorloopt (demo zonder aanmelding, aanwezigheid, ouderportaal, apps) op de roadmap zetten?
