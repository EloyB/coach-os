/**
 * Pilot programma configuratie.
 *
 * Update `takenSeats` met de hand wanneer een nieuwe pilotgebruiker tekent, 
 * de teller op de homepage, de banner en de finale CTA volgen automatisch.
 */
export const PILOT = {
  totalSeats: 5,
  takenSeats: 4,
} as const;

/** Hoeveel plekken nog vrij zijn, afgeleid, niet handmatig in te stellen. */
export const PILOT_AVAILABLE_SEATS = PILOT.totalSeats - PILOT.takenSeats;

export const PILOT_BANNER = {
  prefix: "Pilot programma",
  /** Filled in at render-time using PILOT_AVAILABLE_SEATS. */
  body: (available: number) =>
    available === 1
      ? "nog 1 van 5 plekken vrij"
      : `nog ${available} van ${PILOT.totalSeats} plekken vrij`,
  benefit: "gratis tijdens pilot · lifetime korting",
  ctaLabel: "Reserveer plek",
  href: "#pilot",
} as const;

export const PILOT_KICKER = "PILOT PROGRAMMA";
export const PILOT_HEADING = "Word één van onze eerste vijf scholen";
export const PILOT_SUB =
  "We zoeken vijf scholen of zelfstandige trainers die mee bouwen aan CoachOS. Jouw feedback bepaalt waar we als eerste op inzetten, en jij krijgt er meer dan een plek voor terug.";

export interface PilotBenefit {
  title: string;
  body: string;
}

export const PILOT_BENEFITS: PilotBenefit[] = [
  {
    title: "Gratis tijdens de pilot",
    body: "Geen kosten zolang we in de pilotfase zitten. Niet voor de set-up, niet per seizoen, niet per leerling.",
  },
  {
    title: "Lifetime korting",
    body: "Wanneer de pricing publiek wordt, behoud je 25% korting op je tarief zolang je klant blijft.",
  },
  {
    title: "Directe lijn naar de founder",
    body: "Je hebt rechtstreeks contact met Eloy. Wensen en bugfixes gaan direct in de roadmap.",
  },
];

export const PILOT_CTA = {
  label: "Reserveer je pilotplek",
  href: "#contact",
} as const;
