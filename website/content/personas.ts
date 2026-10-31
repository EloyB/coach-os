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
  /** Optionele eigen H2 boven de pijnpunten; standaardtekst als leeg. */
  problemHeading?: string;
  /** Optionele eigen H2 boven de oplossingen; standaardtekst als leeg. */
  solutionHeading?: string;
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
  linkLabel: "Lessenplanning voor tennisscholen",
  metaTitle: "Lessenplanning voor tennisscholen en -clubs",
  metaDescription:
    "Lessenplanning voor je tennisschool of tennisclub: lessenreeksen, inschrijven via een link en automatisch plannen op voorkeur. Geen Excel meer. 30 dagen gratis proberen.",
  kicker: "VOOR TENNISCLUBS",
  h1: "Lessenplanning voor tennisscholen en tennisclubs",
  lead: "CoachOS is software voor de lessenplanning van tennisscholen en tennisclubs. Je beheert lessenreeksen, verzamelt inschrijvingen via een publieke link en laat het algoritme leerlingen over de tijdsloten verdelen op basis van hun voorkeur, zonder Excel-sheets, zonder accounts voor leden en zonder eindeloos mailen. Zo beheer je je tennisschool met veel minder administratie.",
  problemHeading: "Waarom lessen plannen in Excel vastloopt",
  solutionHeading: "Zo plant CoachOS de lessen van je tennisschool",
  pains: [
    {
      title: "Excel-sheets die elk seizoen opnieuw uit elkaar vallen",
      body: "Versies, formules, verdwaalde celwaarden en kleurcodes: bij elke nieuwe coach of seizoenswissel begint de chaos opnieuw.",
    },
    {
      title: "Eindeloos mailen over voorkeurstijden",
      body: "Ouders mailen tien keer hetzelfde, jij verwerkt het in een spreadsheet, en bij elke wijziging gaat het opnieuw rond.",
    },
    {
      title: "Met de hand puzzelen met voorkeurstijden en groepjes",
      body: "Wie kan wanneer, wie moet bij wie blijven, broers en zussen samenhouden: alles handmatig.",
    },
    {
      title: "Verschillende tools voor inschrijving, planning en betaling",
      body: "Google Forms voor de inschrijving, Excel voor de planning, een app voor betalingen: drie tools voor één bevestiging.",
    },
  ],
  solutions: [
    {
      title: "Eén lessenreeks, hele seizoen geregeld",
      body: "Stel ritme, capaciteit en niveaus één keer in. Lessen worden automatisch gegenereerd over de hele periode.",
    },
    {
      title: "Inschrijflink zonder account: leerlingen vullen voorkeuren zelf in",
      body: "Een publieke link per lessenreeks. Naam, e-mail en voorkeuren, direct gekoppeld aan de juiste reeks.",
    },
    {
      title: "Planningsalgoritme verdeelt leerlingen automatisch",
      body: "Voorkeurstijden en groepsverbanden worden meegenomen, zodat wie samen is ingeschreven ook samen les krijgt. Handmatige aanpassingen blijven bewaard bij herplannen.",
    },
    {
      title: "Betalen en bevestigen in dezelfde flow",
      body: "Inschrijving, planning, bevestiging en betaling lopen door één tool, zonder export-import.",
    },
  ],
  // Voorbeeldscenario is bewust hypothetisch ("Stel dat").
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
        "Zodra de inschrijvingen binnen zijn, laat je het planningsalgoritme draaien. Het verdeelt iedereen over de slots op voorkeurstijd en houdt groepsverbanden, zoals broers en zussen, samen.",
        "Je loopt het voorstel na, past aan waar nodig en bevestigt. Elke leerling krijgt een e-mail met een unieke bevestigingslink en betaalt daarna online.",
      ],
    },
    {
      heading: "Tennislessen inschrijven via een link, zonder account",
      paragraphs: [
        "Elke lessenreeks heeft een eigen publieke inschrijflink. Ouders en leerlingen openen die op hun gsm of laptop, vullen hun naam, e-mailadres en voorkeuren in en zijn klaar. Er is geen account en geen wachtwoord nodig, dus ook wie maar één keer per seizoen inschrijft, kan dat zonder gedoe.",
        "Wil je meer weten van je leerlingen, bijvoorbeeld hun ervaring of een opmerking voor de trainer, dan bouw je per lessenreeks een eigen formulier met tekstvelden, meerkeuzevragen en ja/nee-vragen. Na de inschrijving bevestigt de leerling het lesmoment met één klik via een unieke link in een e-mail.",
      ],
    },
    {
      heading: "Jij houdt de controle over de indeling",
      paragraphs: [
        "Het algoritme neemt het puzzelwerk over, maar jij beslist. Je kan elke indeling bekijken en met de hand aanpassen. Die aanpassingen blijven bewaard als je later opnieuw plant, bijvoorbeeld omdat er nog inschrijvingen bijkomen. Na de bevestiging van de planning betalen leerlingen online via Bancontact of iDEAL, en cash registreer je per inschrijving.",
      ],
    },
    {
      heading: "Tennisstages en zomerkampen organiseren",
      paragraphs: [
        "Naast lessenreeksen organiseer je met CoachOS ook tennisstages en zomerkampen: hele weken, enkele dagen of halve dagen, met vaste pakketten en een limiet per dagdeel. Deelnemers schrijven zich in via dezelfde publieke link, zonder account, en betalen net als bij lessen nadat de planning bevestigd is.",
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
      a: "Ja. Je maakt per doelgroep een eigen lessenreeks, bijvoorbeeld per niveau of leeftijdsgroep, met eigen ritme, capaciteit en groepsgrootte. Het algoritme verdeelt de leerlingen van elke reeks over de tijdsloten op basis van hun voorkeur en houdt groepsverbanden samen.",
    },
    {
      q: "Kan ik mijn tennisschool beheren met meerdere trainers en banen?",
      a: "Ja. Het aantal trainers en banen is onbeperkt. Als clubadmin nodig je trainers uit per e-mail, ook tijdens het seizoen. Elke trainer ziet enkel de eigen lessenreeksen, terwijl jij het overzicht houdt over alle reeksen.",
    },
    {
      q: "Wat als er na het plannen nog leerlingen bijkomen?",
      a: "Dan plan je opnieuw. Handmatige aanpassingen die je al gedaan hebt, blijven bewaard, dus je begint niet van nul.",
    },
    {
      q: "Kunnen leerlingen online betalen?",
      a: "Ja. Nadat de planning bevestigd is, betalen leerlingen online via Mollie met Bancontact of iDEAL. Betaalt een leerling cash, dan registreer je dat per inschrijving.",
    },
    {
      q: "Kan ik tennisstages en zomerkampen organiseren?",
      a: "Ja. Een kamp of stage stel je in met hele weken, enkele dagen of halve dagen, vaste pakketten en een limiet per dagdeel. Inschrijven gaat via een publieke link zonder account, en betalen kan na de bevestiging van de planning.",
    },
    {
      q: "Wat kost lessenplanning voor een tennisschool?",
      a: "Vanaf €25 per maand voor tot 60 actieve leerlingen, €49 voor tot 200 en €89 voor tot 500, met een onbeperkt aantal trainers en alle functies inbegrepen. Alle prijzen zijn excl. btw en je probeert eerst 30 dagen gratis. Alle details staan op de prijzenpagina.",
    },
  ],
};

