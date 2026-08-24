# Hoofdtrainer-rol (read-only inschrijvingen + planning)

**Datum:** 2026-08-24
**Branch:** `feat/hoofdtrainer-role`
**Scope:** backend (migratie, autorisatie, JWT) + frontend (toggle, read-only gating).

## Doel

De admin kan met één klik een trainer tot **hoofdtrainer** maken. Een hoofdtrainer behoudt
alle trainer-functies en krijgt daarbovenop **read-only** toegang tot de **inschrijvingen** en de
**planning** (raadplegen). Geen schrijfrechten en geen toegang tot ander clubbeheer.

## Beslissingen (uit brainstorm)

1. **Rechten-omvang:** enkel inschrijvingen + planning, **read-only**. Alle write-acties en overig
   clubbeheer (org-instellingen, Mollie, trainers beheren) blijven admin-only.
2. **Datamodel:** een vlag `IsHeadTrainer` op `OrganizationMembership` (per club). Rol blijft `Trainer`.
3. **Timing:** de status zit in de JWT/sessie → gaat in **bij de volgende login** van de trainer.

## Backend

- **Domain:** `bool IsHeadTrainer` (default false) op `OrganizationMembership`. EF-migratie
  `AddIsHeadTrainerToMembership` (auto-migrate bij startup).
- **JWT:** voeg claim `isHeadTrainer` toe bij het bouwen van het token (login, org-switch,
  accept-invite) op basis van de actieve membership. Voeg het ook toe aan het user-object in de
  auth-response zodat de frontend het kent.
- **Autorisatie-policy** `EnrollmentsPlanningRead` = `User.IsInRole("Admin") || claim isHeadTrainer == "true"`.
  Toepassen op de **GET**-endpoints die nodig zijn om inschrijvingen + planning te bekijken:
  - Inschrijvingen: `GetEnrollments`, `GetEnrollmentsWithPreferences`.
  - Planning: `GetPlanning`, `GetNonResponders`, `ExportPlanning`.
  - Reeks/lessen die de detail- en planningpagina nodig heeft om te laden (GET lessonserie(s)),
    voor zover die nu admin-only zijn — te bepalen tijdens implementatie.
  - **Alle write-endpoints blijven `RequireRole("Admin")`.**
- **Toggle-endpoint** (admin-only): `PUT /trainers/{trainerId}/head-trainer` met body `{ isHeadTrainer }`.
  Zet de vlag op de membership (`UserId == trainerId`, `OrganizationId == org`, rol `Trainer`).
- **DTO:** `TrainerDto.IsHeadTrainer` mee-mappen.

## Frontend

- **Auth:** `AuthUser.isHeadTrainer` (uit de auth-response); helper `isHeadTrainerViewer()` =
  rol `Trainer` én `isHeadTrainer` (dus geen admin).
- **Trainers-pagina:** per trainer een toggle/knop **"Hoofdtrainer"** + badge (admin-only pagina).
  Roept de toggle-endpoint aan.
- **Read-only gating** op inschrijvingen + planning voor een hoofdtrainer-viewer:
  - Inschrijvingen: geen ⋮-acties/annuleren/markeer betaald/aanpassen, geen groep-annuleren/lid-bewerken;
    detail-dialog zonder "Aanpassen" en zonder lid-bewerkknop. Bekijken (rij → detail) blijft.
  - Planning: geen genereren/opnieuw genereren, bevestigen, lock/unlock, definitief aanbieden,
    toewijzingen maken/verwijderen, handmatig toewijzen, groep ontbinden. Enkel het rooster raadplegen.
- Nav/routing: de middleware laat trainers al bij `/dashboard/lessons`; de hoofdtrainer krijgt via de
  backend nu ook de data. (Plain trainers blijven 403 krijgen op die data.)

## Reset + seed

Na de migratie de reset-flow draaien (`reset-db.sh` + `seed-demo-data.sh`); seed evt. één trainer als
hoofdtrainer markeren voor demo/verificatie.

## Buiten scope

- Schrijfrechten voor hoofdtrainers.
- Onmiddellijke inwerkingtreding zonder re-login (bewust: JWT-gebaseerd).
- Fijnmazige per-reeks rechten.

## Verificatie

- Admin kan een trainer aan/uit hoofdtrainer zetten (knop + badge); niet-admins zien de knop niet.
- Na re-login ziet de hoofdtrainer inschrijvingen + planning **read-only** (geen actieknoppen);
  een plain trainer krijgt 403/geen toegang.
- Write-endpoints blijven verboden voor hoofdtrainers (403 bij directe call).
- Reset + seed loopt groen; `dotnet build` + `tsc --noEmit` groen.
