import type { BlogPost } from "@/content/blog/types";

export const ANONIEME_INSCHRIJVING_AVG: BlogPost = {
  slug: "anonieme-inschrijving-avg",
  title: "Anonieme inschrijving: AVG-conform leerlingen onboarden",
  metaTitle:
    "Anonieme inschrijving: AVG-conform leerlingen onboarden · CoachOS",
  metaDescription:
    "Hoe een sportclub leerlingen inschrijft zonder accounts en AVG-conform werkt: minimale data, magic-link bevestiging, duidelijke bewaartermijnen.",
  publishedAt: "2026-05-07",
  readMinutes: 5,
  category: "GIDS · AVG",
  tags: ["AVG", "GDPR", "anonieme inschrijving", "privacy", "magic link"],
  lead: "Anonieme inschrijving betekent dat een leerling zich kan inschrijven voor een lessenreeks zonder een account aan te maken: alleen naam en e-mailadres volstaan, en bevestiging gebeurt via een tijdelijke magic-link in plaats van een wachtwoord. Voor sportclubs is dit niet alleen gebruiksvriendelijker — het is ook de meest AVG-conforme manier om leerlinggegevens te onboarden, omdat je per definitie minder data verzamelt dan strikt nodig.",
  sections: [
    {
      heading: "Wat 'anonieme inschrijving' juridisch betekent",
      paragraphs: [
        "Strikt genomen is een inschrijving met naam en e-mailadres niet anoniem — die data identificeren een persoon en vallen onder de AVG. De term 'anonieme inschrijving' wordt in de praktijk gebruikt om aan te duiden dat de leerling geen permanent account hoeft aan te maken: geen profielpagina, geen wachtwoord, geen tracking.",
        "Het verschil zit in de hoeveelheid data en de bewaartermijn. Een account-gebaseerd systeem houdt typisch een persistent profiel bij dat blijft bestaan ongeacht of de leerling nog actief is. Een anonieme inschrijving verzamelt alleen wat nodig is voor één specifieke lessenreeks, en de data verlopen samen met die reeks.",
      ],
    },
    {
      heading: "Waarom geen accounts voor leerlingen?",
      paragraphs: [
        "Accounts klinken professioneel maar voegen voor een tennisles weinig toe. Ouders krijgen een mail, klikken op de bevestigingslink, en zijn klaar — een wachtwoord onthouden voor één seizoen lessen is overhead die niemand wil. Resultaat: lagere conversie op de inschrijfflow en meer support-vragen ('ik kan niet inloggen').",
        "Vanuit AVG-perspectief is het sterker: hoe minder accounts er bestaan, hoe minder lekken er kunnen lekken. Geen wachtwoorden om te beveiligen, geen sessietokens om te beheren, geen wachtwoord-reset-flows om correct te implementeren. Minder oppervlakte = minder risico.",
      ],
      callout: {
        tone: "info",
        text: "Het AVG-principe van 'data minimization' (artikel 5.1.c) vraagt expliciet om alleen die persoonsgegevens te verzamelen die nodig zijn voor het doel. Een account aanmaken voor 'misschien volgend seizoen ook' valt daar niet onder.",
      },
    },
    {
      heading: "Welke gegevens mag je verzamelen?",
      paragraphs: [
        "Voor lessenplanning is dit het minimum: voornaam, achternaam, e-mailadres en de antwoorden op je inschrijvingsformulier (niveau, voorkeurstijden, eventueel een opmerking). Telefoon mag, maar alleen als je het echt gebruikt voor lesgerelateerde communicatie — niet 'voor de zekerheid'.",
        "Wat absoluut niet thuishoort in een inschrijvingsformulier: rijksregisternummer, geboortedatum (tenzij relevant voor leeftijdsgroepen), betaalgegevens (die horen bij de betaalprovider, niet in jouw database), of medische info (tenzij specifiek noodzakelijk en met aparte toestemming).",
      ],
      bullets: [
        "Voornaam + achternaam: ✅ noodzakelijk",
        "E-mailadres: ✅ noodzakelijk voor bevestigingen en lescommunicatie",
        "Telefoonnummer: ⚠️ alleen als je het ook echt gebruikt",
        "Geboortedatum: ⚠️ enkel voor leeftijdsgebonden niveaus, niet 'standaard'",
        "Adres: ❌ niet nodig voor lessenplanning",
        "Rijksregister of BSN: ❌ verboden zonder wettelijke grondslag",
      ],
    },
    {
      heading: "Magic-link bevestigingen in plaats van wachtwoorden",
      paragraphs: [
        "Een magic-link is een tijdelijke, eenmalig bruikbare URL die je naar het e-mailadres van de leerling stuurt. Klik = bevestiging. Geen wachtwoord aanmaken, geen wachtwoord onthouden, geen wachtwoord resetten. Vanuit AVG-oogpunt elimineert dit een hele categorie risico's: er bestaan geen wachtwoord-hashes om te lekken.",
        "Belangrijk wel: een magic-link moet aflopen. CoachOS gebruikt 24 uur als standaard, met de mogelijkheid om opnieuw aan te vragen. Een magic-link die nooit verloopt is feitelijk een onbeperkte sleutel — niet meer veiliger dan een gelekt wachtwoord.",
      ],
    },
    {
      heading: "Bewaartermijnen — hoe lang mag je inschrijfdata houden?",
      paragraphs: [
        "Er is geen vaste wettelijke termijn voor inschrijfgegevens van een sportclub, maar de AVG vraagt om een 'gerechtvaardigd doel' voor de bewaartermijn. Voor lessenplanning is dat: het lopende seizoen plus één seizoen erna voor opvolging (heeft de leerling zich opnieuw ingeschreven, levert de groepering inzichten op voor volgend jaar).",
        "Praktische richtlijn: bewaar inschrijfgegevens 14 maanden na het einde van de lessenreeks. Daarna automatisch geanonimiseerd of verwijderd. Boekhoudkundige verplichtingen (factuurdata, betalingen) kunnen langer bewaard blijven, maar daarvoor heb je een aparte wettelijke grondslag.",
      ],
      callout: {
        tone: "tip",
        text: "Documenteer je bewaartermijnen in je privacyverklaring. Bij een eventuele klacht of inspectie is een geschreven beleid + naleving veel waard. Een tool die automatisch verwijdert na X maanden is daarbij een sterk pluspunt.",
      },
    },
    {
      heading: "Wat als een leerling vragen stelt over zijn data?",
      paragraphs: [
        "Onder de AVG heeft elke betrokkene een recht op inzage, correctie, verwijdering en data-portabiliteit. Voor een sportclub vertaalt zich dat naar één concreet ding: er moet een werkbare manier zijn om die rechten in te roepen, en je moet binnen één maand reageren.",
        "In de praktijk: zet een duidelijk e-mailadres voor privacy-verzoeken op je website (bijvoorbeeld privacy@jouwclub.be), en zorg dat je tool die data daadwerkelijk kan inzien, corrigeren en verwijderen op verzoek. Een handmatig 'rij verwijderen in Excel' werkt niet — er moet traceerbaarheid zijn dat het verzoek is uitgevoerd.",
      ],
    },
  ],
  faq: [
    {
      q: "Mag ik e-mail en naam zonder expliciete toestemming bijhouden?",
      a: "Ja, op basis van 'uitvoering van een overeenkomst' (artikel 6.1.b AVG). De inschrijving is de overeenkomst; het verzamelen van naam en e-mail is noodzakelijk om die uit te voeren. Aparte toestemming is alleen nodig voor bijkomende verwerkingen, zoals nieuwsbrieven of foto's.",
    },
    {
      q: "Hoe lang mag ik inschrijfdata bewaren?",
      a: "Geen vaste wettelijke termijn voor de inschrijfgegevens zelf. Praktische richtlijn: 14 maanden na het einde van de lessenreeks, daarna anonimiseren of verwijderen. Factuurdata mogen 7 jaar bewaard blijven (boekhoudplicht).",
    },
    {
      q: "Wat als ouders inschrijven voor hun kinderen onder de 16?",
      a: "De ouder geeft toestemming namens het kind. Het e-mailadres van de ouder is dan het primaire contactkanaal. Vermeld in je privacyverklaring expliciet dat je inschrijvingen van minderjarigen via de ouder verwerkt.",
    },
    {
      q: "Heb ik een verwerkingsregister nodig?",
      a: "Vanaf 250 leerlingen of bij verwerking van bijzondere persoonsgegevens (bijvoorbeeld medische info) is een verwerkingsregister verplicht. Voor kleinere clubs is het sterk aangeraden — het zorgt dat je weet welke data je verzamelt en waarom.",
    },
    {
      q: "Wat als CoachOS de data verwerkt — wie is verantwoordelijk?",
      a: "Jij als club blijft verwerkingsverantwoordelijke; CoachOS treedt op als verwerker. Daarvoor hoort een verwerkersovereenkomst (DPA) bij die de afspraken vastlegt. CoachOS levert die als standaardbijlage bij elke pilot- of klantcontract.",
    },
  ],
  related: ["hoe-plan-je-lesseizoen-tennisclub"],
};