// ─────────────────────────────────────────────────────────────────────────
// PADEL CLUBS
// ─────────────────────────────────────────────────────────────────────────

export const PADEL_CLUBS_PERSONA: Persona = {
  slug: "voor-padelclubs",
  navLabel: "Padelclubs",
  linkLabel: "Lessenplanning voor padelscholen",
  metaTitle: "Lessenplanning voor padelscholen en -clubs",
  metaDescription:
    "Lessenplanning voor je padelschool of padelclub: lessenreeksen, inschrijven op de gsm en automatisch plannen op voorkeur. Zonder account voor spelers. 30 dagen gratis proberen.",
  kicker: "VOOR PADELCLUBS",
  h1: "Lessenplanning voor padelscholen en padelclubs",
  lead: "CoachOS is software voor de lessenplanning van padelscholen en padelclubs. Spelers schrijven zich in via een publieke link op hun gsm, het algoritme plant op voorkeurstijd en je beheert meerdere banen vanuit één dashboard, zonder app-download of account voor je leden.",
  problemHeading: "Waarom padellessen plannen met Excel en WhatsApp vastloopt",
  solutionHeading: "Zo plant CoachOS de lessen van je padelschool",
  pains: [
    {
      title: "Snelle groei die handmatig niet bij te houden is",
      body: "Padel groeit snel in België en Nederland. Wat vorig jaar nog op één Excel-sheet paste, is nu een chaos van WhatsApp-berichten en losse mails.",
    },
    {
      title: "Niveauverschillen tussen beginners en gevorderde spelers",
      body: "Een complete beginner aanmelden naast een ervaren speler levert frustratie aan beide kanten. Handmatig schiften is niet schaalbaar.",
    },
    {
      title: "Spelers die liefst op de gsm inschrijven",
      body: "Padelspelers schrijven vaak in op hun gsm. Een formulier dat enkel op desktop werkt verliest spelers nog voor ze ingeschreven zijn.",
    },
  ],
  solutions: [
    {
      title: "Veel inschrijvingen zonder extra administratie",
      body: "Inschrijvingen worden nooit geblokkeerd, ook niet als je boven de limiet van je plan zit. Het algoritme verdeelt alle aanmeldingen met één klik, ook als ze in grote aantallen binnenstromen.",
    },
    {
      title: "Aparte reeksen per niveau, automatische planning op voorkeur",
      body: "Je maakt een lessenreeks per niveau en spelers kiezen de reeks die bij hen past. Voorkeurstijden en groepsverbanden worden daarna samen meegenomen.",
    },
    {
      title: "Mobiel-vriendelijke inschrijfflow zonder app",
      body: "De inschrijflink werkt vlot op elke gsm. Geen download, geen account, geen wachtwoord. Spelers vullen hun naam, e-mail en de vragen uit jouw formulier in.",
    },
  ],
  // Voorbeeldscenario is bewust hypothetisch ("Stel dat").
  sections: [
    {
      heading: "Een voorbeeld: een nieuwe reeks initiatielessen",
      paragraphs: [
        "Stel dat je padelschool in september start met initiatielessen voor beginners en vervolglessen voor spelers die al een tijd padelen. Padel groeit, de aanvraag is groot, en de meeste spelers schrijven in op hun gsm, vaak tussen twee afspraken door.",
      ],
      bullets: [
        "Je maakt een lessenreeks per niveau met baan, dag, uur en groepsgrootte.",
        "Je deelt de inschrijflink via je website, een sociale post of een bericht in de clubgroep.",
        "Spelers kiezen de reeks die bij hun niveau past, geven hun voorkeurstijden door en zijn in een paar tikken klaar.",
        "Het algoritme verdeelt spelers per reeks op voorkeurstijd en houdt groepsverbanden samen. Omdat je per niveau een reeks maakt, komt een complete beginner niet naast een ervaren speler terecht.",
        "Je bevestigt de indeling en iedereen krijgt een e-mail met een bevestigingslink. Komen er later nog aanmeldingen bij, dan plan je opnieuw en blijven je handmatige aanpassingen staan.",
      ],
    },
    {
      heading: "Padellessen inschrijven op de gsm, zonder app of account",
      paragraphs: [
        "Spelers openen de inschrijflink in hun browser. Er is geen app om te downloaden en geen account om aan te maken: een naam, een e-mailadres en de gekozen voorkeuren zijn genoeg. Het formulier is mobile-first ontworpen, zodat het vlot werkt op elke gsm.",
        "Heb je extra informatie nodig, zoals speelervaring of met wie iemand graag speelt, dan voeg je die vraag toe in het formulier van de lessenreeks.",
      ],
    },
    {
      heading: "Padelstages en kampen organiseren",
      paragraphs: [
        "Naast lessenreeksen organiseer je met CoachOS ook padelstages en kampen: hele weken, enkele dagen of halve dagen, met vaste pakketten en een limiet per dagdeel. Deelnemers schrijven zich in via dezelfde publieke link, zonder account, en betalen online nadat de planning bevestigd is.",
      ],
    },
    {
      heading: "Padel en tennis in één organisatie",
      paragraphs: [
        "Biedt je club zowel padel als tennis aan? Dan beheer je beide in één organisatie, met één overzicht van al je lessenreeksen en trainers.",
      ],
    },
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
      a: "Ja. Het inschrijfformulier is mobile-first ontworpen en werkt vlot op elke gsm, zonder app-download of account.",
    },
    {
      q: "Hoe gaat dit met snelle groei van mijn club?",
      a: "Inschrijvingen worden nooit geblokkeerd, ook niet als je boven de limiet van je plan zit. Je plan hangt af van je aantal actieve leerlingen (tot 60, 200 of 500). Zit je er structureel boven, dan stellen we een upgrade voor.",
    },
    {
      q: "Kunnen mijn spelers zonder app of account inschrijven?",
      a: "Ja. Geen download, geen account, alleen een publieke link. Bevestiging gebeurt via een link in hun e-mail, zonder wachtwoord.",
    },
    {
      q: "Wat als een speler op meerdere niveaus wil les volgen?",
      a: "Ja. Per lessenreeks is er een aparte inschrijving via de eigen inschrijflink.",
    },
    {
      q: "Kan ik als padelschool meerdere banen en trainers beheren?",
      a: "Ja. De baan voeg je toe als vrij tekstveld op de lessenreeks en het aantal trainers is onbeperkt. Elke trainer ziet enkel de eigen lessenreeksen.",
    },
    {
      q: "Kunnen beginners en gevorderden in aparte reeksen les volgen?",
      a: "Ja. Je maakt per niveau een lessenreeks en spelers kiezen de reeks die bij hun niveau past. Binnen die reeks verdeelt het algoritme de spelers op basis van hun voorkeurstijden.",
    },
    {
      q: "Kan ik extra vragen stellen bij het inschrijven?",
      a: "Ja. Per lessenreeks bouw je een formulier met tekstvelden, meerkeuzevragen en ja/nee-vragen, bijvoorbeeld naar speelervaring.",
    },
    {
      q: "Kunnen spelers online betalen?",
      a: "Ja. Nadat de planning bevestigd is, betalen spelers online via Mollie met Bancontact of iDEAL. Betaalt iemand cash, dan registreer je dat per inschrijving.",
    },
    {
      q: "Kan ik padelstages en kampen organiseren?",
      a: "Ja. Een stage of kamp stel je in met hele weken, enkele dagen of halve dagen, vaste pakketten en een limiet per dagdeel. Inschrijven gaat via een publieke link zonder account, en betalen kan na de bevestiging van de planning.",
    },
    {
      q: "Kan ik padel en tennis samen beheren?",
      a: "Ja. Eén organisatie kan zowel padel als tennis aanbieden, met één overzicht van alle lessenreeksen en trainers.",
    },
    {
      q: "Wat kost lessenplanning voor een padelschool?",
      a: "Vanaf €25 per maand voor tot 60 actieve leerlingen, €49 voor tot 200 en €89 voor tot 500, met een onbeperkt aantal trainers en alle functies inbegrepen. Alle prijzen zijn excl. btw en je probeert eerst 30 dagen gratis. Alle details staan op de prijzenpagina.",
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
  lead: "CoachOS is software voor de lessenplanning van tennis- en padeltrainers en hoofdtrainers. Stel een lessenreeks in, deel de inschrijflink en laat het algoritme de planning maken, terwijl jij op de baan staat in plaats van achter de laptop.",
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
      title: "Algoritme plant op voorkeur en groepsverband",
      body: "Planning op voorkeurstijd en groepsverbanden. Aanpassingen achteraf blijven bewaard wanneer je opnieuw plant.",
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
        "Het algoritme maakt een voorstel op voorkeurstijd en groepsverband. Jij past aan en bevestigt.",
      ],
    },
    {
      heading: "Eén link per reeks: zo schrijven je leerlingen zich in",
      paragraphs: [
        "Je deelt de link in een bericht aan ouders, op je website of in een groepsapp. Leerlingen en ouders vullen naam, e-mailadres en voorkeuren in. Een account of wachtwoord is niet nodig. Hun voorkeuren komen direct binnen, gekoppeld aan de juiste reeks, zodat je niet meer in je mailbox moet zoeken wie welk uur wilde.",
        "Na het plannen ontvangt elke leerling een e-mail met een unieke link, waarmee de leerling de lestijd met één klik bevestigt.",
      ],
    },
    {
      heading: "Wat verdwijnt er uit je week?",
      paragraphs: [
        "De avond met lijstjes, het heen-en-weer mailen over voorkeurstijden en het met de hand verdelen van leerlingen over tijdsloten. Wat overblijft is het nakijken van het voorstel en het lesgeven zelf. Je betaalt per actieve leerling, met een onbeperkt aantal trainers, vanaf €25 per maand, en je probeert alles eerst 30 dagen gratis.",
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
      a: "Een lessenreeks instellen duurt ongeveer tien minuten. Een hele seizoensplanning ronden kost een fractie van de tijd die je met Excel kwijt bent: pilotclubs houden er dagen werk aan over.",
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
