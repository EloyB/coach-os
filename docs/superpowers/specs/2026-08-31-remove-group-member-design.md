# Lid uit een groep halen — Design

**Datum:** 2026-08-31
**Branch:** `feat/remove-group-member` (gestackt op `feat/manual-enrollment-management`, PR #204)
**Status:** ontwerp goedgekeurd, klaar voor implementatieplan

## Probleem

Een admin/hoofdtrainer kan vandaag een groepslid enkel **annuleren** (cancelt de inschrijving)
of de **hele groep** annuleren/ontbinden. Er is geen manier om één lid uit een groep te halen
terwijl de rest samen blijft. Tijdens het plannen wil je een deelnemer soms losmaken van z'n
groep en als losse (solo) inschrijving behandelen — zonder z'n inschrijving te annuleren.

## Bestaand model (relevant)

- Een groep wordt ingepland via **één** `ScheduleAssignment` met `EnrollmentGroupId = groupId`
  (geen aparte toewijzing per lid); de bezetting van een slot telt de groep als
  `group.Members.Count`.
- `EnrollmentGroup` heeft `LeaderEnrollmentId` (de leider draagt de gedeelde betaling).
- `AssignmentService.DissolveGroupAsync(seriesId, groupId, orgId)` bestaat: zet elk lid op
  `EnrollmentGroupId = null`, verwijdert de groeps-`ScheduleAssignment`(s), en verwijdert de
  groep-rij. Dit is de "detach"-bouwsteen, maar enkel voor de hele groep.
- Betaling (cash of Mollie) zet de hele groep op `Confirmed`; `PendingPayment` = openstaande
  betaling.
- Groepsleden zijn `Enrollment`-entiteiten met `Status` en `EnrolledAt`.

## Beslissingen

1. **Leider verwijderen** → automatisch een ander lid promoveren tot leider. Regel: het
   **vroegst-ingeschreven** overblijvende lid (`EnrolledAt`, tie-break op `StudentName`).
   `EnrollmentGroup.LeaderEnrollmentId` wordt herzet.
2. **Groep zakt naar 1 lid** → de groep ontbindt; het laatste lid wordt solo
   (`EnrollmentGroupId = null`) en de groep-rij wordt verwijderd.
3. **Betaald/bevestigd** → **blokkeren**. Als de groep (i.e. het betrokken lid — de groep deelt
   de status) `Confirmed` of `PendingPayment` is, faalt de actie met `Conflict`. `Cancelled`
   leden komen niet in aanmerking.
4. **≥2 leden blijven** → de groeps-`ScheduleAssignment` blijft **ongemoeid**; het verwijderde
   lid valt er automatisch uit (bezetting = `Members.Count`) en wordt een oningeplande solo.
   Er wordt niets aan de groeps-toewijzing gewijzigd.
5. **Bij ontbinden (→1)** → de groeps-`ScheduleAssignment` wordt **omgezet** naar een
   individuele toewijzing voor het overblijvende lid (`EnrollmentGroupId = null`,
   `EnrollmentId = <overblijvend lid>`), zodat die z'n ingeplande slot behoudt. Enkel het
   verwijderde lid raakt oningepland. (Veilig want stap 3 blokkeert betaalde/bevestigde
   groepen, dus er zijn in deze fase geen bevestigings-tokens aan de toewijzing gekoppeld.)

## Architectuur

### Backend

**Service — `AssignmentService.RemoveMemberFromGroupAsync(seriesId, groupId, enrollmentId, organizationId, ct)`**
(naast `DissolveGroupAsync`, hergebruikt dezelfde repos: `enrollmentGroupRepo`,
`scheduleAssignmentRepo`).

1. Laad de groep met leden (`enrollmentGroupRepo.GetByIdAsync`); valideer bestaat +
   `LessonSerieId == seriesId`. Zoek het te verwijderen lid in `group.Members`; niet gevonden →
   `NotFound`.
2. **Gate:** is het lid `Confirmed`/`PendingPayment` → `Conflict`
   ("Dit lid kan niet uit de groep gehaald worden: de groep is al betaald of bevestigd.").
3. Detach het lid: `member.EnrollmentGroupId = null`.
4. Bereken de overblijvende leden (`group.Members` minus het lid).
   - **0 of 1 over → ontbinden (stap 2/5):** het overblijvende lid (indien 1) op
     `EnrollmentGroupId = null`; de groeps-toewijzing(en) **omzetten** naar een individuele
     toewijzing voor dat lid (`EnrollmentGroupId = null`, `EnrollmentId = remaining.Id`) i.p.v.
     verwijderen; de groep-rij verwijderen. (Bij 0 over — enkel het lid zelf — geen toewijzing
     om te bewaren; verwijder de groeps-toewijzing en de groep.)
   - **≥2 over (stap 4):** groeps-toewijzing ongemoeid laten. Als het verwijderde lid de leider
     was: `group.LeaderEnrollmentId = <vroegst-ingeschreven overblijvend lid>` (stap 1).
5. Eén `SaveChangesAsync` (alles-of-niets).

**Endpoint** — `DELETE /lessonseries/{id:guid}/enrollment-groups/{groupId:guid}/members/{enrollmentId:guid}`,
routeert naar de service. Autorisatie: **dezelfde admin + hoofdtrainer-scope** als de
cancel-endpoints op deze branch (`HeadTrainerAccess`/policy zoals `CancelEnrollmentGroupEndpoint`).
Retourneert `204 No Content` bij succes.

### Frontend

- **API-client** (`lib/api/enrollments.ts`): `removeGroupMember(seriesId, groupId, enrollmentId)` →
  `DELETE .../enrollment-groups/{groupId}/members/{enrollmentId}`.
- **`PersonRow`** (in `enrollments-table.tsx`): nieuwe actie **"Uit groep halen"** in het
  acties-menu, enkel getoond wanneer de rij een groepslid is (`enrollment.enrollmentGroupId != null`),
  naast "Aanpassen"/"Annuleren". (Zichtbaar voor wie ook de andere schrijfacties ziet;
  hoofdtrainer volgens de bestaande gating.)
- **Bevestigingsdialog** vóór de actie: "{naam} wordt uit de groep gehaald en wordt een losse
  inschrijving." Knoppen "Annuleren" / "Uit groep halen".
- Bij succes: `["enrollments", seriesId]` en `["planning", seriesId]` / `["lessonSeries", seriesId]`
  invalideren; toast.
- Alle nieuwe teksten via `next-intl` in `messages/nl.json` (`enrollmentsTable`-namespace).

## Data flow

```
Planner opent acties-menu van een groepslid → "Uit groep halen" → bevestiging
  → DELETE /lessonseries/{id}/enrollment-groups/{groupId}/members/{enrollmentId}
  → RemoveMemberFromGroupAsync:
       gate (Confirmed/PendingPayment → 409)
       detach lid (EnrollmentGroupId = null)
       ≥2 over: groeps-toewijzing blijft; leider? → promoveer vroegst-ingeschreven lid
       ≤1 over: ontbind → overblijvend lid solo, groeps-toewijzing omgezet naar individueel, groep weg
       SaveChanges
  → FE invalideert enrollments + planning
```

## Testplan

**Unit (NUnit/Moq/FluentAssertions)** — `AssignmentService`:
- Groep van 3, gewoon lid verwijderen → lid `EnrollmentGroupId = null`; groep + leider onveranderd;
  groeps-toewijzing ongemoeid.
- Groep van 3, **leider** verwijderen → `LeaderEnrollmentId` = vroegst-ingeschreven overblijvend lid.
- Groep van 2, lid verwijderen → groep ontbonden (verwijderd); overblijvend lid solo; groeps-toewijzing
  **omgezet** naar individuele toewijzing voor dat lid (behoudt slot).
- `Confirmed`/`PendingPayment` lid → `Conflict`, niets gemuteerd.
- Lid dat niet in de groep zit / onbekende groep → `NotFound`.

**Reset + seed** als definitieve E2E-check (contractwijziging = nieuw endpoint; seed-scripts nalopen —
vermoedelijk geen aanpassing nodig).

## Buiten scope / follow-up

- **Lid toevoegen aan een bestaande groep** — expliciet de **volgende feature**.
- Nieuwe groepen samenstellen uit losse inschrijvingen.
- Betaalde/bevestigde groepen wijzigen (geblokkeerd).
- Herberekening/refund van betalingen.
