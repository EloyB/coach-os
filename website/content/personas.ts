export type PersonaSlug =
  | "voor-tennisclubs"
  | "voor-padelclubs"
  | "voor-trainers";

export interface Persona {
  slug: PersonaSlug;
  /** Short label for breadcrumbs and internal nav. */
  navLabel: string;
  /** Korte, natuurlijke ankertekst voor interne links naar deze pagina. */
  linkLabel: string;
  /** SEO meta title zonder merknaam: de titeltemplate in de root layout voegt " · CoachOS" toe. */
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
  /**
   * Uitgebreide, unieke inhoud per doelgroep (voorbeeld, inschrijven via een link, ...).
   * Rendert tussen het probleem/oplossing-blok en de interne links.
   */
  sections: PersonaSection[];
  /** Persona-specific FAQ — feeds FAQPage JSON-LD on this page. */
  faq: { q: string; a: string }[];
}

export interface PersonaSection {
  heading: string;
  paragraphs: string[];
  /** Optionele stappen of puntjes na de paragrafen. */
  bullets?: string[];
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
  linkLabel: "Lesplanning voor tennisscholen",
  metaTitle: "Lesplanning en software voor tennisscholen en -clubs",
  metaDescription:
    "Lesplanning voor je tennisschool of tennisclub: lessenreeksen, inschrijvingen via een link en automatische groepering op niveau. Geen Excel meer.",
  kicker: "VOOR TENNISCLUBS",
  h1: "Lesplanning voor tennisscholen en tennisclubs",
  lead: "CoachOS is software voor de lesplanning van tennisscholen en tennisclubs. Je beheert lessenreeksen, verzamelt inschrijvingen via een publieke link en laat het algoritme leerlingen op niveau verdelen, zonder Excel-sheets, zonder accounts voor leden en zonder eindeloos mailen. Zo beheer je je tennisschool met veel minder administratie.",
  pains: [
    {
      title: "Excel-sheets die elk seizoen opnieuw uit elkaar vallen",
      body: "Versies, formules, blocked celwaarden: bij elke nieuwe coach of seizoenswissel begint de chaos opnieuw.",
    },
    {
      title: "Eindeloos mailen over voorkeurstijden",
      body: "Ouders mailen tien keer hetzelfde, jij verwerkt het in een spreadsheet, en bij elke wijziging gaat het opnieuw rond.",
    },
    {
      title: "Met de hand puzzelen op niveau- en leeftijdsmatches",
      body: "Junior beginners niet bij gevorderden, A-spelers niet bij C-spelers, broers/zussen samenhouden: alles handmatig.",
    },
    {
      title: "Verschillende tools voor inschrijving, planning en betaling",
      body: "Google Forms voor de inschrijving, Excel voor de planning, een app voor betalingen: vier vinkjes om iemand te bevestigen.",
    },
  ],
  solutions: [
    {
      title: "Eén lessenreeks, hele seizoen geregeld",
      body: "Stel ritme, capaciteit en niveaus één keer in. Lessen worden automatisch gegenereerd over de hele periode.",
    },
    {
      title: "Anonieme inschrijflink: leerlingen vullen voorkeuren zelf in",
      body: "Een publieke link per lessenreeks. Naam, e-mail en voorkeuren, direct gekoppeld aan de juiste reeks.",
    },
    {
      title: "Planningsalgoritme verdeelt leerlingen automatisch",
      body: "Voorkeurstijden, niveaus en groepsverbanden worden meegenomen. Handmatige aanpassingen blijven bewaard bij herplannen.",
    },
    {
      title: "Mollie-betalingen + magic-link bevestigingen in dezelfde flow",
      body: "Inschrijving, planning, bevestiging en betaling lopen door één tool, zonder export-import.",
    },
  ],
  // TODO(Eloy): vervang het voorbeeldscenario door een echte seizoenscase van een pilootclub
  // zodra die er is (aantal inschrijvingen, doorlooptijd). Tot dan is "Stel dat" bewust hypothetisch.
  sections: [
    {
      heading: "Een voorbeeld: voorjaarslessen met 120 inschrijvingen",
      paragraphs: [
        "Stel dat je tennisschool voorjaarslessen aanbiedt en er komen 120 inschrijvingen binnen. Jeugd en volwassenen, beginners en gevorderden, elk met eigen voorkeuren voor dag en uur. Zonder hulpmiddelen is dat een week puzzelen met lijstjes. Met CoachOS verloopt het in vijf stappen.",
      ],
      bullets: [
        "Je maakt per niveau een lessenreeks aan met dag, uur, baan en groepsgrootte. De lessen worden automatisch over het hele seizoen gegenereerd.",
        "Je deelt de inschrijflink van elke reeks via je website, nieuwsbrief of sociale media.",
        "Leerlingen en ouders vullen hun gegevens en voorkeurstijden in. Je ziet de inschrijvingen binnenkomen, gekoppeld aan de juiste reeks.",
        "Zodra de inschrijvingen binnen zijn, laat je het planningsalgoritme draaien. Het verdeelt iedereen over de slots op niveau en voorkeurstijd en houdt groepsverbanden, zoals broers en zussen, samen.",
        "Je loopt het voorstel na, past aan waar nodig en bevestigt. Elke leerling krijgt een e-mail met een unieke bevestigingslink.",
      ],
    },
    {
      heading: "Inschrijven via een link, zonder account",
      paragraphs: [
        "Elke lessenreeks heeft een eigen publieke inschrijflink. Ouders en leerlingen openen die op hun gsm of laptop, vullen hun naam, e-mailadres en voorkeuren in en zijn klaar. Er is geen account en geen wachtwoord nodig, dus ook wie maar één keer per seizoen inschrijft, kan dat zonder gedoe.",
        "Wil je meer weten van je leerlingen, bijvoorbeeld hun ervaring of een opmerking voor de trainer, dan bouw je per lessenreeks een eigen formulier met tekstvelden, meerkeuzevragen en ja/nee-vragen. Na de inschrijving bevestigt de leerling zijn lesmoment met één klik via een unieke link in een e-mail.",
      ],
    },
    {
      heading: "Jij houdt de controle over de indeling",
      paragraphs: [
        "Het algoritme neemt het puzzelwerk over, maar jij beslist. Je kan elke indeling bekijken en met de hand aanpassen. Die aanpassingen blijven bewaard als je later opnieuw plant, bijvoorbeeld omdat er nog inschrijvingen bijkomen. Betalen kan online via Bancontact of iDEAL, en cash registreer je per inschrijving.",
      ],
    },
  ],
  faq: [
    {
      q: "Hoeveel banen kan ik beheren met CoachOS?",
      a: "Onbeperkt. Baan-naam is een vrij tekstveld op de lessenreeks, dus je kan zoveel banen toevoegen als je club heeft, of het nu gravel, hardcourt of indoor is.",
    },
    {
      q: "Werkt dit voor jeugd én volwassenen tegelijk?",
      a: "Ja. Per lessenreeks stel je niveau, leeftijd en groepsgrootte in. Het algoritme houdt rekening met alle drie bij het verdelen van leerlingen over slots.",
    },
    {
      q: "Kan ik tussentijds een nieuwe trainer toevoegen?",
      a: "Ja. Een bestaande clubadmin nodigt nieuwe trainers uit per e-mail. De trainer ziet enkel zijn eigen lessenreeksen. Multi-club rollen en rechten zijn ingebouwd.",
    },
    {
      q: "Kan ik mijn tennisschool beheren met meerdere trainers en banen?",
      a: "Ja. Het aantal trainers en banen is onbeperkt. Trainers ontvang je per e-mail en zien enkel hun eigen lessenreeksen, terwijl jij als clubadmin het overzicht over alle reeksen houdt.",
    },
    {
      q: "Wat als er na het plannen nog leerlingen bijkomen?",
      a: "Dan plan je opnieuw. Handmatige aanpassingen die je al gedaan hebt, blijven bewaard, dus je begint niet van nul.",
    },
    // TODO(Eloy): bevestig hoe het betaalmoment precies verloopt voor lessenreeksen (vs. kampen).
    {
      q: "Kunnen leerlingen online betalen?",
      a: "Ja. Online betalen kan via Mollie met Bancontact of iDEAL. Betaalt een leerling cash, dan registreer je dat per inschrijving.",
    },
  ],
};

