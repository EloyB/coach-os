import { SITE } from "@/content/meta";

/**
 * Site-wide visibility toggle voor alles rond tarieven (homepage-sectie,
 * /prijzen-pagina, nav-link, sitemap). Staat live sinds de prijzen vastliggen.
 */
export const PRICING_VISIBLE: boolean = true;

/** "Start gratis" leidt naar de registratie/proef in de app. */
export const REGISTER_URL = `${SITE.appUrl}/register`;

export type Billing = "monthly" | "yearly";

export interface PricingTier {
  id: string;
  name: string;
  tagline: string;
  /** €/maand. `null` = op maat / op aanvraag. */
  priceMonthly: number | null;
  /** €/jaar (2 maanden gratis t.o.v. maandprijs). `null` = op maat. */
  priceYearly: number | null;
  /** Korte limiet-regel onder de prijs, bv. "tot 60 leerlingen". */
  studentLimit: string;
  /** Visueel uitgelichte kaart. */
  featured?: boolean;
  /** CTA-knop op de kaart. */
  cta: { label: string; href: string };
  /** 5–6 kernpunten per kaart. */
  features: string[];
}

export const PRICING_HEADING = "Eerlijke tarieven, voor elke clubmaat";
export const PRICING_SUB =
  "Je betaalt per actieve leerling, met een onbeperkt aantal trainers. Geen jaarcontract: maandelijks opzegbaar, of betaal per jaar en krijg 2 maanden gratis.";

/** Geruststelling onder de hero en op de kaarten. */
export const PRICING_TRIAL = "30 dagen gratis proberen, zonder betaalgegevens.";
/** Early-bird banner. */
export const PRICING_EARLYBIRD =
  "Early-bird: de eerste clubs krijgen een levenslange korting. Zolang de plaatsen duren.";
export const PRICING_VAT_NOTE = "Alle prijzen zijn excl. btw.";

export const PRICING_TIERS: PricingTier[] = [
  {
    id: "starter",
    name: "Game",
    tagline: "Voor zelfstandige coaches en kleine clubs.",
    priceMonthly: 25,
    priceYearly: 250,
    studentLimit: "tot 60 actieve leerlingen",
    cta: { label: "Start gratis", href: REGISTER_URL },
    features: [
      "Onbeperkt aantal trainers",
      "Alle functies inbegrepen",
      "Planningsalgoritme",
      "Formulierbouwer per lessenreeks",
      "Cash- en online betalingen",
      "E-mailondersteuning",
    ],
  },
  {
    id: "club",
    name: "Set",
    tagline: "Voor tennis- en padelclubs met meerdere trainers.",
    priceMonthly: 49,
    priceYearly: 490,
    studentLimit: "tot 200 actieve leerlingen",
    featured: true,
    cta: { label: "Start gratis", href: REGISTER_URL },
    features: [
      "Alles uit Game",
      "Onbeperkt aantal trainers",
      "Tot 200 actieve leerlingen",
      "Kampen en lessenreeksen",
      "Prioritaire ondersteuning",
    ],
  },
  {
    id: "groot",
    name: "Match",
    tagline: "Voor grote clubs met een druk lesseizoen.",
    priceMonthly: 89,
    priceYearly: 890,
    studentLimit: "tot 500 actieve leerlingen",
    cta: { label: "Start gratis", href: REGISTER_URL },
    features: [
      "Alles uit Set",
      "Tot 500 actieve leerlingen",
      "Vaste contactpersoon",
      "Hulp bij de opstart",
    ],
  },
  {
    id: "opmaat",
    name: "Slam",
    tagline: "Voor federaties en clubs met meerdere locaties.",
    priceMonthly: null,
    priceYearly: null,
    studentLimit: "500+ leerlingen of meerdere clubs",
    cta: { label: "Vraag een offerte", href: "#contact" },
    features: [
      "Alles uit Match",
      "Multi-club beheer",
      "Aangepaste rapportering",
      "SLA en dedicated support",
    ],
  },
];

/** "Hoe werkt de prijs?": uitlegblokken op de /prijzen-pagina. */
export const PRICING_EXPLAINER_HEADING = "Hoe werkt de prijs?";
export const PRICING_EXPLAINER: Array<{ title: string; body: string }> = [
  {
    title: "Per actieve leerling, niet per trainer",
    body: "Je plan hangt af van het aantal unieke leerlingen met een bevestigde inschrijving binnen het jaar. Trainers en jobstudenten voeg je onbeperkt toe, zonder meerkost.",
  },
  {
    title: "Ruime marge, zachte overschrijding",
    body: "Boven je limiet zit nog 10% speling. Daarboven reken je een kleine meerprijs per extra leerling af, één keer per jaar. Zit je structureel hoger, dan stellen we gewoon een upgrade voor.",
  },
  {
    title: "Nooit geblokkeerd",
    body: "Inschrijvingen blijven altijd werken, ook als je boven je limiet zit. We blokkeren nooit midden in een seizoen, want je leerlingen mogen daar niet de dupe van zijn.",
  },
];

/**
 * Vergelijkingsmatrix voor de /prijzen-pagina. Waarden:
 * - `true` / `false` → ✓ / minus-icoon
 * - string → tekst (bv. limieten)
 */
