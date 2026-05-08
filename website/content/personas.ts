export type PersonaSlug =
  | "voor-tennisclubs"
  | "voor-padelclubs"
  | "voor-trainers";

export interface Persona {
  slug: PersonaSlug;
  /** Short label for breadcrumbs and internal nav. */
  navLabel: string;
  /** SEO meta title — keep under ~60 chars. */
  metaTitle: string;
  /** SEO meta description — ~155 chars, primary keyword up front. */
  metaDescription: string;
  kicker: string;
  h1: string;
  /**
   * Lead paragraph. Written like a Wikipedia opener — factual, definition-first,
   * quoteable by AI engines. The first sentence should stand alone as an answer
   * to "wat is CoachOS?"
   */
  lead: string;
  /** Pain points the persona faces in their current world. */
  pains: PersonaItem[];
  /** How CoachOS addresses each pain. Order matches `pains` so they read as pairs. */
  solutions: PersonaItem[];
  /** Persona-specific FAQ — feeds FAQPage JSON-LD on this page. */
  faq: { q: string; a: string }[];
}

export interface PersonaItem {
  title: string;
  body: string;
}

// ─────────────────────────────────────────────────────────────────────────
// TENNIS CLUBS
// ─────────────────────────────────────────────────────────────────────────

export const TENNIS_CLUBS_PERSONA: Persona = {
  slug: "voor-tennisclubs",
  navLabel: "Tennisclubs",
  metaTitle: "Lesplanning voor tennisclubs · CoachOS",
  metaDescription:
    "Software voor lesplanning bij tennisclubs in de Benelux. Lesreeksen, anonieme inschrijvingen en automatische groepering — geen Excel meer.",
  kicker: "VOOR TENNISCLUBS",
  h1: "Lesplanning voor tennisclubs",
  lead: "CoachOS is een lesplanningsysteem voor tennisclubs. Beheer lesreeksen, verzamel inschrijvingen via een publieke link en laat het algoritme leerlingen op niveau verdelen — zonder Excel-sheets, zonder accounts voor leden, zonder mailcarrousel.",
  pains: [
    {
      title: "Excel-sheets die elk seizoen opnieuw uit elkaar vallen",
      body: "Versies, formules, blocked celwaarden — bij elke nieuwe coach of seizoenswissel begint de chaos opnieuw.",
    },
    {
      title: "Eindeloos mailen over voorkeurstijden",
      body: "Ouders mailen tien keer hetzelfde, jij verwerkt het in een spreadsheet, en bij elke wijziging gaat het opnieuw rond.",
    },
    {
      title: "Met de hand puzzelen op niveau- en leeftijdsmatches",
      body: "Junior beginners niet bij gevorderden, A-spelers niet bij C-spelers, broers/zussen samenhouden — alles handmatig.",
    },
    {
      title: "Verschillende tools voor inschrijving, planning en betaling",
      body: "Google Forms voor de inschrijving, Excel voor de planning, een app voor betalingen — vier vinkjes om iemand te bevestigen.",
    },
  ],
  solutions: [
    {
      title: "Eén lesreeks, hele seizoen geregeld",
      body: "Stel ritme, capaciteit en niveaus één keer in. Lessen worden automatisch gegenereerd over de hele periode.",
    },
    {
      title: "Anonieme inschrijflink — leerlingen vullen voorkeuren zelf in",
      body: "Een publieke link per lesreeks. Naam, e-mail, voorkeuren — direct gekoppeld aan de juiste reeks.",
    },
    {
      title: "Planningsalgoritme verdeelt leerlingen automatisch",
      body: "Voorkeurstijden, niveaus en groepsverbanden worden meegenomen. Handmatige aanpassingen blijven bewaard bij herplannen.",
    },
    {
      title: "Mollie-betalingen + magic-link bevestigingen in dezelfde flow",
      body: "Inschrijving, planning, bevestiging en betaling lopen door één tool — geen export-import meer.",
    },
  ],
  faq: [
    {
      q: "Hoeveel banen kan ik beheren met CoachOS?",
      a: "Onbeperkt. Baan-naam is een vrij tekstveld op de lesreeks, dus je kan zoveel banen toevoegen als je club heeft — gravel, hardcourt of indoor.",
    },
    {
      q: "Werkt dit voor jeugd én volwassenen tegelijk?",
      a: "Ja. Per lesreeks stel je niveau, leeftijd en groepsgrootte in. Het algoritme houdt rekening met alle drie bij het verdelen van leerlingen over slots.",
    },
    {
      q: "Hoe zit het met federatie-rapportering (KNLTB, Tennis Vlaanderen)?",
      a: "Federatie-rapportering naar KNLTB en Tennis Vlaanderen staat op de roadmap voor de eerste release. Tijdens de pilot helpen we periodiek handmatig bij de rapportering uit je leerling- en lesreeksoverzicht.",
    },
    {
      q: "Kan ik tussentijds een nieuwe trainer toevoegen?",
      a: "Ja. Een bestaande clubadmin nodigt nieuwe trainers uit per e-mail. De trainer ziet enkel zijn eigen lesreeksen — multi-club rollen en rechten zijn ingebouwd.",
    },
  ],
};

