export type PersonaSlug =
  | "voor-tennisscholen"
  | "voor-padelscholen"
  | "voor-trainers";

export interface Persona {
  slug: PersonaSlug;
  /** Short label for breadcrumbs and internal nav. */
  navLabel: string;
  /** SEO meta title, keep under ~60 chars. */
  metaTitle: string;
  /** SEO meta description, ~155 chars, primary keyword up front. */
  metaDescription: string;
  kicker: string;
  h1: string;
  /**
   * Lead paragraph. Written like a Wikipedia opener, factual, definition-first,
   * quoteable by AI engines. The first sentence should stand alone as an answer
   * to "wat is CoachOS?"
   */
  lead: string;
  /** Pain points the persona faces in their current world. */
  pains: PersonaItem[];
  /** How CoachOS addresses each pain. Order matches `pains` so they read as pairs. */
  solutions: PersonaItem[];
  /** Persona-specific FAQ, feeds FAQPage JSON-LD on this page. */
  faq: { q: string; a: string }[];
}

export interface PersonaItem {
  title: string;
  body: string;
}

// ─────────────────────────────────────────────────────────────────────────
// TENNIS SCHOOLS
// ─────────────────────────────────────────────────────────────────────────

export const TENNIS_CLUBS_PERSONA: Persona = {
  slug: "voor-tennisscholen",
  navLabel: "Tennisscholen",
  metaTitle: "Lessenplanning voor tennisscholen · CoachOS",
  metaDescription:
    "Software voor lessenplanning bij tennisscholen in de Benelux. Lessenreeksen, anonieme inschrijvingen en automatische groepering, geen Excel meer.",
  kicker: "VOOR TENNISSCHOLEN",
  h1: "Lessenplanning voor tennisscholen",
  lead: "CoachOS is een lessenplanningsysteem voor tennisscholen. Beheer lessenreeksen, verzamel inschrijvingen via een publieke link en laat het algoritme leerlingen op niveau verdelen, zonder Excel-sheets, zonder accounts voor leerlingen, zonder oneindig veel mails.",
  pains: [
    {
      title: "Excel-sheets die elk seizoen opnieuw uit elkaar vallen",
      body: "Versies, formules, blocked celwaarden, bij elke nieuwe coach of seizoenswissel begint de chaos opnieuw.",
    },
    {
      title: "Eindeloos mailen over voorkeurstijden",
      body: "Ouders mailen tien keer hetzelfde, jij verwerkt het in een spreadsheet, en bij elke wijziging gaat het opnieuw rond.",
    },
    {
      title: "Met de hand puzzelen op niveau- en leeftijdsmatches",
      body: "Junior beginners niet bij gevorderden, A-spelers niet bij C-spelers, broers/zussen samenhouden, alles handmatig.",
    },
    {
      title: "Verschillende tools voor inschrijving, planning en betaling",
      body: "Google Forms voor de inschrijving, Excel voor de planning, een app voor betalingen, vier vinkjes om iemand te bevestigen.",
    },
  ],
  solutions: [
    {
      title: "Eén lessenreeks, hele seizoen geregeld",
      body: "Stel ritme, capaciteit en niveaus één keer in. Lessen worden automatisch gegenereerd over de hele periode.",
    },
    {
      title: "Anonieme inschrijflink, leerlingen vullen voorkeuren zelf in",
      body: "Een publieke link per lessenreeks. Naam, e-mail, voorkeuren, direct gekoppeld aan de juiste reeks.",
    },
    {
      title: "Planningsalgoritme verdeelt leerlingen automatisch",
      body: "Voorkeurstijden, niveaus en groepsverbanden worden meegenomen. Handmatige aanpassingen blijven bewaard bij herplannen.",
    },
    {
      title: "Mollie-betalingen + magic-link bevestigingen in dezelfde flow",
      body: "Inschrijving, planning, bevestiging en betaling lopen door één tool, geen export-import meer.",
    },
  ],
  faq: [
    {
      q: "Hoeveel banen kan ik beheren met CoachOS?",
      a: "Onbeperkt. Baan-naam is een vrij tekstveld op de lessenreeks, dus je kan zoveel banen toevoegen als je school heeft, gravel, hardcourt of indoor.",
    },
    {
      q: "Werkt dit voor jeugd én volwassenen tegelijk?",
      a: "Ja. Per lessenreeks stel je niveau, leeftijd en groepsgrootte in. Het algoritme houdt rekening met alle drie bij het verdelen van leerlingen over slots.",
    },
    {
      q: "Kan ik tussentijds een nieuwe trainer toevoegen?",
      a: "Ja. Een bestaande schoolbeheerder nodigt nieuwe trainers uit per e-mail. De trainer ziet enkel zijn eigen lessenreeksen, multi-locatie rollen en rechten zijn ingebouwd.",
    },
  ],
};

