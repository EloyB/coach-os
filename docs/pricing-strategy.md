# Pricing Strategie — CoachOS

**Versie:** 1.0
**Datum:** 8 juni 2026
**Scope:** Prijszetting, concurrent-benchmarks en unit-economics voor de CoachOS SaaS.

> Aanvullend op [market-analysis.md](market-analysis.md) en
> [competition-analysis.md](competition-analysis.md). Die documenten dekken de
> marktomvang en het concurrentielandschap; dít document legt de **prijszetting**
> en de **kostenkant per klant** vast met geverifieerde cijfers (juni 2026).

---

## 1. Samenvatting

- **Prijsmodel:** 3 tiers per organisatie — **Solo €29**, **School €89**, **Club/Pro €169** per maand.
- **Geen gratis tier**, wel een **14-daagse gratis trial** (geen creditcard).
- **Jaarbetaling:** 2 maanden gratis (≈ 17% korting).
- **Mollie-transactiekosten** zijn doorrekenbaar en zitten níét in de abonnementsprijs.
- **Kost per klant is bijna nul** (~€0,20–0,50/mo marginaal); de hosting is een
  vaste kost die in **trappen** stijgt. Break-even ligt bij ~2 betalende klanten.

---

## 2. Prijstiers

| Plan | Prijs/mo | Voor wie | Inbegrepen |
|---|---|---|---|
| **Solo** | **€29** | zzp-coach, 1 trainer | Lesplanning, online inschrijving, tot ~40 leerlingen, Mollie-betalingen |
| **School** | **€89** | standaard tennis/padelschool | Meerdere trainers, onbeperkt leerlingen, facturatie, magic-link bevestiging |
| **Club / Pro** | **€169** | grote club, meerdere locaties | Multi-org switcher, prioriteit-support, alles uit School |

**Voorwaarden:**

- **Geen gratis tier.** In een kleine B2B-niche trekt een free tier vooral ruis
  aan. Een **14-daagse trial** bewijst de waarde zonder de prijs te ondermijnen.
- **Jaarabonnement:** 2 maanden gratis bij vooruitbetaling.
- **Geen prijs per transactie.** Mollie rekent al per betaling af (zie §4);
  houd de abonnementsprijs voorspelbaar en flat.
- **Publiceer de prijzen op de website.** Dit alleen al wint deals van Racket
  Class, die enkel op offerte werkt.

> **Afstemming met `market-analysis.md`:** dat document noemt een 4-tier model
> (Starter €29 / Professional €79 / Business €149 / Enterprise €299). Dit
> document verfijnt dat naar 3 tiers met afgeronde, hogere middentiers
> (€89 / €169) op basis van de geverifieerde concurrent-prijzen in §3.
> **Openstaande beslissing:** 3 tiers (dit doc) vs 4 tiers (market-analysis) —
> samen vast te leggen.

---

## 3. Concurrent-benchmarks (geverifieerd juni 2026)

CoachOS is **lesplanning + inschrijving + facturatie** voor tennis/padelscholen,
géén court-booking. Dat splitst de markt in directe en indirecte concurrenten.

### Directe concurrenten (zelfde propositie, Benelux)

| Speler | Prijs/maand | Opmerking |
|---|---|---|
| **Racket Class** (racketclass.com) | *Niet publiek (offerte)* | Exacte tweeling: inschrijven, indelen, communiceren, factureren, betalen voor tennis/padelscholen. NL. **Kans: wij publiceren prijzen, zij niet.** |
| **Tennisschoolapp** (NL) | **€25** (zzp) · **€100** (zonder facturatie) · **€150** (mét facturatie) | Het scherpste transparante prijsanker in de markt. |
| **Elit 2.0** (Tennis & Padel Vlaanderen) | Gratis (federatie) | Club-/ledenbeheer, complex, niet trainer-gericht. Zie [competition-analysis.md](competition-analysis.md). |

### Indirecte concurrenten (court-booking + clubbeheer, breder/duurder)

| Speler | Prijs/maand |
|---|---|
| Playtomic Manager | ~€200–500 (clubbeheer, marktleider EU) |
| MATCHi | ~€100–300 (court-booking, beperkte Benelux-presence) |
| Anolla | usage-based, gratis tier + betaling per geboekt uur |
| Planubo | vanaf ~$17, free-forever (coach-tool, internationaal, geen Benelux-presence) |

### Wat de benchmarks vertellen

