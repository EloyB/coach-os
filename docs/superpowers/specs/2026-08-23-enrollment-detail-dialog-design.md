# Inschrijving-detail-dialog (bekijken)

**Datum:** 2026-08-23
**Branch:** `feat/enrollment-details-dialog` (boven op de inschrijvingen-tabel, PR #188)
**Scope:** frontend-only, inschrijvingen op de detailpagina van een lesreeks.

## Probleem

Een inschrijving heeft nu enkel "Inschrijving aanpassen" en "Inschrijving annuleren".
De aanpas-dialog toont alleen de basisvelden. Je kan **niet alle data** zien: de ingegeven
**beschikbaarheden** (tijdslot-voorkeuren) en de **antwoorden op de extra formuliervelden**
zijn nergens overzichtelijk zichtbaar.

## Doel

Een aparte, **read-only "Details bekijken"-dialog** die alle data van één inschrijving toont:
basis, beschikbaarheden (mini-grid) en formulierantwoorden. Bewerken blijft via de bestaande
aanpas-dialog (enkel basisvelden).

## Beslissingen (uit brainstorm)

1. **Aparte read-only detail-dialog** (niet de aanpas-dialog uitbreiden). Rij-klik opent voortaan
   de details; "Aanpassen" is een knop in de dialog en een ⋮-actie.
2. **Beschikbaarheden als read-only mini-grid** (dag-kolommen × tijdslot-rijen), zoals het
   inschrijfformulier.
3. Beschikbaarheden en formulierantwoorden zijn **enkel ter inzage**; alleen de basisvelden
   blijven bewerkbaar.

## Ontwerp

### Component

`_components/enrollment-detail-dialog.tsx` — read-only dialog. Geopend via rij-klik en ⋮ →
"Details bekijken". De bestaande inline "Details bekijken"-uitklap (formulierantwoorden onder
de rij) vervalt.

### Inhoud

- **Kop:** naam + badge (solo / leider / lid van *Groep X*) + status-badge.
- **Basisgegevens:** contact (eigen e-mail of "via [leider]", telefoon), geboortedatum + leeftijd,
  categorie, inschrijfdatum, "open voor koppeling".
- **Beschikbaarheden:** read-only mini-grid — kolommen = dagen met slots, rijen = tijdslot-ranges,
  cel = ● voorkeur / ○ beschikbaar / ✕ niet beschikbaar (+ legende). Lege staat
  "Geen beschikbaarheden opgegeven".
- **Formulierantwoorden:** label → waarde. Lege staat "Geen extra antwoorden".
- **Footer:** "Sluiten" + "Aanpassen" (opent de bestaande bewerk-dialog).

### ⋮-menu

Details bekijken · Inschrijving aanpassen · Markeer als betaald (leider/solo) · Inschrijving annuleren.

### Data

- Formulierantwoorden: al aanwezig in `LessonSeriesEnrollmentDto.formResponses`.
- Beschikbaarheden: lazy ophalen (React Query, gecached per reeks), geïndexeerd op inschrijving-id:
  - `GET /lessonseries/{id}/enrollments/planning` → `EnrollmentWithPreferencesDto[]`
    (`{ id, ..., preferences: [{ weeklyTemplateEntryId, preference }] }`, preference: 1=Beschikbaar,
    2=Voorkeur, 3=Niet beschikbaar) — nieuwe FE-wrapper toevoegen.
  - `getPublicTimeSlots(seriesId)` → tijdslot-definities (dag/uur) voor de grid-labels.
- **Frontend-only, geen backend-/DB-wijziging** → geen reset nodig.

### Randgevallen

- **Groepsleden** hebben meestal geen eigen beschikbaarheden/antwoorden (enkel de leider vulde die in):
  nette lege staat, evt. verwijzing "ingegeven door leider [naam]".
- **Parallelle banen** op hetzelfde dag+uur: per slot een bolletje in de cel (baannaam als tooltip).
- Beschikbaarheden nog aan het laden: kleine spinner/placeholder in de sectie.

### i18n

Nieuwe teksten (titel, sectiekoppen, legende, lege staten, "Aanpassen", "Sluiten", ...) naar
`messages/nl.json` via `useTranslations`.

## Buiten scope

- Bewerken van beschikbaarheden of formulierantwoorden.
- Backend-wijziging.

## Verificatie

- Rij-klik en ⋮ → "Details bekijken" openen de dialog met basis + beschikbaarheden-grid +
  formulierantwoorden.
- Grid toont de juiste voorkeuren; lege staten kloppen (solo zonder antwoorden, groepslid zonder prefs).
- "Aanpassen" opent de bestaande bewerk-dialog; annuleren/markeer-betaald blijven werken.
- `tsc --noEmit` groen; visuele check in de browser met de seed-inschrijvingen.
