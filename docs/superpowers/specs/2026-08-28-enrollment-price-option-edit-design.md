# Prijsoptie aanpassen in de inschrijf-dialog — Design

**Datum:** 2026-08-28
**Status:** ontwerp goedgekeurd, klaar voor implementatieplan

## Probleem

Bij het inschrijven kiest de contactpersoon één prijsoptie (tarief) voor zichzelf of
de groep. Die keuze hangt vaak samen met de groepsgrootte (bv. "Groep van 3"). Tijdens
het plannen kan de samenstelling wijzigen — iemand die voor 3 inschreef wordt bij een
4de persoon gezet — waardoor de gekozen prijsoptie niet meer klopt. De planner moet de
prijsoptie daarom achteraf kunnen corrigeren.

Automatische prijsoptie-toewijzing (op basis van ingevulde gegevens + geplande tijdslot)
komt in een **later stadium** en valt buiten deze feature.

## Bestaand model (geen schemawijziging nodig)

- `Enrollment.SelectedPriceOptionId` (`Guid?`) bestaat al — de door de speler gekozen
  prijsoptie. Bij een groepsinschrijving krijgt elk lid dezelfde optie toegewezen.
- `LessonSerie.Prices` (`ICollection<LessonSeriePrice>`) — de benoemde prijsopties per
  reeks (label, beschrijving, bedrag per deelnemer). Zonder opties valt de reeks terug
  op het legacy veld `LessonSerie.Price`.
- `PricingService.CalculateForGroupAsync` sommeert per gekozen optie × aantal deelnemers.
  Het bedrag wordt on-the-fly berekend bij betaling/bevestiging; een `Payment`-rij legt
  het bedrag als snapshot vast op het moment van aanmaken.
- Betaling zet de status op `Confirmed` (zowel cash via `MarkEnrollmentCashPaidAsync` als
  de Mollie-webhook, telkens voor de hele groep). `PendingPayment` = openstaande betaling.

## Beslissingen

1. **Reikwijdte bij groepen:** de wijziging geldt voor **alle leden** van de groep. Dit
   houdt de groep consistent (de prijs sommeert per lid) en past bij het scenario.
2. **Gate op betaling:** de prijsoptie is aanpasbaar zolang de inschrijving `Pending` is.
   Bij `Confirmed`, `PendingPayment` of `Cancelled` is ze **read-only** (vergrendeld); de
   dialog toont de huidige optie met uitleg waarom aanpassen niet meer kan. De backend
   dwingt dit af — client-side verbergen is geen grens.
3. **Alleen tonen** wanneer de reeks prijsopties heeft (`Prices.Count > 0`). Heeft de reeks
   geen opties (legacy vaste prijs), dan verschijnt er geen selector.
4. **Geen automatische herberekening/refund.** Enkel `SelectedPriceOptionId` wordt gezet.
   Een reeds aangemaakte `Payment` blijft ongemoeid (en is via de gate sowieso uitgesloten).
5. **Opslag:** meegestuurd met de bestaande basis-update (één "Opslaan"). Geen apart endpoint.
6. **Autorisatie:** ongewijzigd — `UpdateBasicEnrollmentEndpoint` is al **Admin-only**.
   Planners zijn admins; hoofdtrainers blijven read-only.

## Architectuur

### Backend

**DTO's**
- `LessonSerieEnrollmentDto`: veld `SelectedPriceOptionId` (`Guid?`) toevoegen zodat de
  dialog de huidige keuze kan voorselecteren. Mapper (`ApplicationMapper`) bijwerken.
- `UpdateBasicEnrollmentRequest`: veld `SelectedPriceOptionId` (`Guid?`) toevoegen.

**Service — `EnrollmentService.UpdateBasicEnrollmentAsync`**

Bovenop de bestaande basis-updates (naam/e-mail/telefoon/geboortedatum/isOpenToGrouping):