1. **Markt-band "school met facturatie" ≈ €100–150/mo** (Tennisschoolapp expliciet).
   Onze School-tier (€89) komt daar bewust ónder binnen.
2. **Solo/zzp-anker ≈ €25/mo.** Onze Solo (€29) zit er net boven, gerechtvaardigd
   door betere planning + native Mollie.
3. **Racket Class verbergt prijzen → traag offerteproces.** Transparantie is onze
   grootste hefboom.
4. **Native Mollie (Bancontact + iDEAL, immediate payments)** is een echte
   differentiator t.o.v. internationale tools.

**Bronnen:** Tennisschoolapp (tennisschoolapp.nl), Racket Class (racketclass.com),
Planubo (capterra.com), Playtomic Manager (playtomic.com/pricing),
Anolla (anolla.com). Geverifieerd juni 2026.

---

## 4. Wat kost een klant ons?

Twee soorten kost — het onderscheid bepaalt de schaalstrategie.

### 4.1 Marginale kost (echt variabel, per extra klant)

| Component | Per school/seizoen | Detail |
|---|---|---|
| E-mail (Scaleway TEM) | ~€0,18 | ~700 mails × €0,00025; eerste 300/mo gratis |
| DB-storage | ~€0 | enkele MB op 10 GB |
| Mollie | €0,29–0,39 / transactie | **doorrekenbaar** — geen netto kost |

→ **Netto marginale kost: ~€0,20–0,50 per klant per maand. Praktisch gratis.**

### 4.2 Vaste infra-kost (zie ook `infrastructure/README.md`)

| Onderdeel | €/mo |
|---|---|
| VPS (PRO2-XXS, 2 vCPU / 8 GB) | ~€41 |
| Reserved IP | ~€1 |
| Managed Postgres (DB-DEV-S, 10 GB) | ~€12 |
| Container Registry + TEM + Secret Manager + Object Storage | ~€0–3 |
| Domein (coach-os.be) | ~€0,60 |
| **Totaal** | **~€55/mo** |

### 4.3 Geamortiseerde kost per klant

| Klanten | Infra/mo | Kost/klant | Bruto marge bij €89 School |
|---|---|---|---|
| 1 | €55 | €55 | break-even |
| 5 | €55 | €11 | ~88% |
| 10 | ~€80¹ | €8 | ~91% |
| 25 | ~€80 | €3,2 | ~96% |
| 50 | ~€120² | €2,4 | ~97% |

¹ inclusief DB-HA · ² grotere DB + evt. tweede VPS

---

## 5. Schaaltrappen

De kost is **niet lineair**: je betaalt bijna niets extra per klant tot een
resource-plafond, dan spring je een trap omhoog. Richtgetallen (nog niet
load-getest):

| Trap | Setup | €/mo | Capaciteit (schatting) | Trigger |
|---|---|---|---|---|
| **Nu** | PRO2-XXS 8 GB + DB-DEV-S (single-node) | ~€55 | ~20–40 scholen | — |
| **+HA** | idem + `is_ha_cluster = true` | ~€80 | ~50–80 scholen | **vóór 1e betalende klant** (data!) |
| **+DB-GP** | grotere DB + evt. tweede VPS | ~€120–150 | 100–200+ scholen | DB-connections/CPU lopen vol |

**Bottleneck-volgorde:** de DB knelt eerder dan de VPS (8 GB heeft ruim
headroom). De eerste echte uitgave is dus **DB-HA**, en die is sowieso verplicht
vóór betalende klanten — verlies van de single-node DB is een ramp.

---

## 6. Strategische conclusie

> **Kosten zijn niet de schaalrem.** Marginale kost ~€0, vaste kost stijgt in
> kleine trappen (€25–50) die telkens tientallen klanten ontgrendelen. Bruto
> marge verbetert met schaal en zit al bij 10 klanten boven 90%. De uitdaging
> zit in acquisitie, niet in kosten.

**Niet-onderhandelbaar:** DB-HA (`is_ha_cluster = true`) aanzetten vóór de eerste
betalende klant.

---

## 7. Open beslissingen

1. **3 tiers (€29/€89/€169) vs 4 tiers** (incl. Enterprise €299 uit market-analysis).
2. Exacte leerlingen-cap op Solo (voorstel: ~40).
3. Lifetime early-adopter korting voor pilotscholen (zie GTM in market-analysis §5.2).

---

**Laatst bijgewerkt:** 8 juni 2026 · **Volgende review:** bij eerste 10 betalende klanten
