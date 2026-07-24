# Betaalmethodes en inschrijfwijze op een lessenreeks

**Datum:** 2026-07-24
**Status:** ontwerp, goedgekeurd

## Probleem

Bij het aanmaken van een lessenreeks kan de admin nu niet kiezen:

1. **Welke betaalmethodes** de leerling mag gebruiken. Elke betaling loopt impliciet via
   Mollie (online); er is geen manier om overschrijving/cash toe te laten, en online betalen
   kan aangeboden worden ook al is er geen Mollie-koppeling (dan faalt de inschrijving pas
   laat).
2. **Op welke manier** ingeschreven mag worden. De leerling kiest nu altijd vrij tussen
   solo en groep op de inschrijfpagina, ook als de reeks maar één van beide bedoelt.

De admin wil beide per reeks kunnen instellen, met zinnige defaults, en het moet daarna
ook echt afgedwongen worden bij het inschrijven.

## Uitgangspunt

Elke reeks krijgt twee onafhankelijke keuzeparen — inschrijfwijze (solo / groep) en
betaalmethode (online / handmatig) — als losse booleans. Losse booleans i.p.v. één enum,
omdat de UI twee losse vinkjes zijn en "beide" de default is. De online-betaaloptie is
alleen beschikbaar als de organisatie een Mollie-koppeling heeft. De keuzes worden zowel
in het aanmaak-/bewerkformulier gezet, publiek afgedwongen op de inschrijfpagina, als
hard gevalideerd bij het submitten. De handmatige betaling spiegelt het bestaande
cash-patroon van tenniskampen.

## Beslissingen

| Onderwerp | Keuze |
|---|---|
| Modellering | 4 losse booleans op `LessonSerie`, geen enum |
| Inschrijfwijze default | Solo én groep beide aan |
| Betaalmethode default (formulier) | Online aan als Mollie gekoppeld, anders handmatig aan |
| Online zonder Mollie | Niet aanvinkbaar; validatie weigert `AcceptOnlinePayment=true` zonder `MollieConnection` |
| Minstens één | Minstens één inschrijfwijze én minstens één betaalmethode verplicht |
| Waar wordt betaalwijze gekozen | Door de student op de **bevestigingspagina** (`/confirmation/[token]`), NIET bij het inschrijven. De serie-vlaggen bepalen welke opties daar getoond worden. |
| Handmatige betaling | Camp-stijl: student kiest cash → enrollment `PendingPayment` + `Payment{ Method=Cash, Status=Pending }`; admin markeert betaald → `Confirmed`. **Wijzigt het huidige series-gedrag** (dat cash meteen `Paid`+`Confirmed` zette). |
| `PaymentMode` (Immediate/Deferred) | Blijft ongewijzigd; enkel relevant wanneer online betaald wordt |
| Bewerkbaar | Ja — ook via het bewerken-formulier op de reeksdetailpagina |

> **Correctie t.o.v. eerste ontwerp:** de betaalkeuze staat al in de bestaande
> confirmation-flow (`StudentConfirmationService.ConfirmAsync` / `PickAlternativeAsync`
> verwerken `request.PaymentMethod`, cash vs online). Dit ontwerp bouwt géén nieuwe
> betaalkeuze bij het inschrijven; het (a) beperkt de bestaande keuze tot wat de reeks
> toelaat, en (b) verandert het cash-pad van "meteen betaald" naar "wacht op admin".

## 1. Datamodel & validatie

`LessonSerie` krijgt vier nieuwe booleans:

| Kolom | Type | Default | Betekenis |
|---|---|---|---|
| `AllowSoloEnrollment` | `bool` | `true` | Leerling mag solo inschrijven. |
| `AllowGroupEnrollment` | `bool` | `true` | Leerling mag als groep inschrijven. |
| `AcceptOnlinePayment` | `bool` | `true` | Online betalen via Mollie toegestaan. |
| `AcceptManualPayment` | `bool` | `false` | Handmatig (overschrijving/cash) toegestaan. |

- **Migratie**: kolommen toevoegen met `defaultValue` `true` voor alle vier. Bestaande
  reeksen bieden vandaag zowel cash als online aan op de confirmation-pagina, dus alle vier
  op `true` backfillen houdt hun gedrag identiek (beide inschrijfwijzen, beide
  betaalmethodes). De formulier-default voor nieuwe reeksen (`AcceptManualPayment` uit
  wanneer Mollie gekoppeld is) wordt door de frontend gezet, niet door de kolom-default.
- **EF-configuratie**: in `LessonSerieConfiguration` (`IEntityTypeConfiguration<T>`), geen
  fluent config in `ApplicationDbContext`.

**Validatie** (`CreateLessonSerieRequestValidator` én `UpdateLessonSerieRequestValidator`):

