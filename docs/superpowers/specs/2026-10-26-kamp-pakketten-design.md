# Kamp-pakketten: inschrijven voor dagdelen

Status: geïmplementeerd op `feat/kamp-pakketten`. Datum: 2026-10-26.

## Waarom

Nu schrijf je altijd in voor het hele kamp. Clubs willen voor de jongste kinderen ook halve dagen of enkele
dagen kunnen aanbieden, tegen een eigen prijs. De club stelt daarvoor vaste pakketten samen. De ouder kiest bij
het inschrijven het pakket, en het formulier, de prijs, de betaling en de limiet volgen daaruit.

## Doelen

- Een kampdag heeft één of twee vaste dagdelen met eigen uren (bv. ochtend 9:00-12:00, namiddag 13:00-16:00).
- De club maakt per kamp pakketten: een naam, een prijs en de dagdelen die erin zitten.
- De ouder kiest bij het inschrijven een pakket. Bij een groep (leider plus maximaal 3 leden) kiest elk kind
  een eigen pakket.
- De prijs en de betaling volgen uit de gekozen pakketten. De leider betaalt de som, zoals nu.
- De limiet `MaxParticipants` geldt per dagdeel. Een pakket is vol zodra één van zijn dagdelen vol zit.
- De bestaande extra tarieven ("Leden", "Tweede kind") blijven bestaan, maar worden een vast kortingsbedrag
  op de pakketprijs.
- Een kamp zonder pakketten werkt voor de ouder zoals nu: één prijs voor het hele kamp.

## Niet-doelen

- Geen vrije dagkeuze door de ouder. Alleen pakketten die de club samenstelt.
- Geen groepenverdeling of shiften per trainingsgroep. Dat komt in een latere branch.
- Geen wachtlijst voor volle dagdelen.
- Geen korting in procenten.
- Geen wijziging van het Mollie-gedrag buiten het bedrag.

## Beslissingen van de gebruiker

- Pakketten zijn vast en door de club samengesteld.
- Elk kind in een groep kiest een eigen pakket.
- Dagdelen zijn vast per kampdag, met eigen uren per dag.
- De limiet geldt per dagdeel.
- Bestaande tarieven blijven naast pakketten bestaan als korting op de pakketprijs.
- De korting is een vast bedrag.
- Bestaande tarieven worden automatisch omgerekend naar een vast kortingsbedrag: hoofdprijs min tariefprijs.

## Mijn keuzes (nog niet bevestigd)

1. **Dagdelen:** een nieuwe entiteit `CampDayPart` (CampDay, naam, start, eind, volgorde). `CampDay.StartTime`
   en `EndTime` blijven bestaan voor kampen zonder dagdelen en voor weergave.
2. **Pakketten:** een nieuwe entiteit `CampPackage` (naam, omschrijving, prijs, volgorde) met een koppeltabel
   `CampPackageDayPart`. Een kamp zonder pakketten krijgt geen verplichte pakketten.
3. **Inschrijving:** `CampEnrollment` krijgt een optionele `CampPackageId`. `CampPriceId` blijft, maar betekent
   nu de korting. Een inschrijving zonder pakket telt voor alle dagdelen van het kamp (dat dekt bestaande data).
4. **Prijs:** per deelnemer is dat pakketprijs min kortingsbedrag, nooit onder 0. Zonder pakket is de basis de
   hoofdprijs `Camp.Price`. `CampPricing` blijft de enige plek voor deze berekening.
5. **Wie kiest de korting:** de beheerder wijst die toe, zoals nu. Ouders kiezen alleen het pakket, want een
   ledenkorting is niet te controleren.
6. **Limiet:** bij inschrijven telt het systeem per dagdeel de actieve deelnemers (niet-geannuleerd) en weigert
   met "Dit kamp is volzet" als één van de dagdelen van een gekozen pakket de limiet zou overschrijden. Dit
   gebeurt in dezelfde Serializable-transactie als nu.
   Een kamp zonder dagdelen houdt de limiet als één totaal, zoals nu. Een inschrijving zonder pakket telt mee in
   elk dagdeel.
7. **Omrekening bestaande tarieven:** in de EF-migratie. Kortingsbedrag = `Camp.Price` min `CampPrice.Amount`,
   minimaal 0. Bestaande inschrijvingen houden hun tarief en hun bedrag blijft gelijk.
8. **Publieke pagina:** toont de pakketten met dagen en uren en een keuze per deelnemer. Alleen als het kamp
   pakketten heeft.

## Afwijkingen bij de uitvoering

- **Slots op (datum, dagdeelpositie):** `CampPackageSlot` verwijst niet via een FK naar `CampDayPart`, want een
  kampwijziging vervangt alle dagen en dagdelen. Bij elke wijziging wordt gecontroleerd dat elk slot nog naar een
  bestaand dagdeel wijst, ook als de pakketten niet worden meegestuurd.
- **Niet aangepast:** de e-mailtemplates (bevestiging en betaallink) tonen het gekozen pakket niet, en de
  dashboardkalender telt niet per dagdeel. Beide kunnen later zonder datamodelwijziging.
- **Betaalpagina:** het te betalen bedrag komt nu uit de inschrijving (som van de groep), niet meer uit de
  hoofdprijs. Dat verbetert ook kampen zonder pakketten.

## Raakvlakken die mee moeten

- Kampwizard (stap 2: dagen krijgen dagdelen; nieuwe stap of kaart voor pakketten) en kampbewerking.
- Publiek inschrijfformulier en `SubmitCampEnrollmentRequest` (pakket per deelnemer, ook voor groepsleden).
- `CampEnrollmentService` en `PaymentService`: bedrag, Mollie-omschrijving, gratis-controle, limietcontrole.
- Handmatige inschrijving door de beheerder en het bewerken van een inschrijving (pakket wijzigen).
- Admin-overzicht van inschrijvingen, Excel-export, bevestigingsmails en kalenderweergave (aanwezigen per
  dagdeel).
- Seed-scripts in `backend/Scripts/` en `messages/nl.json`.
- Tests: `CampPricing`, `CampEnrollmentService`, validators, de repository met een echte DbContext, en E2E.

## Open voor de bouwer

- Exacte schermindeling van het pakkettenraster (dagen × dagdelen).
- Naamgeving van velden en DTOs, volgens de bestaande conventies.
- Of `CampDay.StartTime/EndTime` afgeleid kan worden uit de dagdelen.
