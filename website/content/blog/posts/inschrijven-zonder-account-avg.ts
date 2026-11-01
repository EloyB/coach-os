import type { BlogPost } from "@/content/blog/types";

export const INSCHRIJVEN_ZONDER_ACCOUNT_AVG: BlogPost = {
  slug: "inschrijven-zonder-account-avg",
  title: "Inschrijven zonder account: AVG-conform leerlingen onboarden",
  metaTitle: "Inschrijven zonder account: AVG-conform leerlingen onboarden",
  metaDescription:
    "Hoe een sportclub leerlingen inschrijft zonder accounts en AVG-conform werkt: minimale data, bevestiging via een link, duidelijke bewaartermijnen.",
  publishedAt: "2026-05-07",
  updatedAt: "2026-11-01",
  readMinutes: 6,
  category: "GIDS · AVG",
  tags: [
    "AVG",
    "GDPR",
    "inschrijven zonder account",
    "anonieme inschrijving",
    "privacy",
  ],
  lead: "Inschrijven zonder account betekent dat een leerling zich kan inschrijven voor een lessenreeks zonder een profiel of wachtwoord aan te maken: naam, e-mailadres en de vragen uit het inschrijvingsformulier volstaan, en bevestiging gebeurt via een persoonlijke link in een e-mail. Voor sportclubs is dit niet alleen gebruiksvriendelijker, het helpt ook om AVG-conform te werken, omdat je minder gegevens verzamelt en bewaart dan met een account-gebaseerd systeem. Dit artikel is algemene informatie en geen juridisch advies.",
  sections: [
    {
      heading: "Is inschrijven zonder account hetzelfde als anoniem?",
      paragraphs: [
        "Strikt genomen is een inschrijving met naam en e-mailadres niet anoniem: die gegevens identificeren een persoon en vallen onder de AVG. Met 'anonieme inschrijving' bedoelen mensen in de praktijk dat de leerling geen permanent account hoeft aan te maken: geen profielpagina en geen wachtwoord.",
        "Het verschil zit in de hoeveelheid gegevens en de bewaartermijn. Een account-gebaseerd systeem houdt typisch een persistent profiel bij dat blijft bestaan ongeacht of de leerling nog actief is. Een inschrijving zonder account verzamelt alleen wat nodig is voor één specifieke lessenreeks.",
      ],
    },
    {
      heading: "Waarom geen accounts voor leerlingen?",
      paragraphs: [
        "Accounts klinken professioneel maar voegen voor een tennis- of padelles weinig toe. Ouders krijgen een mail, klikken op de bevestigingslink, en zijn klaar. Een wachtwoord onthouden voor één seizoen lessen is overhead die niemand wil. Het gevolg: meer afhakers in de inschrijfflow en meer vragen als 'ik kan niet inloggen'.",
        "Vanuit AVG-perspectief is het sterker: hoe minder accounts er bestaan, hoe minder er kan uitlekken. Geen wachtwoorden om te beveiligen, geen sessietokens om te beheren, geen wachtwoord-reset-flows om correct te implementeren. Minder oppervlakte betekent minder risico.",
      ],
      callout: {
        tone: "info",
        text: "Het AVG-principe van 'dataminimalisatie' (artikel 5.1.c) vraagt expliciet om alleen die persoonsgegevens te verzamelen die nodig zijn voor het doel. Een account aanmaken voor 'misschien volgend seizoen ook' valt daar niet onder.",
      },
    },
    {
      heading: "Welke gegevens mag je verzamelen?",
      paragraphs: [
        "Voor lessenplanning is dit het minimum: voornaam, achternaam, e-mailadres en de antwoorden op je inschrijvingsformulier (niveau, voorkeurstijden, eventueel een opmerking). Telefoon mag, maar alleen als je het echt gebruikt voor lesgerelateerde communicatie, niet 'voor de zekerheid'.",
        "Wat niet thuishoort in een inschrijvingsformulier: rijksregisternummer of BSN, geboortedatum (tenzij relevant voor leeftijdsgroepen), betaalgegevens (die horen bij de betaalprovider, niet in jouw database), of medische informatie (tenzij specifiek noodzakelijk en met aparte toestemming).",
      ],
      bullets: [
        "Voornaam + achternaam: ✅ noodzakelijk",
        "E-mailadres: ✅ noodzakelijk voor bevestigingen en lescommunicatie",
        "Telefoonnummer: ⚠️ alleen als je het ook echt gebruikt",
        "Geboortedatum: ⚠️ enkel voor leeftijdsgebonden niveaus, niet 'standaard'",
        "Adres: ❌ niet nodig voor lessenplanning",
        "Rijksregisternummer of BSN: ❌ niet toegestaan zonder wettelijke grondslag",
      ],
    },
    {
      heading: "Bevestigingslinks in plaats van wachtwoorden",
      paragraphs: [
        "Een bevestigingslink is een tijdelijke URL die je naar het e-mailadres van de leerling stuurt. Klik en het is bevestigd. Geen wachtwoord aanmaken, geen wachtwoord onthouden, geen wachtwoord resetten. Vanuit AVG-oogpunt elimineert dit een hele categorie risico's: er bestaan geen wachtwoord-hashes om te lekken.",
        "Belangrijk wel: zo'n link moet aflopen. In CoachOS verloopt een bevestigingslink na 72 uur. Een link die nooit verloopt is feitelijk een onbeperkte sleutel, niet veiliger dan een gelekt wachtwoord.",
      ],
    },
    {
      heading: "Bewaartermijnen: hoe lang mag je inschrijfdata houden?",
      paragraphs: [
        "Er is geen vaste wettelijke termijn voor inschrijfgegevens van een sportclub, maar de AVG vraagt om een gerechtvaardigd doel voor de bewaartermijn. Voor lessenplanning is dat: het lopende seizoen plus een korte periode erna voor opvolging, bijvoorbeeld om te zien of een leerling zich opnieuw inschrijft.",
        "Een praktische aanbeveling is om inschrijfgegevens ongeveer 14 maanden na het einde van de lessenreeks te verwijderen of te anonimiseren. Dat is een richtlijn, geen wettelijke termijn. Boekhoudkundige gegevens (facturen en betalingen) bewaar je minstens 7 jaar; daarvoor geldt een aparte wettelijke grondslag.",
      ],
      callout: {
        tone: "tip",
        text: "Documenteer je bewaartermijnen in je privacyverklaring en spreek af wie de gegevens na die termijn verwijdert. Bij een eventuele klacht of inspectie is een geschreven beleid plus naleving veel waard.",
      },
    },
    {
      heading: "Wat als een leerling vragen stelt over zijn of haar data?",
      paragraphs: [
        "Onder de AVG heeft elke betrokkene recht op inzage, correctie, verwijdering en gegevensoverdraagbaarheid. Voor een sportclub vertaalt zich dat naar één concreet ding: er moet een werkbare manier zijn om die rechten in te roepen, en je moet binnen één maand reageren.",
        "In de praktijk: zet een duidelijk e-mailadres voor privacy-verzoeken op je website (bijvoorbeeld privacy@jouwclub.be), en zorg dat je die gegevens ook daadwerkelijk kan inzien, corrigeren en verwijderen. Een handmatig 'rij verwijderen in Excel' werkt niet: er moet traceerbaarheid zijn dat het verzoek is uitgevoerd.",
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
      a: "Er is geen vaste wettelijke termijn voor de inschrijfgegevens zelf. Een praktische richtlijn is ongeveer 14 maanden na het einde van de lessenreeks, daarna verwijderen of anonimiseren. Facturen en betalingsgegevens bewaar je minstens 7 jaar (boekhoudplicht).",
    },
    {
      q: "Wat als ouders inschrijven voor hun kinderen?",
      a: "Bij minderjarigen schrijft de ouder of voogd in en geeft die toestemming namens het kind. Het e-mailadres van de ouder is dan het primaire contactkanaal. Vermeld in je privacyverklaring expliciet dat je inschrijvingen van minderjarigen via de ouder verwerkt.",
    },
    {
      q: "Moet ik een verwerkingsregister bijhouden?",
      a: "Waarschijnlijk wel. De AVG (artikel 30) verplicht een verwerkingsregister voor organisaties met 250 of meer werknemers, maar ook voor kleinere organisaties wanneer de verwerking niet incidenteel is of bijzondere persoonsgegevens omvat. Een club die elk seizoen inschrijvingen verwerkt, doet dat regelmatig. Vraag bij twijfel advies aan een jurist of aan de toezichthouder (de Gegevensbeschermingsautoriteit in België, de Autoriteit Persoonsgegevens in Nederland).",
    },
    {
      q: "Wie is verantwoordelijk als een planningstool de data verwerkt?",
      a: "Jij als club blijft verwerkingsverantwoordelijke; de leverancier van de tool treedt op als verwerker. Tussen beide partijen horen schriftelijke afspraken over die rol, meestal in een verwerkersovereenkomst. Vraag ernaar bij je leverancier voor je de tool in gebruik neemt.",
    },
  ],
  related: ["lesseizoen-plannen-tennis-en-padelclub"],
};
