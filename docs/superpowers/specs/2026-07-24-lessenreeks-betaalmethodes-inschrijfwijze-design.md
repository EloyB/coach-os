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
| Betaalmethode default | Online aan als Mollie gekoppeld, anders handmatig aan |
| Online zonder Mollie | Niet aanvinkbaar; validatie weigert `AcceptOnlinePayment=true` zonder `MollieConnection` |
| Minstens één | Minstens één inschrijfwijze én minstens één betaalmethode verplicht |
| Handmatige betaling | Spiegelt camp-cashflow: enrollment `PendingPayment` + `Payment{ Method=Cash }`, admin markeert betaald |
| `PaymentMode` (Immediate/Deferred) | Blijft ongewijzigd; enkel relevant wanneer online betaald wordt |
| Bewerkbaar | Ja — ook via het bewerken-formulier op de reeksdetailpagina |

## 1. Datamodel & validatie

`LessonSerie` krijgt vier nieuwe booleans:

| Kolom | Type | Default | Betekenis |
|---|---|---|---|
| `AllowSoloEnrollment` | `bool` | `true` | Leerling mag solo inschrijven. |
| `AllowGroupEnrollment` | `bool` | `true` | Leerling mag als groep inschrijven. |
| `AcceptOnlinePayment` | `bool` | `true` | Online betalen via Mollie toegestaan. |
| `AcceptManualPayment` | `bool` | `false` | Handmatig (overschrijving/cash) toegestaan. |

- **Migratie**: kolommen toevoegen met `defaultValue` `true`/`true`/`true`/`false` zodat
  bestaande reeksen ongewijzigd gedrag houden ("beide inschrijfwijzen + online betalen").
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

## 4. Inschrijfpagina afdwingen (frontend + backend)

**Publieke inschrijfpagina `app/(public)/enroll/[seriesId]/page.tsx`:**

- **Inschrijfwijze**: toon de solo/groep-radio's enkel voor de toegelaten wijzen. Mag er
  maar één, dan die vast (geselecteerd, geen keuze getoond). De default-`useState` volgt
  de eerst-toegelaten wijze i.p.v. hard `"solo"`.
- **Betaalwijze**: toon de keuze online/overschrijving op basis van de serie-vlaggen.
  - Enkel online → huidig gedrag (Mollie checkout / betaallink volgens `PaymentMode`).
  - Enkel handmatig → geen Mollie; "je ontvangt betaalinstructies voor overschrijving".
  - Beide → leerling kiest; keuze gaat mee in de submit.

**Submit-validatie** (`SubmitEnrollmentRequest` + `SubmitEnrollmentRequestValidator` +
`EnrollmentService`):

- Request krijgt een `PaymentChoice` veld (`"online"` | `"manual"`).
- Service weigert (`Result.Failure`, gelokaliseerd):
  - een `EnrollmentType` die de serie niet toelaat;
  - een `PaymentChoice` die de serie niet toelaat.
- De vorm-validatie (enum-waarden aanwezig) zit in de validator; de serie-afhankelijke
  checks in de service (die de `LessonSerie` al laadt).

## 5. Handmatige betaling voor lessenreeksen (backend)

Spiegelt het bestaande camp-patroon (`RecordCampCashPaymentAsync` /
`MarkCampCashPaidAsync`).

- **Bij inschrijving met `PaymentChoice="manual"`**: `EnrollmentService` maakt de
  enrollment aan met status `PendingPayment` en registreert een
  `Payment{ Method = PaymentMethod.Cash, Status = PaymentStatus.Pending }` voor het
  berekende bedrag (via de bestaande `PricingService`-breakdown). Geen Mollie-call.
- **Nieuwe service-methode** `RecordEnrollmentCashPaymentAsync` op `IPaymentService`,
  dezelfde laag als de camp-variant (`RecordCampCashPaymentAsync`). `EnrollmentService`
  roept ze aan bij `PaymentChoice="manual"`.
- **Admin markeert betaald**: nieuwe `MarkEnrollmentCashPaidAsync(enrollmentId,
  organizationId)` (analoog aan `MarkCampCashPaidAsync`): zet `Payment.Status = Paid` en
  `Enrollment.Status = Confirmed`. Nieuw `IEndpoint` onder de bestaande
  enrollments/payments-endpoints, `.RequireAuthorization()`, filtert op `organizationId`.
- **Repository**: methode om de laatste openstaande cash-`Payment` per `EnrollmentId` te
  vinden (analoog aan `GetLatestPendingCashByCampEnrollmentIdAsync`).
- **Frontend admin**: op de inschrijvingenlijst van een reeks een "Markeer als betaald"-
  actie voor `PendingPayment`-inschrijvingen met een cash-betaling. (Minimale UI; zelfde
  interactiepatroon als bij kampen.)

## 6. E-mail / bevestiging

- **Enkel handmatig / gekozen overschrijving**: de bevestigingsmail vermeldt de
  betaalinstructies (bedrag + "via overschrijving; je plek is bevestigd zodra de betaling
  is verwerkt"). Tokens toevoegen aan de betrokken MJML-template; `MjmlTemplateRenderer`
  ongewijzigd van vorm.
- **Online**: bestaand gedrag (checkout-redirect of betaallink volgens `PaymentMode`).

## 7. Tests

- **Unit** — `CreateLessonSerieRequestValidator`/`UpdateLessonSerieRequestValidator`:
  minstens-één-regels. `LessonSerieService`: weigert `AcceptOnlinePayment` zonder
  Mollie-koppeling. `EnrollmentService`: weigert niet-toegelaten `EnrollmentType` en
  `PaymentChoice`; maakt bij `manual` een pending cash-`Payment`. `PaymentService`:
  `MarkEnrollmentCashPaidAsync` zet enrollment op `Confirmed`.
- **Reset + seed (definitieve E2E-check)**: `seed-data.json` + `seed-demo-data.py`
  bijwerken zodat het contract klopt (de vier nieuwe velden op create, minstens één
  reeks met handmatige betaling en één solo-only / groep-only reeks). Daarna
  `reset-db.sh --no-frontend` + `seed-demo-data.sh` groen.

## Buiten scope

- Concrete Mollie-methode-selectie (Bancontact vs iDEAL apart) — bewust niet: online is
  één toggle die alle op het Mollie-account ingeschakelde methodes toont.
- Wijzigen van de `PaymentMode` (Immediate/Deferred) semantiek.
- Terugbetaling/annulatie van handmatige betalingen buiten de bestaande annuleerflow.
