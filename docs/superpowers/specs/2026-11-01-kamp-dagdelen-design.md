# Kampen: dagen en dagdelen kiezen

## Aanleiding

Een club (Turnhoutse Tennis & Padel) laat leerlingen per kampdag kiezen: voormiddag (9u15-12u00), namiddag (13u15-16u00) of hele dag. Prijs: 32,50 voor een hele dag, 20 voor een halve dag. Per kamp kan wat mogelijk is per dag verschillen (kerstkamp: donderdag enkel voormiddag). Nu schrijf je in CoachOS in voor het hele kamp tegen één hoofdprijs. Een eerdere poging (branch `feat/kamp-pakketten`, pakketten + kortingen) is bewust weggegooid; dit ontwerp vertrekt opnieuw van wat de club echt doet.

## Doel

- Organisator stelt in de wizard een dagprijs, een halve-dagprijs en een toggle "halve dagen toegestaan" in.
- Deelnemer kiest per dag voormiddag, namiddag of hele dag, of kiest "Ik kom heel het kamp".
- Totaalprijs volgt uit de keuze en wordt bij inschrijving vastgelegd.
- Bestaande kampen en inschrijvingen in productie blijven werken met exact dezelfde bedragen.

## Niet-doelen (nu)

- Extra tarieven (leden, tweede kind, kortingen, weekprijs). Later, eerst de basis.
- Trainers per dagdeel koppelen.
- Groepsverdeling en shiften (aparte latere branch).

## Beslissingen (van de gebruiker)

1. **Prijs.** `Camp.Price` en `CampPrice` (extra tarieven) verdwijnen. Vervangen door `DayPrice` en `HalfDayPrice`.
2. **Halve dagen toggle** per kamp. Uit: enkel hele dagen of heel kamp. Aan: ook voormiddag/namiddag per dag.
3. **Per dag aanpasbaar** in de laatste wizardstap, zoals de per-week-aanpassing bij lessenreeksen: per dag voormiddag aan/uit, namiddag aan/uit, met eigen uren per dagdeel. Dag volledig uit is mogelijk.
4. **"Ik kom heel het kamp"** = alle beschikbare dagen, voorlopig dagen x dagprijs.
5. **Capaciteit**: één maximum per dagdeel, elke dag apart geteld. Een hele dag telt mee in voormiddag en namiddag van die dag.
6. **Trainers** blijven per dag gekoppeld met eigen uren (`CampDayTrainer` ongewijzigd).
7. **Migratie** bewaart bestaande bedragen exact (zie onder).

## Ontwerp

### Datamodel

- `Camp`: `Price` weg. Erbij: `DayPrice`, `HalfDayPrice` (decimal, EUR), `AllowHalfDays` (bool). `MaxParticipants` blijft, betekenis wordt "maximum per dagdeel".
- `CampDay`: `StartTime`/`EndTime` vervangen door `MorningStart`/`MorningEnd` en `AfternoonStart`/`AfternoonEnd` (TimeOnly, nullable: null = dagdeel bestaat niet op die dag). Minstens één dagdeel per dag. Dagen zonder dagdelen worden niet opgeslagen.
- Nieuw `CampEnrollmentDay`: `CampEnrollmentId`, `CampDayId`, `Morning` (bool), `Afternoon` (bool); minstens één waar. `OrganizationId` voor tenantfilter. `DeleteBehavior.Restrict`.
- `CampEnrollment`: `CampPriceId` weg, `TotalAmount` (decimal) erbij: vastgelegd bij inschrijving, zodat latere prijswijzigingen oude inschrijvingen niet raken. `CampPrice` entiteit en tabel verdwijnen.

### Prijsberekening (`CampPricing`)

Per gekozen dag: hele dag (voor- én namiddag) = `DayPrice`, één dagdeel = `HalfDayPrice`. Totaal = som. Een dag met slechts één beschikbaar dagdeel telt voor die ene keuze als halve dag (mijn keuze, past bij het donderdag-voorbeeld van de club). "Heel kamp" kiest op elke dag alles wat beschikbaar is, dus een hele dag tegen `DayPrice`, of een halve dag tegen `HalfDayPrice` op dagen met één dagdeel.