// ─────────────────────────────────────────────────────────────────────────
// PADEL CLUBS
// ─────────────────────────────────────────────────────────────────────────

export const PADEL_CLUBS_PERSONA: Persona = {
  slug: "voor-padelclubs",
  navLabel: "Padelclubs",
  metaTitle: "Lesplanning voor padelclubs · CoachOS",
  metaDescription:
    "Software voor padelclubs — beheer lesreeksen, verzamel inschrijvingen op de gsm en plan automatisch op niveau. Zonder accounts voor spelers.",
  kicker: "VOOR PADELCLUBS",
  h1: "Lesplanning voor padelclubs",
  lead: "CoachOS is een lesplanningsysteem voor padelclubs. Spelers schrijven zich in via een publieke link op hun gsm, het algoritme groepeert op niveau, en je beheert meerdere banen vanuit één dashboard — zonder app-download of account voor je leden.",
  pains: [
    {
      title: "Snelle groei die handmatig niet bij te houden is",
      body: "Padel groeit elk seizoen. Wat vorig jaar nog op één Excel-sheet paste, is nu een chaos van WhatsApp-berichten en losse mails.",
    },
    {
      title: "Niveauverschillen tussen beginners en gevorderde spelers",
      body: "Een complete beginner aanmelden naast een ervaren A-speler levert frustratie aan beide kanten — maar handmatig schiften is niet schaalbaar.",
    },
    {
      title: "Banen die zowel vrij geboekt als voor lesreeksen gebruikt worden",
      body: "Wanneer is welke baan vrij? Wie heeft les en wie speelt vrij? Geen overzicht zonder vier tabs open te hebben.",
    },
    {
      title: "Spelers die liefst op de gsm inschrijven",
      body: "Padel-community is mobile-first. Een formulier dat enkel op desktop werkt verliest spelers nog voor ze ingeschreven zijn.",
    },
  ],
  solutions: [
    {
      title: "Onbeperkt aantal inschrijvingen zonder admin-werk",
      body: "Geen limieten op leerlingen of inschrijvingen per lesreeks. Het algoritme schaalt mee zodra de seizoen-aanmeldingen binnenstromen.",
    },
    {
      title: "Voorkeur- en niveaumatch in het planningsalgoritme",
      body: "Niveau, voorkeurstijd en groepsverbanden worden samen meegenomen. Beginners blijven bij beginners, gevorderden bij gevorderden.",
    },
    {
      title: "Centraal overzicht per baan en per slot",
      body: "Eén dashboard toont welke baan op welk moment voor lesreeks gebruikt wordt — geen conflicten meer met vrij geboekte tijden.",
    },
    {
      title: "Mobiel-vriendelijke inschrijfflow zonder app",
      body: "De publieke inschrijflink werkt vlot op elke gsm. Geen download, geen account, geen wachtwoord — alleen naam en e-mail.",
    },
  ],
  faq: [
    {
      q: "Werkt CoachOS goed op de gsm voor inschrijvingen?",
      a: "Ja. De inschrijfflow is mobile-first ontworpen: één scherm per stap, grote tap-targets, geen scroll-puzzels. Spelers schrijven zich in onder de minuut.",
    },
    {
      q: "Hoe gaat dit met snelle groei van mijn club?",
      a: "Geen limiet op leerlingen of inschrijvingen per lesreeks. Het Federatie-abonnement biedt extra ondersteuning voor multi-locatie clubs.",
    },
    {
      q: "Kunnen mijn spelers zonder app of account inschrijven?",
      a: "Ja. Geen download, geen account, alleen een publieke link. Bevestiging gebeurt via een magic-link in hun e-mail — geen wachtwoord nodig.",
    },
    {
      q: "Wat als een speler op meerdere niveaus wil les volgen?",
      a: "Per lesreeks aparte inschrijving. Het planningsalgoritme houdt rekening met overlap zodat dezelfde speler niet op twee gelijktijdige slots ingedeeld wordt.",
    },
  ],
};