// ─────────────────────────────────────────────────────────────────────────
// PADEL CLUBS
// ─────────────────────────────────────────────────────────────────────────

export const PADEL_CLUBS_PERSONA: Persona = {
  slug: "voor-padelclubs",
  navLabel: "Padelclubs",
  linkLabel: "Lesplanning voor padelscholen",
  metaTitle: "Lesplanning en software voor padelscholen en -clubs",
  metaDescription:
    "Lesplanning voor je padelschool of padelclub: beheer lessenreeksen, verzamel inschrijvingen op de gsm en plan automatisch op niveau. Zonder accounts voor spelers.",
  kicker: "VOOR PADELCLUBS",
  h1: "Lesplanning voor padelscholen en padelclubs",
  lead: "CoachOS is software voor de lesplanning van padelscholen en padelclubs. Spelers schrijven zich in via een publieke link op hun gsm, het algoritme groepeert op niveau en je beheert meerdere banen vanuit één dashboard, zonder app-download of account voor je leden.",
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
      title: "Veel inschrijvingen zonder extra admin-werk",
      body: "Geen limiet op inschrijvingen per lessenreeks en inschrijvingen worden nooit geblokkeerd. Het algoritme schaalt mee zodra de seizoen-aanmeldingen binnenstromen.",
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
  // TODO(Eloy): voorbeeldscenario is hypothetisch; vervang door een echte padelcase zodra die er is.
  sections: [
    {
      heading: "Een voorbeeld: een nieuwe reeks initiatielessen",
      paragraphs: [
        "Stel dat je padelschool in september start met initiatielessen voor beginners en vervolglessen voor spelers die al een tijd padelen. Padel groeit, de aanvraag is groot, en de meeste spelers schrijven in op hun gsm, vaak tussen twee afspraken door.",
      ],
      bullets: [
        "Je maakt een lessenreeks per niveau met baan, dag, uur en groepsgrootte.",
        "Je deelt de inschrijflink via je website, een sociale post of een bericht in de clubgroep.",
        "Spelers kiezen hun niveau en voorkeurstijden en zijn in een paar tikken klaar.",
        "Het algoritme groepeert spelers op niveau en voorkeurstijd. Een complete beginner komt niet naast een ervaren speler terecht.",
        "Je bevestigt de indeling en iedereen krijgt een e-mail met een bevestigingslink. Komen er later nog aanmeldingen bij, dan plan je opnieuw en blijven je handmatige aanpassingen staan.",
      ],
    },
    {
      heading: "Inschrijven op de gsm, zonder app of account",
      paragraphs: [
        "Spelers openen de inschrijflink in hun browser. Er is geen app om te downloaden en geen account om aan te maken: een naam, een e-mailadres en de gekozen voorkeuren zijn genoeg. De flow is mobile-first ontworpen, met één onderwerp per scherm en grote knoppen.",
        "Heb je extra informatie nodig, zoals speelervaring of met wie iemand graag speelt, dan voeg je die vraag toe in het formulier van de lessenreeks.",
      ],
    },
    // TODO(Eloy): bevestig dat de schaalstructuur hieronder (plannen tot 60/200/500 actieve leerlingen)
    // het juiste verhaal is voor padelscholen en pas /prijzen aan als dat verandert.
    {
      heading: "Groeien zonder extra administratie",
      paragraphs: [
        "Een padelschool die groeit, krijgt elk seizoen meer reeksen, meer banen en meer spelers. Daarom hangt je abonnement af van het aantal actieve leerlingen en niet van het aantal trainers of banen. Je kiest een plan tot 60, 200 of 500 actieve leerlingen, en inschrijvingen blijven altijd werken, ook als je boven je limiet zit.",
      ],
    },
  ],
  faq: [
    {
      q: "Werkt CoachOS goed op de gsm voor inschrijvingen?",
      a: "Ja. De inschrijfflow is mobile-first ontworpen: één scherm per stap, grote tap-targets, geen scroll-puzzels. Spelers schrijven zich in onder de minuut.",
    },
    {
      q: "Hoe gaat dit met snelle groei van mijn club?",
      a: "Er is geen limiet op inschrijvingen per lessenreeks en inschrijvingen worden nooit geblokkeerd. Je plan hangt af van je aantal actieve leerlingen (tot 60, 200 of 500). Zit je er structureel boven, dan stellen we een upgrade voor.",
    },
    {
      q: "Kunnen mijn spelers zonder app of account inschrijven?",
      a: "Ja. Geen download, geen account, alleen een publieke link. Bevestiging gebeurt via een magic-link in hun e-mail, zonder wachtwoord.",
    },
    {
      q: "Wat als een speler op meerdere niveaus wil les volgen?",
      a: "Per lessenreeks aparte inschrijving. Het planningsalgoritme houdt rekening met overlap zodat dezelfde speler niet op twee gelijktijdige slots ingedeeld wordt.",
    },
    {
      q: "Kan ik als padelschool meerdere banen en trainers beheren?",
      a: "Ja. De baan voeg je toe als vrij tekstveld op de lessenreeks en het aantal trainers is onbeperkt. Elke trainer ziet enkel zijn eigen lessenreeksen.",
    },
    {
      q: "Kunnen beginners en gevorderden in aparte reeksen les volgen?",
      a: "Ja. Je maakt per niveau een lessenreeks en het algoritme houdt bij het verdelen rekening met het gekozen niveau en de voorkeurstijden.",
    },
    {
      q: "Kan ik extra vragen stellen bij het inschrijven?",
      a: "Ja. Per lessenreeks bouw je een formulier met tekstvelden, meerkeuzevragen en ja/nee-vragen, bijvoorbeeld naar speelervaring.",
    },
  ],
};

