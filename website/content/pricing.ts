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
export const PRICING_VISIBLE: boolean = true;

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

export const PRICING_HEADING = "Eerlijke tarieven, voor elke schoolgrootte";
export const PRICING_SUB =
  "Geen verborgen kosten per leerling. Betaal maandelijks, of kies jaarlijks en bespaar €5 per maand.";

/** Korting per maand (in EUR) bij jaarlijkse facturatie. */
export const ANNUAL_MONTHLY_DISCOUNT = 5;

export const PRICING_TIERS: PricingTier[] = [
  {
    id: "starter",
    name: "Starter",
    tagline: "Voor zelfstandige coaches en kleine groepen.",
    priceMonthly: 35,
    priceSuffix: "/maand",
    priceHelper: "Excl. btw · maandelijks opzegbaar",
    cta: { label: "Begin met Starter", href: "#contact" },
    features: [
      "1 trainer",
      "Tot 50 actieve leerlingen",
      "1 school of locatie",
      "Anonieme inschrijvingen",
      "Magic-link bevestigingen",
      "E-mail ondersteuning",
    ],
  },
  {
    id: "club",
    name: "School",
    tagline: "Voor tennis- en padelscholen met meerdere trainers.",
    priceMonthly: 70,
    priceSuffix: "/maand",
    priceHelper: "Excl. btw · maandelijks opzegbaar",
    featured: true,
    cta: { label: "Kies School", href: "#contact" },
    features: [
      "Tot 10 trainers",
      "Onbeperkt aantal leerlingen",
      "1 school of locatie",
      "Formulierbouwer per lessenreeks",
      "Planningsalgoritme",
      "Mollie betalingen (Bancontact + iDEAL)",
      "Prioritaire ondersteuning",
    ],
  },
  {
    id: "federatie",
    name: "Federatie",
    tagline: "Voor multi-locatie scholen en overkoepelende organisaties.",
    priceMonthly: 99,
    priceSuffix: "/maand",
    priceHelper: "Excl. btw · maandelijks opzegbaar",
    cta: { label: "Kies Federatie", href: "#contact" },
    features: [
      "Onbeperkte trainers",
      "Meerdere scholen / locaties",
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
        values: {
          starter: "Tot 50",
          club: "Onbeperkt",
          federatie: "Onbeperkt",
        },
      },
      {
        feature: "Scholen / locaties",
        values: { starter: "1", club: "1", federatie: "Onbeperkt" },
      },
    ],
  },
  {
    label: "Lessenplanning",
    rows: [
      {
        feature: "Lessenreeksen aanmaken",
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
        feature: "Formulierbouwer per lessenreeks",
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
        values: {
          starter: false,
          club: true,
          federatie: true,
        },
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
        feature: "Multi-locatie beheer",
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
    a: "Ja. Dit zijn onze vaste tarieven — geen verborgen kosten per leerling en geen verrassingen achteraf. Vroege gebruikers behouden hun starttarief minstens 12 maanden, ook als de prijzen later stijgen.",
  },
  {
    q: "Zit btw inbegrepen?",
    a: "Nee. Alle bedragen zijn excl. btw. Voor Belgische klanten geldt 21%, voor Nederlandse klanten 21%. Btw-nummers van scholen en zelfstandigen worden op de factuur vermeld.",
  },
  {
    q: "Kan ik maandelijks opzeggen?",
    a: "Ja. Op een maandabonnement zeg je op vóór de eerstvolgende factuurdatum en loopt je toegang tot het einde van de lopende maand. Kies je voor jaarlijkse facturatie, dan betaal je het jaar vooruit met €5 per maand korting.",
  },
  {
    q: "Bieden jullie korting voor meerdere scholen of federaties?",
    a: "Ja. Bij meerdere scholen binnen één federatie of overkoepelende organisatie maken we een gecombineerde offerte. Neem contact op voor een berekening op basis van het aantal locaties en trainers.",
  },
  {
    q: "Welke betaalmethodes accepteren jullie zelf?",
    a: "SEPA-domiciliëring, Bancontact en iDEAL voor Belgische en Nederlandse klanten. Betaling op factuur via bankoverschrijving kan op aanvraag.",
  },
  {
    q: "Wat als ik tijdens een seizoen meer trainers nodig heb?",
    a: "Je upgradet meteen naar het volgende abonnement — pro rata aangerekend voor de resterende dagen van de maand. Downgrades gaan in op de volgende factuurdatum.",
  },
  {
    q: "Bestaat er een gratis proefperiode?",
    a: "Ja. Tijdens de pre-launch krijgen alle scholen een verlengde proefperiode van 60 dagen, zonder betaalgegevens vooraf. Je beslist na een volledig seizoen of je doorgaat.",
  },
];

export const PRICING_FAQ_HEADING = "Vragen over tarifering";
