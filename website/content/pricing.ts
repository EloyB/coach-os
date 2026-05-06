/**
 * Site-wide visibility toggle for everything pricing-related:
 * - homepage Pricing section
 * - /prijzen page (returns 404 when hidden)
 * - "Prijzen" nav link
 * - /prijzen entry in sitemap.xml
 *
 * Hidden during the pilot stage while we finalise tarifering. Flip to
 * `true` to bring the full pricing surface back without code changes.
 *
 * Typed as `boolean` (not the literal `false`) so the dependent
 * conditional code paths don't get TS-narrowed away.
 */
export const PRICING_VISIBLE: boolean = false;

export interface PricingTier {
  id: string;
  name: string;
  tagline: string;
  /** Monthly price in EUR. `null` = custom / on quote. */
  priceMonthly: number | null;
  /** Shown next to the price, e.g. "/maand" or "op maat". */
  priceSuffix: string;
  /** Short helper line below the price. */
  priceHelper: string;
  /** Marks the visually highlighted tier on the card grid. */
  featured?: boolean;
  /** CTA button on the tier card. */
  cta: { label: string; href: string };
  /** Top features displayed on every card. Keep to 5-7 items. */
  features: string[];
}

export const PRICING_HEADING = "Eerlijke tarieven, voor elke clubmaat";
export const PRICING_SUB =
  "Geen verborgen kosten per leerling, geen jaarcontracten. Maandelijks opzegbaar — al is dat hopelijk niet nodig.";

export const PRICING_DISCLAIMER =
  "Voorlopige tarifering — definitieve prijzen volgen bij lancering.";

export const PRICING_TIERS: PricingTier[] = [
  {
    id: "starter",
    name: "Starter",
    tagline: "Voor zelfstandige coaches en kleine groepen.",
    priceMonthly: 19,
    priceSuffix: "/maand",
    priceHelper: "Excl. btw · maandelijks opzegbaar",
    cta: { label: "Begin met Starter", href: "#contact" },
    features: [
      "1 trainer",
      "Tot 50 actieve leerlingen",
      "1 club of locatie",
      "Anonieme inschrijvingen",
      "Magic-link bevestigingen",
      "E-mail ondersteuning",
    ],
  },
  {
    id: "club",
    name: "Club",
    tagline: "Voor tennis- en padelclubs met meerdere trainers.",
    priceMonthly: 49,
    priceSuffix: "/maand",
    priceHelper: "Excl. btw · maandelijks opzegbaar",
    featured: true,
    cta: { label: "Kies Club", href: "#contact" },
    features: [
      "Tot 10 trainers",
      "Onbeperkt aantal leerlingen",
      "1 club of locatie",
      "Formulierbouwer per lesreeks",
      "Planningsalgoritme",
      "Mollie betalingen (op de roadmap)",
      "Prioritaire ondersteuning",
    ],
  },
  {
    id: "federatie",
    name: "Federatie",
    tagline: "Voor multi-locatie clubs en overkoepelende organisaties.",
    priceMonthly: null,
    priceSuffix: "op maat",
    priceHelper: "Vanaf €149/maand · jaarcontract",
    cta: { label: "Vraag een offerte", href: "#contact" },
    features: [
      "Onbeperkte trainers",
      "Multi-club / multi-locatie",
      "Single sign-on (op aanvraag)",
      "Aangepaste rapportering",
      "Aangepaste integraties",
      "SLA + dedicated support",
    ],
  },
];

/**
 * Comparison matrix for the dedicated /prijzen page. Values:
 * - `true` / `false` render as ✓ / —
 * - a string renders as text (for limits like "Tot 50")
 */
export interface CompareGroup {
  label: string;
  rows: Array<{
    feature: string;
    /** Indexed by tier id from `PRICING_TIERS`. */
    values: Record<string, boolean | string>;
  }>;
}

