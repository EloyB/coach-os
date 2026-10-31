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
    a: "CoachOS (ook geschreven als Coach OS) is een lessenplanningsysteem voor tennis- en padelclubs. Trainers stellen een lessenreeks in, leerlingen schrijven zich zonder account in via een link, en het planningsalgoritme verdeelt iedereen automatisch over slots op voorkeur en groepsverband. Zonder accounts, zonder Excel, zonder oneindig veel mails.",
  },
  {
    q: "Voor welke sporten is CoachOS gemaakt?",
    a: "Vandaag specifiek voor tennis en padel. Lessenreeksen, kampen en de manier van inschrijven zijn op die twee sporten afgestemd. Andere sporten liggen op de roadmap voor later.",
  },
  {
    q: "Hoe lang duurt het om te starten met CoachOS?",
    a: "Een lessenreeks instellen duurt ongeveer tien minuten. Een hele seizoensplanning maken (inschrijvingen verzamelen, algoritme draaien, bevestigingen versturen) kost een fractie van de tijd die je met Excel kwijt bent: clubs uit onze pilot houden er dagen werk aan over. Geen voorafgaande training of consultancy nodig.",
  },
  {
    q: "Moeten mijn leerlingen een account aanmaken?",
    a: "Nee. Leerlingen schrijven zich in via een link met hun naam, e-mailadres en de vragen die jij in het formulier hebt ingesteld. Bevestigingen en lesinformatie krijgen ze via een unieke link in hun mail. Geen wachtwoord nodig.",
  },
  {
    q: "Werkt CoachOS op de gsm?",
    a: "Ja, volledig. De inschrijfflow voor leerlingen is mobile-first ontworpen. Geen app-download, geen account, alles in de browser. De trainer-interface is volledig responsief en werkt op gsm, tablet en desktop.",
  },
  {
    q: "Wat kost CoachOS?",
    a: "Je betaalt per actieve leerling, met een onbeperkt aantal trainers: vanaf €25 per maand, of €250 per jaar met 2 maanden gratis. Nieuwe clubs proberen alles eerst 30 dagen gratis, zonder betaalgegevens. Zie de prijzenpagina voor alle details.",
  },
  {
    q: "In welke talen is CoachOS beschikbaar?",
    a: "We starten Nederlandstalig (Nederland + Vlaanderen). Frans volgt zodra de Vlaamse lancering rond is. Wallonië en Frankrijk zitten op de roadmap, gevolgd door bredere Europese expansie.",
  },
  {
    q: "Kan ik meerdere clubs of locaties beheren vanuit één account?",
    a: "Ja. CoachOS is multi-tenant: je kan lid zijn van meerdere organisaties met verschillende rollen (admin in club A, trainer in club B) en wisselt met één klik.",
  },
  {
    q: "Hoe zit het met betalingen?",
    a: "Zodra de planning bevestigd is, betalen leerlingen online via Mollie (Bancontact en iDEAL). Het geld komt rechtstreeks op de rekening van je school of club. Cash betalingen registreer je met één klik per inschrijving, zodat je overzicht klopt.",
  },
  {
    q: "Kan ik ook kampen en stages organiseren met CoachOS?",
    a: "Ja. Je maakt een kamp of stage aan met hele weken, enkele dagen of halve dagen, werkt met vaste pakketten en stelt een limiet in per dagdeel. Inschrijven gaat via dezelfde publieke link als bij lessenreeksen, zonder account, en betalen kan na de bevestiging van de planning.",
  },
  {
    q: "Wat doen jullie met de gegevens van leerlingen?",
    a: "Alleen wat strikt nodig is voor lessenplanning. Geen advertentie-doeleinden, geen verkoop aan derden. We werken GDPR-conform met dataverwerkers binnen de EU.",
  },
  {
    q: "Welke integraties zijn beschikbaar?",
    a: "Vandaag: e-mailbevestiging via unieke link en Mollie-betalingen (Bancontact + iDEAL). Specifieke integraties zoals boekhouding, kassa-systemen of agenda-apps kijken we graag mee. We staan altijd open voor suggesties en verbeteringen: mis je iets, laat het ons weten.",
  },
  {
    q: "Kan ik mijn eigen formulier-vragen stellen per lessenreeks?",
    a: "Ja. Per lessenreeks bouw je een formulier op uit tekstvelden, meerkeuze-opties en ja/nee-vragen. Je kan de antwoorden op je gekozen vragen bekijken per inschrijving.",
  },
  {
    q: "Wie heeft toegang tot de data binnen mijn club?",
    a: "Je bepaalt zelf rollen. Admins zien alle lessenreeksen en leerlingen; trainers zien enkel hun eigen reeksen; leerlingen zien alleen hun eigen inschrijving via unieke link. Geen 'iedereen ziet alles'-modus.",
  },
  {
    q: "Wat is het verschil met een algemene clubadministratie-tool?",
    a: "Algemene clubadministratie-tools doen ledenbeheer en boekhouding voor de hele club. CoachOS focust specifiek op de lessenkant: lessenreeksen, inschrijvingen per reeks en seizoensplanning. We integreren met of zitten naast bestaande administratie-software, niet als vervanging.",
  },
];

export const FAQ_HEADING = "Veelgestelde vragen";
