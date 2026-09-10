# Parallelle tijdslots samenvouwen in het inschrijvingsformulier

**Datum:** 2026-09-10
**Scope:** frontend-only — `frontend/app/(public)/enroll/[seriesId]/page.tsx`

## Probleem

In het publieke inschrijvingsformulier duidt een speler zijn beschikbaarheid aan
per tijdslot. Wanneer een lessenreeks op één dag/uur meerdere parallelle banen
heeft, staat datzelfde uur nu meerdere keren in de lijst (één rij per baan). De
speler moet dan drie keer dezelfde beschikbaarheid aanduiden voor wat in de
praktijk hetzelfde uur is. Overbodig en verwarrend.

## Doel

Toon elk uniek uur per dag één keer. De speler kiest zijn beschikbaarheid één
keer; die keuze geldt voor alle parallelle banen op dat uur.

## Waarom frontend-only

- Elke parallelle baan is een aparte `WeeklyTemplateEntry` (zelfde `dayOfWeek` +
  `startTime` + `endTime`, andere `courtName`).
- Beschikbaarheid gaat naar de backend als
  `timeSlotPreferences: [{ weeklyTemplateEntryId, preference }]`.
- Het scheduling-algoritme (`SchedulingAlgorithm`, `PlanningProposalBuilder`)
  verwacht één preference **per** `WeeklyTemplateEntryId`.

Als een speler beschikbaar is voor "dat uur", is hij beschikbaar voor élke
parallelle baan op dat uur. We groeperen dus in de UI en klappen bij submit de
gekozen preference uit naar alle onderliggende entries. De backend krijgt exact
wat hij vandaag al verwacht — geen wijziging aan DTOs, entities, validators of
het algoritme.

## Ontwerp

### 1. Groeperen

Groepeer de slots op key `dayOfWeek|startTime|endTime`. Elke groep bevat de
`weeklyTemplateEntryId`s van de parallelle banen op dat uur. De bestaande
dag-header (Maandag, Dinsdag, …) blijft.

Groepering op exact gelijke start- én eindtijd: verschillende duur (bv.
10:00–11:00 vs 10:00–11:30) blijft apart, want dat zijn andere lessen.

### 2. Preference-state per groep

De `preferences`-state gaat van per-slot-id naar per-groep-key. Eén klik zet de
preference voor de hele groep. `PrefButton` en `setPreference` werken op de
groep-key i.p.v. `slot.id`.

### 3. Submit — uitklappen naar alle entries

```
timeSlotPreferences = groups.flatMap(g =>
  g.entryIds.map(id => ({ weeklyTemplateEntryId: id, preference: g.pref }))
)
```

### 4. Baannaam verdwijnt uit de rij

De baannaam is informatie voor trainers/admins (op welke baan een slot valt),
niet relevant voor de speler die op uur kiest. De rij toont enkel
`startTime — endTime`. Geldt voor desktop én mobiel.

### 5. Rand-geval — één baan per uur

Een groep met één entry werkt identiek (groep van grootte 1). Geen speciale
behandeling nodig.

## Buiten scope

Datacontracten, scheduling-algoritme, capaciteit/merge-logica, en de algemene
desktop/mobiele layout-structuur blijven ongewijzigd. Enkel de rij-inhoud en de
state-key veranderen.