export const PRICING_COMPARE: CompareGroup[] = [
  {
    label: "Limieten",
    rows: [
      {
        feature: "Trainers",
        values: { starter: "1", club: "Tot 10", federatie: "Onbeperkt" },
      },
      {
        feature: "Actieve leerlingen",
        values: { starter: "Tot 50", club: "Onbeperkt", federatie: "Onbeperkt" },
      },
      {
        feature: "Clubs / locaties",
        values: { starter: "1", club: "1", federatie: "Onbeperkt" },
      },
    ],
  },
  {
    label: "Lesplanning",
    rows: [
      {
        feature: "Lesreeksen aanmaken",
        values: { starter: true, club: true, federatie: true },
      },
      {
        feature: "Anonieme inschrijvingen",
        values: { starter: true, club: true, federatie: true },
      },
      {
        feature: "Magic-link bevestigingen",
        values: { starter: true, club: true, federatie: true },
      },
      {
        feature: "Formulierbouwer per lesreeks",
        values: { starter: false, club: true, federatie: true },
      },
      {
        feature: "Planningsalgoritme",
        values: { starter: false, club: true, federatie: true },
      },
    ],
  },
  {
    label: "Betalingen",
    rows: [
      {
        feature: "Cash registratie per inschrijving",
        values: { starter: true, club: true, federatie: true },
      },
      {
        feature: "Mollie (Bancontact + iDEAL)",
        values: { starter: false, club: "Op de roadmap", federatie: "Op de roadmap" },
      },
      {
        feature: "Aangepaste betaalafspraken",
        values: { starter: false, club: false, federatie: true },
      },
    ],
  },
  {
    label: "Beheer & integraties",
    rows: [
      {
        feature: "Multi-club beheer",
        values: { starter: false, club: false, federatie: true },
      },
      {
        feature: "Single sign-on",
        values: { starter: false, club: false, federatie: "Op aanvraag" },
      },
      {
        feature: "Aangepaste rapportering",
        values: { starter: false, club: false, federatie: true },
      },
      {
        feature: "Aangepaste integraties",
        values: { starter: false, club: false, federatie: true },
      },
    ],
  },
  {
    label: "Ondersteuning",
    rows: [
      {
        feature: "E-mail ondersteuning",
        values: { starter: true, club: true, federatie: true },
      },
      {
        feature: "Prioritaire ondersteuning",
        values: { starter: false, club: true, federatie: true },
      },
      {
        feature: "Dedicated contactpersoon",
        values: { starter: false, club: false, federatie: true },
      },
      {
        feature: "SLA",
        values: { starter: false, club: false, federatie: true },
      },
    ],
  },
];

export interface PricingFaqEntry {
  q: string;
  a: string;
}

export const PRICING_FAQ: PricingFaqEntry[] = [
  {
    q: "Zijn deze tarieven definitief?",
    a: "Nee. Dit zijn voorlopige tarieven die we hanteren tijdens de pre-launch om early adopters duidelijkheid te geven. Definitieve prijzen volgen bij lancering — vroege gebruikers behouden hun starttarief minstens 12 maanden.",
  },
  {
    q: "Zit btw inbegrepen?",
    a: "Nee. Alle bedragen zijn excl. btw. Voor Belgische klanten geldt 21%, voor Nederlandse klanten 21%. Btw-nummers van clubs en zelfstandigen worden op de factuur vermeld.",
  },
  {
    q: "Kan ik maandelijks opzeggen?",
    a: "Ja, voor Starter en Club. Je zegt op vóór de eerstvolgende factuurdatum en je toegang loopt tot het einde van de lopende maand. Federatie werkt met een jaarcontract vanwege de aangepaste implementatie.",
  },
  {
    q: "Bieden jullie korting voor sportfederaties of meerdere clubs?",
    a: "Ja. Bij meerdere clubs binnen één federatie of overkoepelende organisatie maken we een gecombineerde offerte. Neem contact op voor een berekening op basis van het aantal locaties en trainers.",
  },
  {
    q: "Welke betaalmethodes accepteren jullie zelf?",
    a: "SEPA-domiciliëring, Bancontact en iDEAL voor Belgische en Nederlandse klanten. Voor jaarcontracten kan ook bankoverschrijving op factuur.",
  },
  {
    q: "Wat als ik tijdens een seizoen meer trainers nodig heb?",
    a: "Je upgradet meteen naar het volgende abonnement — pro rata aangerekend voor de resterende dagen van de maand. Downgrades gaan in op de volgende factuurdatum.",
  },
  {
    q: "Bestaat er een gratis proefperiode?",
    a: "Ja. Tijdens de pre-launch krijgen alle clubs een verlengde proefperiode van 60 dagen, zonder betaalgegevens vooraf. Je beslist na een volledig seizoen of je doorgaat.",
  },
];

export const PRICING_FAQ_HEADING = "Vragen over tarifering";
