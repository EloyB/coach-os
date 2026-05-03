export interface FaqEntry {
  q: string;
  a: string;
}

export const FAQ: FaqEntry[] = [
  {
    q: "Moeten mijn leerlingen een account aanmaken?",
    a: "Nee. Leerlingen schrijven zich in via een publieke link met alleen hun naam en e-mailadres. Bevestigingen en lesinformatie krijgen ze via een magic link in hun mail — geen wachtwoord nodig.",
  },
  {
    q: "Wat kost CoachOS?",
    a: "We finaliseren onze tarifering nog. Op de wachtlijst zetten of contact opnemen geeft je vroege toegang en prijs-info zodra die rond is.",
  },
  {
    q: "In welke talen is CoachOS beschikbaar?",
    a: "We starten Nederlandstalig (Nederland + Vlaanderen). Frans volgt zodra de Vlaamse lancering rond is — Wallonië en Frankrijk zitten op de roadmap.",
  },
  {
    q: "Kan ik meerdere clubs of locaties beheren vanuit één account?",
    a: "Ja. CoachOS is multi-tenant: je kan lid zijn van meerdere organisaties met verschillende rollen (admin in club A, trainer in club B) en wisselt met één klik.",
  },
  {
    q: "Hoe zit het met betalingen?",
    a: "Cash betalingen registreer je vandaag al manueel per inschrijving. Online betalen via Mollie (Bancontact + iDEAL) staat op de roadmap voor de eerste release.",
  },
  {
    q: "Wat doen jullie met de gegevens van leerlingen?",
    a: "Alleen wat strikt nodig is voor lesplanning. Geen tracking, geen advertentie-doeleinden, geen verkoop aan derden. We werken volgens de GDPR met dataverwerkers binnen de EU.",
  },
  {
    q: "Kan ik mijn eigen formulier-vragen stellen per lesreeks?",
    a: "Ja. Per lesreeks bouw je een formulier op uit tekstvelden, meerkeuze-opties en ja/nee-vragen. De antwoorden zie je direct bij de inschrijving.",
  },
];

export const FAQ_HEADING = "Veelgestelde vragen";
