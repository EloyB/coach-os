# Planning-pagina UX quick-wins

**Datum:** 2026-09-10
**Scope:** frontend-only, geen nieuwe libraries, geen backend-wijzigingen.
**Branch:** `feat/planning-ux-redesign`

## Doel

De planningspagina (`app/(dashboard)/dashboard/lessons/[id]/planning/`) heeft te
veel kliks nodig voor courante acties, en je kan er de volledige
inschrijvingsdata van een persoon/groep niet raadplegen. Dit zijn incrementele
quick-wins; geen drag-and-drop en geen layout-omgooi.

Deze branch levert **raadplegen**. Bewerken vanuit de planning is een bewust
uitgestelde tweede stap (aparte branch).

## Wijzigingen

### 1. Klikbare personen → inschrijving-detaildialog (read-only)

De bestaande `EnrollmentDetailDialog` (tabs Gegevens / Leden / Beschikbaarheden)
wordt herbruikt vanaf de planning, in kijkmodus.

- **Dialog view-only maken:** `onEdit` wordt optioneel; de "Bewerken"-knop
  toont enkel wanneer `onEdit` is meegegeven. De reeks-tabel geeft `onEdit` nog
  steeds mee → geen gedragswijziging daar. De planning geeft geen bewerk-callbacks
  → puur raadplegen.
- **Data:** de planning haalt `getLessonSeriesEnrollments(id)` op (query
  `["enrollments", id]`, al gecached door de reeks-tabel) en bouwt een
  id → `LessonSeriesEnrollmentDto` map. De enrollment-id's komen overeen met de
  `PlanningEnrollmentDto.id`'s.
- **Klikbaar op 3 plekken:**
  1. Avatars/namen in de kalendertegels.
  2. De niet-toegewezen kaarten in de sidebar (solo-naam; groepsnaam + leden).
  3. De ledenrijen in de slot-dialog (`TimeslotDetailDialog`).
- **Gedrag:** klik op een solo → persoon-detail. Klik op een groep(snaam) →
  groep-detail (leider als `enrollment`, leden als `groupMembers`), zonder de
  lid-bewerk/verwijder-callbacks.

### 2. Toewijs-modus met beschikbaarheids-kleuren

Vervangt de huidige platte slot-lijst in de sidebar-kaarten.

- Klik "Toewijzen" op een niet-toegewezen kaart → **toewijs-modus** voor die
  persoon/groep. Een banner toont wie je aan het inplannen bent + "Esc om te
  annuleren".
- In toewijs-modus kleuren de kalendertegels naar de voorkeur:
  **groen = voorkeur, blauw = beschikbaar, gedimd = niet beschikbaar,
  doorkruist/uit = vol**. Voor een groep: kleur op basis van de **leider** z'n
  voorkeuren (consistent met de sidebar).
- Klik een tegel → `assignMutation`. Een **volle** tegel is niet klikbaar; een
  "niet beschikbaar"-tegel blijft klikbaar zodat de trainer bewust kan
  overschrijven. Esc of opnieuw op de kaart klikken sluit de modus.

### 3. Hover-popover: propere read-only info (acties in de dialog)

Eerst geprobeerd: inline snelacties (lock/aanbieden/loskoppelen) als icoontjes in
de popover. Teruggedraaid na feedback — te veel clutter in te weinig ruimte en
icoon-only was onduidelijk.

Definitief:

- De hover-popover is een **read-only** info-kaart (dag/uur, baan/trainer,
  toewijzingen + namen, bezetting). Acties blijven in de slot-dialog, waar ze
  al mét tekstlabel staan (Vastzetten / Definitief aanbieden / Loskoppelen).
  Patroon: hover = preview, klik op de tegel = handelen.
- De popover rendert via **Radix HoverCard** (`components/ui/hover-card.tsx`,
  portal + botsing-detectie) i.p.v. een absolute div binnen de scrollbare
  kalender. Zo valt de popover niet meer binnen de agenda-container (geen
  clipping/scroll meer bij tijdslots onderaan) en flipt hij automatisch in beeld.

## Buiten scope (bewust)

- Bewerken vanuit de planning (aparte tweede stap).
- Drag-and-drop, nieuwe layout, bulk "alles aanbieden", backend-wijzigingen.

## Klikwinst

| Actie | Nu | Na |
|---|---|---|
| Inschrijvingsdata bekijken | onmogelijk | 1 klik |
| Toewijzen | 2 (blind scrollen) | 2 (met kleurhints) |
| Verplaatsen | 4+ | 2 (loskoppelen via hover + toewijs-modus) |
| Lock / aanbieden | 2–3 | 1–2 (inline) |

## Risico

Het grootste risico zit in het klikbaar maken zonder de bestaande read-only
(hoofdtrainer) gates te doorbreken, en in het view-only maken van de gedeelde
dialog zonder de reeks-tabel te raken. Mitigatie: `onEdit` optioneel maken is
puur additief; bestaande e2e (`enrollment.spec.ts`, planning-tests) als vangnet.