### Validatie

- Kamp: `DayPrice > 0` of gratis (0, geen betaalstap); `HalfDayPrice` verplicht als `AllowHalfDays`; `HalfDayPrice <= DayPrice` (mijn keuze, voorkomt dat twee halve dagen goedkoper zijn dan een hele).
- Inschrijving: gekozen dagdelen moeten op die dag bestaan en, als `AllowHalfDays` uit staat, altijd een hele dag zijn (beide dagdelen). Minstens één dag. Capaciteit per dagdeel gecontroleerd binnen de transactie, zoals bij lessen (dubbele telling bij hele dag). Een vol dagdeel geeft een duidelijke fout met dag en dagdeel.

### Wizard (organisator)

Stappen blijven drie: basis, dagen, controle.
- **Stap 1 (basis):** periode, dagprijs, toggle halve dagen, halve-dagprijs (alleen zichtbaar bij toggle aan), maximum per dagdeel; tarieven-editor verdwijnt.
- **Stap 2 (dagen):** lijst met alle dagen van de periode. Standaard: alle dagen beschikbaar volgens de toggle (voormiddag en namiddag, met standaarduren die je één keer instelt). Per dag aanpasbaar: voormiddag aan/uit + uren, namiddag aan/uit + uren, dag uit. Trainers per dag zoals nu. Zoals de lessenreeks-wizard: eerst standaardwaarden, daarna uitzonderingen per rij.
- **Stap 3 (controle):** overzicht inclusief prijzen.

### Publiek inschrijfformulier

Per beschikbare dag een keuze voormiddag / namiddag / hele dag (alleen opties die bestaan en toegestaan zijn), met uren zichtbaar. Bovenaan een snelkeuze "Ik kom heel het kamp". Live totaalprijs. Volle dagdelen worden uitgeschakeld met label "vol".

### Beheer

- Handmatige inschrijving en bewerken van een inschrijving: zelfde dagkeuze; bedrag herberekend, beheerder kan niet zomaar een ander bedrag kiezen (extra tarieven komen later).
- Inschrijvingsdetail toont gekozen dagen/dagdelen.
- Export (Excel) krijgt per deelnemer de gekozen dagdelen, plus een overzicht per dag en dagdeel (aantal deelnemers). Betaalbedrag uit `TotalAmount`.
- Dashboard "kampdagen" (`GetMyCampDays`, `GetOrganizationCampDays`) toont per dag de dagdelen en trainers.
- Betaling (Mollie, cash) gebruikt `TotalAmount` in plaats van `camp.Price`.

### Groepsinschrijving

Mijn keuze, past bij het bestaande model: een groep (leider + leden) maakt één dagkeuze die voor iedereen geldt; totaal = per persoon. Per-lid keuzes zijn een mogelijke uitbreiding, niet nu.

### Migratie (productiedata blijft kloppen)

- Bestaand kamp: `AllowHalfDays = false`; `DayPrice = oude Price / aantal dagen` (enkel voor weergave); elke bestaande dag krijgt de oude `StartTime`/`EndTime` als enig dagdeel (voormiddag, namiddag leeg; mijn keuze, de oude uren bevatten geen middaginfo). `MaxParticipants` blijft als maximum per dagdeel.
- Bestaande inschrijving: `TotalAmount` = tot nu toe geldend bedrag (gekozen tarief of hoofdprijs); een `CampEnrollmentDay`-rij per kampdag met dat ene dagdeel (= "heel kamp"); het opgeslagen bedrag blijft leidend, niet een herberekening.
- `CampPrice` vervalt pas nadat `TotalAmount` is gevuld. Migratie en teruggave getest op een kopie met bestaande data.

## Aan de bouwer overgelaten

Exacte UI-vormgeving van de dag-rijen, standaarduren in stap 2, de naam van foutmeldingen en i18n-keys (NL in `messages/nl.json`), en de precieze migratiestappen. Seed-scripts, testgids (`docs/testing/kampen/TESTGIDS.md`) en de reset + seed E2E-check horen bij het werk.
