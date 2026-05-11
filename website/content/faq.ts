export interface FaqEntry {
  q: string;
  a: string;
}

/**
 * Homepage FAQ. Order is thematic: definitional → onboarding → for members →
 * pricing/contract → money → data/privacy → customisation → access/comparison.
 * Each answer is self-contained (AI engines quote them out of context).
 */
export const FAQ: FaqEntry[] = [
  {
    q: "Wat is CoachOS?",
    a: "CoachOS is een lessenplanningsysteem voor tennis- en padelclubs. Trainers stellen een lessenreeks één keer in, leerlingen schrijven zich anoniem in via een publieke link, en een planningsalgoritme verdeelt iedereen automatisch over slots op niveau en voorkeur — zonder accounts, zonder Excel, zonder mailcarrousel.",
  },
  {
    q: "Voor welke sporten is CoachOS gemaakt?",
    a: "Vandaag specifiek voor tennis en padel. Niveau-indelingen, baanorganisatie en federatieconcepten zijn op die twee sporten afgestemd. Andere sporten liggen op de roadmap voor later.",
  },
  {
    q: "Hoe lang duurt het om te starten met CoachOS?",
    a: "Een lessenreeks instellen duurt ongeveer tien minuten. Een hele seizoensplanning ronden — inschrijvingen verzamelen, algoritme draaien, bevestigingen versturen — duurt voor de meeste clubs één middag. Geen voorafgaande training of consultancy nodig.",
  },
  {
    q: "Moeten mijn leerlingen een account aanmaken?",
    a: "Nee. Leerlingen schrijven zich in via een publieke link met alleen hun naam en e-mailadres. Bevestigingen en lesinformatie krijgen ze via een magic-link in hun mail — geen wachtwoord nodig.",
  },
  {
    q: "Werkt CoachOS op de gsm?",
    a: "Ja, volledig. De inschrijfflow voor leerlingen is mobile-first ontworpen — geen app-download, geen account, alles in de browser. De trainer-interface werkt ook op gsm voor op-de-baan beheer; voor seizoensplanning zelf werkt een laptop comfortabeler.",
  },
  {
    q: "Wat kost CoachOS?",
    a: "We finaliseren onze tarifering nog tijdens de pilotfase. Pilotgebruikers (max. 5 clubs of trainers) krijgen gratis toegang en behouden bij lancering een lifetime korting. Boek een demo of neem contact op om je plek te reserveren.",
  },
  {
    q: "In welke talen is CoachOS beschikbaar?",
    a: "We starten Nederlandstalig (Nederland + Vlaanderen). Frans volgt zodra de Vlaamse lancering rond is — Wallonië en Frankrijk zitten op de roadmap, gevolgd door bredere Europese expansie.",
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
    q: "Heb ik een verwerkersovereenkomst (DPA) nodig?",
    a: "Ja. Je club is verwerkingsverantwoordelijke, CoachOS treedt op als verwerker. Een standaard DPA hoort bij elk pilot- en klantcontract en regelt waar data staat, wie er toegang toe heeft, en hoe lang ze bewaard wordt.",
  },
  {
    q: "Wat doen jullie met de gegevens van leerlingen?",
    a: "Alleen wat strikt nodig is voor lessenplanning. Geen tracking, geen advertentie-doeleinden, geen verkoop aan derden. We werken AVG-conform met dataverwerkers binnen de EU.",
  },
  {
    q: "Welke integraties zijn beschikbaar?",
    a: "Vandaag: e-mailbevestiging via magic-link en Mollie-betalingen (Bancontact + iDEAL). Specifieke integraties — boekhouding, kassa-systemen, agenda-apps — bouwen we op aanvraag tijdens pilot.",
  },
  {
    q: "Kan ik mijn eigen formulier-vragen stellen per lessenreeks?",
    a: "Ja. Per lessenreeks bouw je een formulier op uit tekstvelden, meerkeuze-opties en ja/nee-vragen. De antwoorden zie je direct bij de inschrijving.",
  },
  {
    q: "Wie heeft toegang tot de data binnen mijn club?",
    a: "Je bepaalt zelf rollen. Admins zien alle lessenreeksen en leerlingen; trainers zien enkel hun eigen reeksen; leerlingen zien alleen hun eigen inschrijving via magic-link. Geen 'iedereen ziet alles'-modus.",
  },
  {
    q: "Wat is het verschil met een algemene clubadministratie-tool?",
    a: "Algemene clubadministratie-tools doen ledenbeheer en boekhouding voor de hele club. CoachOS focust specifiek op de lessenkant: lessenreeksen, inschrijvingen per reeks, niveau-indeling en seizoensplanning. We integreren met of zitten naast bestaande administratie-software, niet als vervanging.",
  },
];

export const FAQ_HEADING = "Veelgestelde vragen";