// ─────────────────────────────────────────────────────────────────────────
// TRAINERS
// ─────────────────────────────────────────────────────────────────────────

export const TRAINERS_PERSONA: Persona = {
  slug: "voor-trainers",
  navLabel: "Trainers",
  metaTitle: "Lesplanning voor trainers · CoachOS",
  metaDescription:
    "Voor zelfstandige tennis- en padeltrainers — automatiseer inschrijvingen, planning en bevestigingen. Eén tool, geen Excel, gratis tijdens pilot.",
  kicker: "VOOR TRAINERS",
  h1: "Lesplanning voor trainers",
  lead: "CoachOS is een lesplanningsysteem gemaakt voor trainers en hoofdtrainers in tennis en padel. Stel een lesreeks één keer in, deel een publieke inschrijflink, en laat het algoritme de planning maken — terwijl jij op de baan staat in plaats van achter de laptop.",
  pains: [
    {
      title: "Elke maandag opnieuw lijstjes maken in Excel",
      body: "Een nieuwe seizoensplanning is altijd een avond achter de laptop met copy-paste en formules die ergens stiekem breken.",
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
      body: "Lesgeven is je vak. Administratie is wat je doet ná de lessen — als er nog energie over is.",
    },
  ],
  solutions: [
    {
      title: "Eén lesreeks volstaat voor het hele seizoen",
      body: "Maandagen, woensdagavonden of zaterdagochtenden — elk ritme. Lessen genereren automatisch.",
    },
    {
      title: "Inschrijfflow vervangt mailcarrousel volledig",
      body: "Je deelt één link. Leerlingen vullen alles in. Hun voorkeuren komen direct binnen, gekoppeld aan de juiste reeks.",
    },
    {
      title: "Algoritme groepeert op voorkeur en niveau",
      body: "Niveau, voorkeurstijd, groepsverbanden — alles in één run. Aanpassingen achteraf blijven bewaard wanneer je opnieuw plant.",
    },
    {
      title: "Inschrijving → planning → bevestiging → betaling, één tool",
      body: "Geen export-import tussen vier verschillende apps. Eén dashboard, één login, één plek om de seizoenstatus te zien.",
    },
  ],
  faq: [
    {
      q: "Werkt CoachOS ook als ik aan meerdere clubs lesgeef?",
      a: "Ja. Multi-club via één account. Elke club is een aparte organisatie, je ziet alles vanuit één login en wisselt met één klik tussen rollen.",
    },
    {
      q: "Wat kost CoachOS voor zelfstandige trainers?",
      a: "Tijdens de pilotfase volledig gratis. Pilotgebruikers behouden bij lancering een lifetime korting op het reguliere tarief — definitieve prijzen volgen nog.",
    },
    {
      q: "Hoe lang duurt het om te starten?",
      a: "Een lesreeks instellen duurt ongeveer tien minuten. Een hele seizoensplanning ronden duurt — afhankelijk van je groepsgrootte — meestal één middag.",
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