1. Wanneer `SelectedPriceOptionId` in de request wijzigt t.o.v. de huidige waarde:
   - **Gate:** faal met `Conflict` als de inschrijving (of, bij een groep, de bewerkte
     inschrijving — die deelt de status van de groep) `Confirmed` of `PendingPayment` is.
     Melding: "De prijsoptie kan niet meer aangepast worden: deze inschrijving is al
     betaald of bevestigd." `Cancelled` is sowieso niet bewerkbaar in de dialog.
   - **Validatie:** de optie moet bij deze reeks horen (`LessonSeriePrice` met dat Id op
     de `LessonSerieId` van de inschrijving). Anders `Validation`-fout.
   - **Propagatie:** zit de inschrijving in een groep, zet dan `SelectedPriceOptionId` op
     alle leden van de groep; anders enkel op deze inschrijving.
2. Wijzigt de optie niet, dan verandert er niets aan het prijsgedrag (idempotent).

**Repository**
- Een manier nodig om alle inschrijvingen van een groep te laden/muteren. Hergebruik
  bestaande group-aware laadmethode (`GetByIdWithGroupAsync`) of een gerichte
  `GetByGroupAsync`; bepaald in het implementatieplan.
- Optie-validatie via `ILessonSeriePriceRepository` (bestaat al).

**Validator**
- `UpdateBasicEnrollmentRequestValidator`: geen strikte regel nodig op `SelectedPriceOptionId`
  (nullable Guid); de business-validatie (hoort bij de reeks) zit in de service.

### Frontend — `EditEnrollmentDialog` (in `enrollments-table.tsx`)

- Prijsopties ophalen via het bestaande admin-endpoint `GET /lessonseries/{id}/prices`
  (nieuwe `getLessonSeriesPrices`-helper in `lib/api/` indien nog niet aanwezig).
- **Dropdown** met de prijsopties, voorgeselecteerd op `enrollment.selectedPriceOptionId`.
  Enkel renderen als er ≥1 optie is.
- **Groep-hint:** bij een groepsinschrijving een korte tekst "geldt voor de hele groep".
- **Vergrendeld:** is de inschrijving `Confirmed`/`PendingPayment`/`Cancelled`, toon de
  huidige optie read-only met uitleg (geen dropdown).
- Bij "Opslaan" wordt `selectedPriceOptionId` meegestuurd in de bestaande
  `updateBasicEnrollment`-call. Bij succes de queries invalideren zoals nu.
- Alle nieuwe teksten via `next-intl` in `messages/nl.json` (geen hardcoded strings).

## Data flow

```
Planner opent aanpas-dialog (PersonRow → EditEnrollmentDialog)
  → dialog haalt reeks-prijsopties op (GET /lessonseries/{id}/prices)
  → dropdown preselect op enrollment.selectedPriceOptionId
  → (indien betaald/bevestigd: read-only + uitleg)
Planner kiest andere optie + Opslaan
  → PUT /lessonseries/{id}/enrollments/{enrollmentId}  (incl. selectedPriceOptionId)
  → UpdateBasicEnrollmentAsync:
       gate (Confirmed/PendingPayment → 409)
       valideer optie hoort bij reeks (→ 400)
       propageer naar alle groepsleden (of enkel deze bij solo)
       SaveChanges
  → FE invalideert ["enrollments", seriesId] + ["planning"/"lessonSeries"]
```

## Testplan

**Unit (NUnit/Moq/FluentAssertions)** — `EnrollmentService`:
- Solo Pending: optie wijzigen → opgeslagen op die inschrijving.
- Groep Pending: optie wijzigen op één lid → toegepast op **alle** leden.
- `Confirmed` → wijziging geblokkeerd met `Conflict`, niets gemuteerd.
- `PendingPayment` → geblokkeerd met `Conflict`.
- Optie die niet bij de reeks hoort → `Validation`.
- Optie ongewijzigd → geen neveneffect (idempotent), basis-velden nog steeds bijgewerkt.

**Reset + seed** als definitieve E2E-check (contractwijziging op een publiek endpoint →
seed-scripts nalopen; de seed zet geen prijsoptie via edit, dus wellicht geen aanpassing
nodig — verifiëren).

## Buiten scope

- Automatische prijsoptie-toewijzing op basis van gegevens/tijdslot (later).
- Herberekening/aanmaak/refund van betalingen bij een optie-wijziging.
- Prijsopties aanpassen voor reeksen zonder prijsopties (legacy vaste prijs).