// ─────────────────────────────────────────────────────────────────────────
// TRAINERS
// ─────────────────────────────────────────────────────────────────────────

export const TRAINERS_PERSONA: Persona = {
  slug: "voor-trainers",
  navLabel: "Trainers",
  linkLabel: "Planning voor tennis- en padeltrainers",
  metaTitle: "Planning voor tennis- en padeltrainers",
  metaDescription:
    "Planning voor tennis- en padeltrainers: lessenreeksen, inschrijvingen en bevestigingen in één tool. Geen Excel, geen eindeloos mailen, 30 dagen gratis proberen.",
  kicker: "VOOR TRAINERS",
  h1: "Planning voor tennis- en padeltrainers",
  lead: "CoachOS is software voor de lesplanning van tennis- en padeltrainers en hoofdtrainers. Stel een lessenreeks in, deel de inschrijflink en laat het algoritme de planning maken, terwijl jij op de baan staat in plaats van achter de laptop.",
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
      body: "Maandagen, woensdagavonden of zaterdagochtenden: elk ritme. Lessen genereren automatisch.",
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
  // TODO(Eloy): voorbeeldscenario is hypothetisch; vervang door een echte trainerscase of citaat zodra beschikbaar.
  sections: [
    {
      heading: "Een voorbeeld: een seizoen met groepen op meerdere locaties",
      paragraphs: [
        "Stel dat je als zelfstandige trainer op maandag en woensdag lesgeeft bij een club en op zaterdag je eigen groepen hebt op een andere locatie. Zonder hulpmiddelen houd je drie lijstjes bij, met telkens dezelfde mails over voorkeurstijden.",
      ],
      bullets: [
        "Elke club of locatie is een aparte organisatie in je account. Je wisselt met één klik tussen beide.",
        "Per groep maak je een lessenreeks met ritme, niveau en groepsgrootte.",
        "Je deelt per reeks één inschrijflink en laat de voorkeuren binnenkomen.",
        "Het algoritme maakt een voorstel op niveau en voorkeurstijd. Jij past aan en bevestigt.",
      ],
    },
    {
      heading: "Eén link per reeks: zo schrijven je leerlingen zich in",
      paragraphs: [
        "Je deelt de link in een bericht aan ouders, op je website of in een groepsapp. Leerlingen en ouders vullen naam, e-mailadres en voorkeuren in. Een account of wachtwoord is niet nodig. Hun voorkeuren komen direct binnen, gekoppeld aan de juiste reeks, zodat je niet meer in je mailbox moet zoeken wie welk uur wilde.",
        "Na het plannen ontvangt elke leerling een e-mail met een unieke link, waarmee hij zijn lestijd met één klik bevestigt.",
      ],
    },
    {
      heading: "Wat verdwijnt er uit je week?",
      paragraphs: [
        "De avond met lijstjes, het heen-en-weer mailen over voorkeurstijden en het met de hand vormen van niveaugroepen. Wat overblijft is het nakijken van het voorstel en het lesgeven zelf. Je betaalt per actieve leerling, met een onbeperkt aantal trainers, vanaf €25 per maand, en je probeert alles eerst 30 dagen gratis.",
      ],
    },
  ],
  faq: [
    {
      q: "Werkt CoachOS ook als ik aan meerdere clubs lesgeef?",
      a: "Ja. Multi-club via één account. Elke club is een aparte organisatie, je ziet alles vanuit één login en wisselt met één klik tussen rollen.",
    },
    {
      q: "Wat kost CoachOS voor zelfstandige trainers?",
      a: "Je betaalt per actieve leerling, met een onbeperkt aantal trainers: vanaf €25 per maand (of €250 per jaar met 2 maanden gratis). Je probeert alles eerst 30 dagen gratis, zonder betaalgegevens.",
    },
    {
      q: "Hoe lang duurt het om te starten?",
      a: "Een lessenreeks instellen duurt ongeveer tien minuten. Een hele seizoensplanning ronden duurt, afhankelijk van je groepsgrootte, meestal één middag.",
    },
    {
      q: "Heb ik administratieve hulp nodig om dit te gebruiken?",
      a: "Nee. CoachOS is gemaakt voor solo-werk. Geen IT-ondersteuning, geen aparte beheerder, geen accountantsexport om te leren. Het werkt out-of-the-box.",
    },
    // TODO(Eloy): bevestig dat een trainer zonder club zelf een organisatie kan aanmaken en starten.
    {
      q: "Kan ik CoachOS gebruiken zonder club, voor mijn eigen groepen?",
      a: "Ja. Je werkt vanuit je eigen organisatie en stelt lessenreeksen in voor je eigen groepen. Je hebt geen club nodig om te starten.",
    },
    {
      q: "Hoe schrijven leerlingen zich in bij mijn lessenreeks?",
      a: "Via een publieke link die je zelf deelt. Leerlingen of ouders vullen hun naam, e-mailadres en voorkeuren in, zonder account of wachtwoord.",
    },
    // TODO(Eloy): bevestig dat tennis- en padelreeksen in dezelfde organisatie naast elkaar kunnen lopen.
    {
      q: "Kan ik tegelijk tennis- en padellessen plannen?",
      a: "Ja. CoachOS is gemaakt voor beide sporten, en je plant tennis- en padelreeksen vanuit hetzelfde dashboard.",
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