- `AllowSoloEnrollment || AllowGroupEnrollment` — minstens één inschrijfwijze.
- `AcceptOnlinePayment || AcceptManualPayment` — minstens één betaalmethode.
- Als `AcceptOnlinePayment == true`: de organisatie moet een `MollieConnection` hebben.
  Deze check heeft de DB nodig, dus ze gebeurt in de **service** (`LessonSerieService`),
  niet in de FluentValidation-validator — die valideert enkel de vorm. De service geeft
  bij ontbrekende koppeling een `Result.Failure` met een duidelijke, gelokaliseerde
  foutmelding terug (nieuwe `ErrorCodes`-boodschap in `nl.json`/`SharedResources`).

Note: de defaults in de request-DTO's zijn `AllowSolo=true, AllowGroup=true,
AcceptOnline=true, AcceptManual=false`; de frontend overschrijft `AcceptOnline`/
`AcceptManual` op basis van de Mollie-status (zie §3).

## 2. DTO's, mapping & endpoints

- `CreateLessonSerieRequest` en `UpdateLessonSerieRequest` krijgen de vier booleans
  (met bovenstaande defaults).
- `LessonSerieDto` (respons) krijgt de vier velden zodat het bewerkformulier en de
  publieke inschrijfpagina ze kennen.
- `ApplicationMapper` (Mapperly): de vier velden mee mappen in de bestaande
  create/update/naar-DTO methodes.
- Endpoints (`Create`/`Update`/`Get`) blijven thin; de Mollie-koppelingscheck zit in de
  service. Bestaande `.RequireAuthorization()` + `ValidationFilter<T>` blijven.

## 3. Create-/bewerkformulier (frontend)

**Aanmaak-wizard `app/(dashboard)/dashboard/lessons/new/`** — twee nieuwe blokken
onderaan **step 1** (`step-1-basisinfo.tsx`), plus de bijbehorende velden in
`_types.ts` (`Step1Data`) en de submit-mapping naar `CreateLessonSeriesRequest`.

- **Inschrijfwijze** — twee checkboxes "Solo" en "In groep", beide standaard aangevinkt.
  Client-side melding als beide uitgevinkt worden.
- **Betaalmethodes** — twee checkboxes "Online betalen (Mollie)" en "Overschrijving".
  - Mollie-status ophalen via de bestaande `lib/api/mollieConnect.ts` (connected-status).
  - Niet gekoppeld → online-checkbox **gedimd en uitgevinkt**, met uitleg + link
    "Verbind Mollie in instellingen"; "Overschrijving" staat dan standaard aan.
  - Gekoppeld → online standaard aan, overschrijving standaard uit.
  - Client-side melding als beide uitgevinkt worden.
- Alle labels via `messages/nl.json` (`useTranslations`), geen hardcoded strings.

**Bewerkformulier op de reeksdetailpagina** krijgt dezelfde twee blokken, voorgevuld
vanuit `LessonSerieDto`, met dezelfde Mollie-gating.

## 4. Inschrijfwijze afdwingen (publieke inschrijfpagina)

De publieke inschrijfpagina `app/(public)/enroll/[seriesId]/page.tsx` toont **geen**
betaalkeuze — die staat pas op de bevestigingspagina (§5). Hier enkel de solo/groep-gating.

**Frontend:**
- Toon de solo/groep-radio's enkel voor de toegelaten wijzen. Mag er maar één, dan die
  vast (geselecteerd, geen keuze getoond). De default-`useState` (nu hard `"solo"`) volgt
  de eerst-toegelaten wijze; `LessonSeriesDto` levert de twee vlaggen (§2).

**Backend-afdwinging** (`EnrollmentService.SubmitEnrollmentAsync`):
- De service laadt de `LessonSerie` al. Weiger vóór de transactie met een gelokaliseerde
  `Result.Failure` als `request.EnrollmentType == "solo"` terwijl `!AllowSoloEnrollment`,
  of `== "group"` terwijl `!AllowGroupEnrollment`. Geen nieuw request-veld nodig.

## 5. Betaalmethode afdwingen (bevestigingspagina + confirmation-service)

De keuze cash/online bestaat al in `StudentConfirmationService.ConfirmAsync` en
`PickAlternativeAsync` via `request.PaymentMethod` (`1 = Online`, `2 = Cash`) en in de FE
op `app/confirmation/[token]/page.tsx`. Dit ontwerp beperkt die keuze tot de
serie-vlaggen en zet het cash-pad om naar camp-stijl.

**5a. Vlaggen naar de bevestigingspagina.** `AssignmentDetailsDto` krijgt twee velden
`AcceptOnlinePayment` en `AcceptManualPayment`; `BuildDetailsAsync` vult ze uit de
`series`. De confirmation-pagina toont de betaalmethode-tegels enkel voor de toegelaten
opties en zet de default-`useState` op de enige toegelaten optie als er maar één is.

**5b. Server-side afdwinging.** In `ConfirmAsync` én `PickAlternativeAsync` (beide laden
`series`): weiger met een gelokaliseerde `Result.Failure` als de gekozen methode niet is
toegestaan (`Online` terwijl `!AcceptOnlinePayment`, of `Cash` terwijl
`!AcceptManualPayment`). Zo is de gating niet te omzeilen door een handmatige request.

**5c. Cash → camp-stijl (gedragswijziging).** Vandaag zet het cash-pad de enrollment
meteen op `Confirmed` met een `Payment{ Status = Paid }`. Dit wordt:
- `Payment{ Method = Cash, Status = Pending }` (i.p.v. `Paid`, geen `PaidAt`).
- `ConfirmEnrollmentStatuses(assignment, EnrollmentStatus.PendingPayment)` (i.p.v.
  `Confirmed`).
- `TryFinalizeSeriesAsync` wordt in het cash-pad **niet** meer aangeroepen (de reeks is
  pas rond zodra betaald). Dezelfde omzetting geldt in het cash-pad van
  `PickAlternativeAsync`.

**5d. Admin markeert betaald.** Nieuw:
- `IPaymentRepository.GetLatestPendingCashByEnrollmentIdAsync(Guid enrollmentId,
  Guid organizationId, CancellationToken ct)` — analoog aan de bestaande
  `GetLatestPendingCashByCampEnrollmentIdAsync`.
- `IPaymentService.MarkEnrollmentCashPaidAsync(Guid enrollmentId, Guid organizationId,
  CancellationToken ct)` — analoog aan `MarkCampCashPaidAsync`: zet de openstaande
  cash-`Payment` op `Paid` (+`PaidAt`) en de enrollment(s) op `Confirmed`, roept daarna
  `TryFinalizeSeriesAsync` aan (logica hiervoor leeft in `StudentConfirmationService`;
  hergebruik via een bestaande finalize-helper of dupliceer de reeks-finalisatie in de
  payment-laag — kies de laag waar `TryFinalizeSeriesAsync` al bereikbaar is).
- Nieuw `IEndpoint` `MarkEnrollmentCashPaidEndpoint`
  (`POST /enrollments/{enrollmentId:guid}/mark-cash-paid`), `.RequireAuthorization(...)`
  met rol `Admin`/`Trainer`, `ctx.GetOrganizationId()`.
- **Admin-UI**: op de reeksdetailpagina (`app/(dashboard)/dashboard/lessons/[id]/page.tsx`,
  inschrijvingenlijst) een "Markeer als betaald"-actie voor inschrijvingen die
  `PendingPayment` zijn met een openstaande cash-betaling — zelfde interactiepatroon als
  bij kampen. Nieuwe api-call in `lib/api/`.

> **Edge case:** een reeks aangemaakt met `AcceptOnlinePayment=true` waarna de org Mollie
> ontkoppelt. Online blijft dan aangeboden en de Mollie-call faalt (net als vandaag). Dit
> valt buiten scope; de bestaande foutafhandeling in `CreatePaymentForEnrollmentAsync`
> vangt het af.

## 6. E-mail / bevestiging

- **Cash gekozen** (nu `PendingPayment`): de bevestigingsmail vermeldt de betaalinstructies
  (bedrag + "via overschrijving; je plek is definitief zodra de club je betaling bevestigt").
  Bestaande MJML-template uitbreiden met de nodige tokens; `MjmlTemplateRenderer`
  ongewijzigd van vorm.
- **Cash bevestigd door admin** (`MarkEnrollmentCashPaidAsync`): stuur dezelfde
  bevestigingsmail als het online-betaalde pad (plek definitief), analoog aan wat
  `MarkCampCashPaidAsync` doet.
- **Online**: bestaand gedrag (checkout-redirect + webhook-bevestiging).

## 7. Tests

- **Unit** — `CreateLessonSerieRequestValidator`/`UpdateLessonSerieRequestValidator`:
  minstens-één-regels (solo/groep én online/handmatig). `LessonSerieService`: weigert
  `AcceptOnlinePayment` zonder Mollie-koppeling. `EnrollmentService`: weigert een
  `EnrollmentType` die de reeks niet toelaat. `StudentConfirmationService`: weigert een
  betaalmethode die de reeks niet toelaat; cash-pad zet enrollment op `PendingPayment` met
  een `Payment{ Status = Pending }` (niet `Paid`/`Confirmed`). `PaymentService`:
  `MarkEnrollmentCashPaidAsync` zet de cash-`Payment` op `Paid` en de enrollment op
  `Confirmed`.
- **Reset + seed (definitieve E2E-check)**: `seed-data.json` + `seed-demo-data.py`
  bijwerken zodat het contract klopt (de vier nieuwe velden op create, minstens één
  reeks met handmatige betaling en één solo-only / groep-only reeks). Daarna
  `reset-db.sh --no-frontend` + `seed-demo-data.sh` groen.

## Buiten scope

- Concrete Mollie-methode-selectie (Bancontact vs iDEAL apart) — bewust niet: online is
  één toggle die alle op het Mollie-account ingeschakelde methodes toont.
- Wijzigen van de `PaymentMode` (Immediate/Deferred) semantiek.
- Terugbetaling/annulatie van handmatige betalingen buiten de bestaande annuleerflow.
