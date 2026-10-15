# Lid manueel toevoegen aan een bestaande groep — Design

**Datum:** 2026-09-03
**Branch:** `feat/remove-group-member` (groep-management; gestackt op `feat/manual-enrollment-management`, PR #204)
**Status:** ontwerp goedgekeurd, klaar voor implementatieplan

## Probleem

Admin/hoofdtrainer kan een deelnemer manueel inschrijven als **solo** (`CreateManualEnrollmentAsync`),
maar er is geen manier om iemand handmatig aan een **bestaande groep** toe te voegen. Tijdens het
plannen wil je soms een extra speler in een bestaande groep zetten zonder dat die via de publieke
inschrijfflow gaat.

## Bestaand model (relevant)

- `CreateManualEnrollmentAsync`: maakt een solo-inschrijving, status **Confirmed**, geen betaalflow,
  met bevestigingsmail via de outbox; valideert leeftijd (`CheckAgeEligibility`), formulier
  (`FormResponseValidator`), duplicaat (`IsDuplicateParticipantAsync`) en capaciteit
  (`CountActiveBySeriesAsync` vs `MaxRegistrations`), in een serializable transactie.
- Groepslid = `Enrollment` met `EnrollmentGroupId = group.Id`. De groep heeft `LeaderEnrollmentId`
  en de leden delen de `SelectedPriceOptionId`.
- Betaling zet de hele groep op `Confirmed`; `PendingPayment` = openstaande betaling.
- `CreateManualEnrollmentRequest`: `StudentName`, `ContactEmail`, `StudentEmail?`, `StudentPhone?`,
  `DateOfBirth`, `Responses`.
- `AssignmentService.RemoveMemberFromGroupAsync` (net toegevoegd) toont het patroon voor
  groep-mutaties met de betaald/bevestigd-gate.

## Beslissingen

1. **Status wordt geërfd:** het nieuwe lid krijgt de status van de groep (de status van de leider;
   gezien de gate hieronder is dat `Pending`, evt. `Waitlisted`) en de **gedeelde prijsoptie**
   (`SelectedPriceOptionId` van de leider). Het lid wordt met de groep mee bevestigd/betaald in de
   normale flow.
2. **Gate:** toevoegen is **geblokkeerd** (`Conflict`) als de groep al `Confirmed`/`PendingPayment`
   is (symmetrisch met "uit groep halen"). Annuleren/betalen van een betaalde groep valt buiten
   deze feature.
3. **Validaties hergebruiken:** leeftijd, formulier-antwoorden, duplicaat-deelnemer en capaciteit —
   identiek aan de manuele solo.
4. **Categorie** uit geboortedatum (`ResolveCategory`). Nieuw lid is **nooit** de leider; de
   `LeaderEnrollmentId` van de groep blijft ongewijzigd.
5. **Geen bevestigingsmail** bij toevoegen — het lid is nog niet bevestigd; de bevestigingsmail
   volgt via de normale groeps-bevestiging. (De manuele solo stuurt er wél één omdat die meteen
   `Confirmed` is; dat verschil is bewust.)

## Architectuur

### Backend

**Service — `EnrollmentService.AddGroupMemberAsync(lessonSeriesId, groupId, request, organizationId, ct)`**
(`request` = `CreateManualEnrollmentRequest`; retourneert `Result<Guid>` met de nieuwe enrollment-id).

1. Laad de reeks (`GetByIdPublicAsync`) + org-check; niet gevonden → `NotFound`.
2. Laad de groep (`enrollmentGroupRepo.GetByIdAsync(groupId, organizationId)`); valideer bestaat +
   `LessonSerieId == lessonSeriesId`; niet gevonden → `NotFound`. De leider (via
   `LeaderEnrollmentId` in `group.Members`) levert status + prijsoptie.
3. **Gate:** is de leider `Confirmed`/`PendingPayment` → `Conflict`
   ("Je kan geen lid toevoegen aan een groep die al betaald of bevestigd is.").
4. Valideer geboortedatum, leeftijd, formulier (zoals `CreateManualEnrollmentAsync`).
5. In een serializable transactie: capaciteits-check (`MaxRegistrations`), duplicaat-check, dan
   `Enrollment` aanmaken met:
   - `EnrollmentGroupId = groupId`
   - `Status = leader.Status` (geërfd)
   - `SelectedPriceOptionId = leader.SelectedPriceOptionId` (geërfd)
   - `Category` uit DOB, `IsOpenToGrouping = false`, contact/telefoon zoals de manuele solo.
   - Formulier-antwoorden opslaan.
   - **Geen** outbox-bevestigingsmail.
   - Commit; return de nieuwe id.

**Interface:** `IEnrollmentService.AddGroupMemberAsync(...)`.

**Endpoint:** `POST /lessonseries/{id:guid}/enrollment-groups/{groupId:guid}/members`, body =
`CreateManualEnrollmentRequest`, `AddEndpointFilter<ValidationFilter<CreateManualEnrollmentRequest>>()`.
Autorisatie: admin + hoofdtrainer, zoals de andere groep-endpoints op deze branch
(`HeadTrainerAccess.EnsureSerieAccessAsync` + `EnsureManualEnrollmentAllowed`). Retourneert `201`.

### Frontend

- **API-client** (`lib/api/enrollments.ts`): `addGroupMember(seriesId, groupId, request)` →
  `POST .../enrollment-groups/{groupId}/members`. Body-type = het bestaande manuele-inschrijving
  request-type.
- **`GroupBlockRows`**: een **"lid toevoegen"**-actie (in het groep-acties-menu of als knop),
  zichtbaar voor beheerders (`canManage`) en **verborgen bij een betaalde/bevestigde groep**
  (leiderstatus `Confirmed`/`PendingPayment`). Opent een dialog met dezelfde velden als de manuele
  inschrijving (naam/e-mail/telefoon/geboortedatum) — géén prijsoptie-veld (wordt geërfd).
- De dialog kan de bestaande `ManualEnrollmentDialog` hergebruiken/uitbreiden met een optionele
  `groupId` (bij aanwezig → `addGroupMember`, titel "Lid toevoegen aan groep"), of een aparte kleine
  dialog. Implementatiekeuze in het plan.
- Bij succes: `["enrollments", seriesId]`, `["planning", seriesId]`, `["lessonSeries", seriesId]`
  invalideren + toast. Alle nieuwe teksten via `next-intl` in `messages/nl.json`.

## Data flow

```
Beheerder klikt "lid toevoegen" op een groep → dialog (naam/e-mail/telefoon/dob)
  → POST /lessonseries/{id}/enrollment-groups/{groupId}/members
  → AddGroupMemberAsync:
       gate (leider Confirmed/PendingPayment → 409)
       valideer leeftijd/formulier
       [transactie] capaciteit + duplicaat → Enrollment (EnrollmentGroupId, status+optie geërfd)
       commit (geen mail)
  → FE invalideert enrollments + planning
```

## Testplan

**Unit (NUnit/Moq/FluentAssertions)** — `EnrollmentService`:
- Toevoegen aan een `Pending`-groep → nieuw lid met `EnrollmentGroupId` = groep, `Status = Pending`,
  `SelectedPriceOptionId` = die van de leider.
- Groep `Confirmed`/`PendingPayment` → `Conflict`, niets aangemaakt.
- Duplicaat-deelnemer → `Conflict`; reeks volzet → `Conflict`; ongeldige/te-jonge geboortedatum →
  `Validation`.
- Groep niet gevonden / hoort niet bij de reeks → `NotFound`.
- Geen outbox-mail aangemaakt (Verify Times.Never op de mail-outbox voor deze flow).

**Reset + seed** als definitieve E2E-check (nieuw endpoint; seed-scripts nalopen — vermoedelijk
geen aanpassing).

## Buiten scope / follow-up

- Nieuwe groep samenstellen uit losse inschrijvingen.
- Lid toevoegen aan een betaalde/bevestigde groep (geblokkeerd; betaalafhandeling buiten scope).
- Bevestigingsmail bij toevoegen (volgt via de normale groeps-bevestiging).