export interface CompareGroup {
  label: string;
  rows: Array<{
    feature: string;
    /** Geïndexeerd op tier-id uit `PRICING_TIERS`. */
    values: Record<string, boolean | string>;
  }>;
}

export const PRICING_COMPARE: CompareGroup[] = [
  {
    label: "Limieten",
    rows: [
      {
        feature: "Actieve leerlingen",
        values: {
          starter: "Tot 60",
          club: "Tot 200",
          groot: "Tot 500",
          opmaat: "500+",
        },
      },
      {
        feature: "Trainers",
        values: {
          starter: "Onbeperkt",
          club: "Onbeperkt",
          groot: "Onbeperkt",
          opmaat: "Onbeperkt",
        },
      },
      {
        feature: "Clubs / locaties",
        values: { starter: "1", club: "1", groot: "1", opmaat: "Meerdere" },
      },
    ],
  },
  {
    label: "Lessenplanning",
    rows: [
      {
        feature: "Lessenreeksen en kampen",
        values: { starter: true, club: true, groot: true, opmaat: true },
      },
      {
        feature: "Planningsalgoritme",
        values: { starter: true, club: true, groot: true, opmaat: true },
      },
      {
        feature: "Formulierbouwer per lessenreeks",
        values: { starter: true, club: true, groot: true, opmaat: true },
      },
      {
        feature: "Magic-link bevestigingen",
        values: { starter: true, club: true, groot: true, opmaat: true },
      },
    ],
  },
  {
    label: "Betalingen",
    rows: [
      {
        feature: "Cash registratie per inschrijving",
        values: { starter: true, club: true, groot: true, opmaat: true },
      },
      {
        feature: "Online betalingen (Mollie)",
        values: { starter: true, club: true, groot: true, opmaat: true },
      },
    ],
  },
  {
    label: "Beheer & ondersteuning",
    rows: [
      {
        feature: "E-mailondersteuning",
        values: { starter: true, club: true, groot: true, opmaat: true },
      },
      {
        feature: "Prioritaire ondersteuning",
        values: { starter: false, club: true, groot: true, opmaat: true },
      },
      {
        feature: "Vaste contactpersoon",
        values: { starter: false, club: false, groot: true, opmaat: true },
      },
      {
        feature: "Multi-club beheer",
        values: { starter: false, club: false, groot: false, opmaat: true },
      },
      {
        feature: "Aangepaste rapportering",
        values: { starter: false, club: false, groot: false, opmaat: true },
      },
      {
        feature: "SLA",
        values: { starter: false, club: false, groot: false, opmaat: true },
      },
    ],
  },
];

export interface PricingFaqEntry {
  q: string;
  a: string;
}

export const PRICING_FAQ_HEADING = "Vragen over tarifering";
export const PRICING_FAQ: PricingFaqEntry[] = [
  {
    q: "Wat telt als een actieve leerling?",
    a: "Een unieke persoon met minstens één bevestigde inschrijving in een lessenreeks of kamp binnen het facturatiejaar. Wie annuleert vóór de start telt niet mee, en dezelfde leerling in meerdere reeksen telt maar één keer. Broers en zussen onder hetzelfde ouder-e-mailadres tellen apart.",
  },
  {
    q: "Betaal ik per trainer?",
    a: "Nee. Het aantal trainers is altijd onbeperkt. Je betaalt enkel op basis van het aantal actieve leerlingen. Zo kan je gerust met part-time trainers en jobstudenten werken zonder accounts te delen.",
  },
  {
    q: "Maandelijks of jaarlijks?",
    a: "Allebei kan. Maandelijks is volledig opzegbaar. Betaal je per jaar, dan krijg je 2 maanden gratis (bv. €250 i.p.v. €300 voor Game). Maandbetaling verloopt via automatische afschrijving; jaarbetaling kan ook op factuur.",
  },
  {
    q: "Is er een gratis proefperiode?",
    a: "Ja. Je probeert CoachOS 30 dagen volledig gratis, met alle functies en zonder betaalgegevens vooraf. Je beslist pas daarna of je doorgaat.",
  },
  {
    q: "Wat als ik boven mijn limiet ga?",
    a: "Geen paniek: inschrijvingen worden nooit geblokkeerd. Boven je limiet zit nog 10% marge. Daarboven reken je een kleine meerprijs per extra leerling af bij je verlenging. Zit je er structureel boven, dan stellen we een upgrade voor. Dat is meestal voordeliger.",
  },
  {
    q: "Zit btw inbegrepen?",
    a: "Nee, alle bedragen zijn excl. btw. Voor Belgische klanten geldt 21%; je btw-nummer komt op de factuur. Voor Nederlandse klanten met een geldig btw-nummer wordt de btw verlegd.",
  },
  {
    q: "Kan ik opzeggen of veranderen van plan?",
    a: "Upgraden kan altijd, pro rata aangerekend. Een maandabonnement zeg je op tegen de volgende factuurdatum; downgraden gaat in bij je volgende verlenging. Geen jaarcontract, geen verborgen kosten.",
  },
];