// ─────────────────────────────────────────────────────────────────────────
// PADEL SCHOOLS
// ─────────────────────────────────────────────────────────────────────────

export const PADEL_CLUBS_PERSONA: Persona = {
  slug: "voor-padelscholen",
  navLabel: "Padelscholen",
  metaTitle: "Lessenplanning voor padelscholen · CoachOS",
  metaDescription:
    "Software voor padelscholen, beheer lessenreeksen, verzamel inschrijvingen op de gsm en plan automatisch op niveau. Zonder accounts voor spelers.",
  kicker: "VOOR PADELSCHOLEN",
  h1: "Lessenplanning voor padelscholen",
  lead: "CoachOS is een lessenplanningsysteem voor padelscholen. Spelers schrijven zich in via een publieke link op hun gsm, het algoritme groepeert op niveau, en je beheert meerdere banen vanuit één dashboard, zonder app-download of account voor je leerlingen.",
  pains: [
    {
      title: "Snelle groei die handmatig niet bij te houden is",
      body: "Padel groeit elk seizoen. Wat vorig jaar nog op één Excel-sheet paste, is nu een chaos van WhatsApp-berichten en losse mails.",
    },
    {
      title: "Niveauverschillen tussen beginners en gevorderde spelers",
      body: "Een complete beginner aanmelden naast een ervaren speler levert frustratie aan beide kanten. Handmatig schiften is niet schaalbaar.",
    },
    {
      title: "Spelers die liefst op de gsm inschrijven",
      body: "Padel-community is mobile-first. Een formulier dat enkel op desktop werkt verliest spelers nog voor ze ingeschreven zijn.",
    },
  ],
  solutions: [
    {
      title: "Onbeperkt aantal inschrijvingen zonder admin-werk",
      body: "Geen limieten op leerlingen of inschrijvingen per lessenreeks. Het algoritme schaalt mee zodra de seizoen-aanmeldingen binnenstromen.",
    },
    {
      title: "Voorkeur- en niveaumatch in het planningsalgoritme",
      body: "Niveau, voorkeurstijd en groepsverbanden worden samen meegenomen. Beginners blijven bij beginners, gevorderden bij gevorderden.",
    },
    {
      title: "Mobiel-vriendelijke inschrijfflow zonder app",
      body: "De inschrijflink werkt vlot op elke gsm. Geen download, geen account, geen wachtwoord, alleen naam en e-mail.",
    },
  ],
  faq: [
    {
      q: "Werkt CoachOS goed op de gsm voor inschrijvingen?",
      a: "Ja. De inschrijfflow is mobile-first ontworpen: één scherm per stap, grote tap-targets, geen scroll-puzzels. Spelers schrijven zich in onder de minuut.",
    },
    {
      q: "Hoe gaat dit met snelle groei van mijn school?",
      a: "Geen limiet op leerlingen of inschrijvingen per lessenreeks. Het school-abonnement biedt extra ondersteuning voor multi-locatie scholen.",
    },
    {
      q: "Kunnen mijn spelers zonder app of account inschrijven?",
      a: "Ja. Geen download, geen account, alleen een publieke link. Bevestiging gebeurt via een magic-link in hun e-mail, geen wachtwoord nodig.",
    },
    {
      q: "Wat als een speler op meerdere niveaus wil les volgen?",
      a: "Per lessenreeks aparte inschrijving. Het planningsalgoritme houdt rekening met overlap zodat dezelfde speler niet op twee gelijktijdige slots ingedeeld wordt.",
    },
  ],
};

