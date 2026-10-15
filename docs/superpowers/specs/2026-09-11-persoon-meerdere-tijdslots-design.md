# Persoon op meerdere tijdslots (multi-slot toewijzen)

**Datum:** 2026-09-11
**Type:** bugfix/feature — tester loopt vast omdat een persoon 2 trainingen/week wil.
**Branch:** eigen fix-branch vanaf `main` (los van de planning-UX-redesign).

## Probleem

Een inschrijving kan momenteel maar aan één tijdslot toegewezen worden. Een
speler die 2 trainingen per week wil volgen, kan niet op een tweede tijdslot
gezet worden.

## Kernvondst

Het datamodel is **al ontworpen** voor multi-slot. De unieke index op
`ScheduleAssignment` is `(LessonSerieId, WeeklyTemplateEntryId, EnrollmentId)` —
dezelfde leerling mag dus wél op meerdere *verschillende* slots, enkel niet 2×
op hetzelfde slot. Het enige dat multi-slot blokkeert is één te strenge guard in
`AssignmentService.CreateAssignmentAsync`.

## Aanpak

Meerdere toewijzingen op **dezelfde** inschrijving (verschillende slots) — niet
een tweede inschrijving. Dat vermijdt duplicaat-personen en e-mail-uniekheid, en
houdt één ontvanger voor de bevestigingsmail.

### Backend

- `CreateAssignmentAsync`: de guard versoepelen zodat enkel een duplicaat op
  **hetzelfde** slot geblokkeerd wordt (i.p.v. elke 2e toewijzing). Voor solo én
  groep:
  - solo: blokkeer als er al een assignment is met dezelfde `EnrollmentId` **en**
    dezelfde `WeeklyTemplateEntryId`.
  - groep: idem met `EnrollmentGroupId`.
  - Meldingen: "… staat al op dit tijdslot."
- De DB-index dekt dit al af als extra vangnet. Geen migratie nodig.
- Capaciteitscheck blijft ongewijzigd (bestaat al).

### Prijs / betaling — extra per slot (bevestigde keuze)

- Ongewijzigd. Betaling gebeurt al per toewijzing, dus 2 slots = 2× de
  reeksprijs. Elke bevestiging maakt z'n eigen `Payment`.

### Bevestigingsmail — één mail met beide slots

- Ongewijzigd. `ConfirmScheduleAsync` groepeert de mails al op `ContactEmail`.
  Beide toewijzingen van dezelfde inschrijving resolven naar dezelfde ontvanger
  → dezelfde `ContactEmail` → **één gebundelde mail** met beide slots, met een
  bevestig-knop per slot (elke knop = z'n eigen token/betaling, consistent met
  "extra per slot").

### Frontend — planningspagina (huidige main-UI)

Ingang: **in de slot-detail-dialog**.

- Per toewijzing (solo of groep) een knop **"+ Extra tijdslot"**.
- Klik → toont een slot-kiezer binnen de dialog met de overige slots
  (huidige slot + slots waar de persoon/groep al op staat uitgesloten; volle
  slots disabled).
- Kies een slot → `createAssignment` voor dezelfde `enrollmentId`/`groupId` op
  dat slot. De planning ververst; de persoon verschijnt nu in beide tegels.
- De pagina berekent de in-aanmerking-komende slots (kent alle assignments) en
  geeft die + een `onAssignToSlot`-callback door aan de dialog.

## Buiten scope

- Publieke inschrijving (blijft één slot-keuze via beschikbaarheid; multi-slot is
  een bewuste admin-actie op de planning).
- Auto-scheduling wijst nog steeds max. 1 slot per inschrijving toe; extra slots
  zijn puur manueel (en `IsLocked=true`, dus behouden bij opnieuw genereren).
- De planning-UX-redesign (aparte branch) — daar wordt dit later mee verweven /
  gerebased.

## Verificatie

- Reset + seed niet strikt nodig (geen migratie); wel: unit test op
  `CreateAssignmentAsync` (2e slot toegestaan, zelfde slot geweigerd), en
  handmatig: persoon op 2 slots zetten → bevestigen → één mail met beide slots +
  2 betalingen.