// ─────────────────────────────────────────────────────────────────────────
// TRAINERS
// ─────────────────────────────────────────────────────────────────────────

export const TRAINERS_PERSONA: Persona = {
  slug: "voor-trainers",
  navLabel: "Trainers",
  metaTitle: "Lessenplanning voor trainers · CoachOS",
  metaDescription:
    "Voor zelfstandige tennis- en padeltrainers, automatiseer inschrijvingen, planning en bevestigingen. Eén tool, geen Excel, gratis tijdens pilot.",
  kicker: "VOOR TRAINERS",
  h1: "Lessenplanning voor trainers",
  lead: "CoachOS is een lessenplanningsysteem gemaakt voor trainers en hoofdtrainers in tennis en padel. Stel een lessenreeks in, deel de inschrijflink, en laat het algoritme de planning maken terwijl jij op de baan staat in plaats van achter de laptop.",
  pains: [
    {
      title: "Elke maandag opnieuw lijstjes maken in Excel",
      body: "Een nieuwe seizoensplanning is altijd een avond achter de laptop met talloze Excel-sheets en gegevens.",
    },
    {
      title: "Eindeloos heen-en-weer mailen over voorkeurstijden",
      body: "Ouders willen overleggen, leerlingen veranderen van gedacht, en jij houdt de overzichten bij in je hoofd of op een briefje.",
    },
    {
      title: "Met de hand niveau-groepjes vormen",
      body: "Je weet wie bij wie hoort qua niveau, maar het is een puzzel om dat met voorkeurstijden EN beschikbaarheid te combineren.",
    },
    {
      title: "Geen tijd voor administratie naast lesgeven",
      body: "Lesgeven is je vak. Administratie is wat je doet ná de lessen, als er nog energie over is.",
    },
  ],
  solutions: [
    {
      title: "Eén lessenreeks kan volstaan voor het hele seizoen",
      body: "Maandagen, woensdagavonden of zaterdagochtenden, elk ritme. Lessen genereren automatisch.",
    },
    {
      title: "Inschrijfflow vervangt de oneindig veel mails volledig",
      body: "Je deelt één link. Leerlingen vullen alles in. Hun voorkeuren komen direct binnen, gekoppeld aan de juiste reeks.",
    },
    {
      title: "Algoritme groepeert op voorkeur en niveau",
      body: "Planning op niveau, voorkeurstijd en groepsverbanden. Aanpassingen achteraf blijven bewaard wanneer je opnieuw plant.",
    },
    {
      title: "Inschrijving → planning → bevestiging → betaling, één tool",
      body: "Geen export-import tussen vier verschillende apps. Eén dashboard, één login, één plek om de lessenstatus te zien.",
    },
  ],
  faq: [
    {
      q: "Werkt CoachOS ook als ik aan meerdere scholen lesgeef?",
      a: "Ja. Meerdere scholen via één account. Elke school is een aparte organisatie, je ziet alles vanuit één login en wisselt met één klik tussen rollen.",
    },
    {
      q: "Wat kost CoachOS voor zelfstandige trainers?",
      a: "Tijdens de pilotfase volledig gratis. Pilotgebruikers behouden bij lancering een lifetime korting op het reguliere tarief, definitieve prijzen volgen nog.",
    },
    {
      q: "Hoe lang duurt het om te starten?",
      a: "Een lessenreeks instellen duurt ongeveer tien minuten. Een hele seizoensplanning ronden duurt, afhankelijk van je groepsgrootte, meestal één middag.",
    },
    {
      q: "Heb ik administratieve hulp nodig om dit te gebruiken?",
      a: "Nee. CoachOS is gemaakt voor solo-werk. Geen IT-ondersteuning, geen aparte beheerder, geen accountantsexport om te leren. Het werkt out-of-the-box.",
    },
  ],
};

export const ALL_PERSONAS: Persona[] = [
  TENNIS_CLUBS_PERSONA,
  PADEL_CLUBS_PERSONA,
  TRAINERS_PERSONA,
];

export function getPersonaBySlug(slug: PersonaSlug): Persona | undefined {
  return ALL_PERSONAS.find((p) => p.slug === slug);
}
