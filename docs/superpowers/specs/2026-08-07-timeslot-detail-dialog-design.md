# Tijdslot-detail-dialog (planningspagina)

**Datum:** 2026-08-07
**Branch:** `feat/issue-178-lock-complete-groups`
**Scope:** frontend-only, planningspagina van een lesreeks.

## Probleem

Op de planningspagina (`app/(dashboard)/dashboard/lessons/[id]/planning/page.tsx`) staan
de acties om een tijdslot **vast te zetten** en **definitief aan te bieden** als kleine knoppen
direct op de kalendertegel én nog eens in een hover-popover. Dat maakt de tegel druk en verspreidt
dezelfde acties over twee plekken.

## Doel

Klikken op een tijdslot-tegel opent een **dialog** met alle info van dat slot en de acties om
toewijzingen aan te passen. De tegel wordt schoner; de hover-popover wordt een read-only "peek".

## Beslissingen (uit brainstorm)

1. **Hover-popover** → read-only. Alleen kop (dag/uur), deelnemers (avatar + naam) en bezetting.
   Geen actieknoppen meer.
2. **Dialog-acties** → exact de bestaande acties, netjes gebundeld: per toewijzing vastzetten/vrijgeven,
   definitief aanbieden, en toewijzing verwijderen. Geen verplaatsen, geen contactknoppen (die blijven
   in de zijbalk).

## Ontwerp

### Nieuw component

`app/(dashboard)/dashboard/lessons/[id]/planning/_components/timeslot-detail-dialog.tsx`

- Gebruikt shadcn `Dialog` (zoals `lessons/new/_components/slot-dialog.tsx`), zodat styling consistent is.
- Presentational + gecontroleerd. De mutaties (React Query) blijven in `page.tsx`; het component krijgt
  callbacks + pending-states door.

**Props (concept):**

```ts
interface TimeslotDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  slot: PlanningTimeSlotDto | null;          // null → dialog rendert niets
  assignments: PlanningAssignmentDto[];
  enrollmentMap: Map<string, PlanningEnrollmentDto>;
  groupMap: Map<string, PlanningGroupDto>;
  currentCount: number;
  planningStatus: string;
  onLock: (assignmentId: string, isLocked: boolean) => void;
  onOffer: (assignmentId: string) => void;
  onUnassign: (assignmentId: string) => void;
  isLockPending: boolean;
  isOfferPending: boolean;
  isUnassignPending: boolean;
}
```

### Page-wijziging

- Nieuwe state `openSlotId: string | null`.
- Tegel-`onClick` zet `openSlotId` (werkt voor élk slot, ook leeg → dialog toont slot-info + lege staat).
- Eén `<TimeslotDetailDialog>` onderaan de render, gestuurd door `openSlotId`.
- Acties in de dialog roepen dezelfde bestaande mutaties aan (`lockMutation`, `sendConfirmationMutation`,
  `unassignMutation`).

### Tegel wordt schoner

- Verwijder de twee actieknoppen (lock/unlock + mail) uit de tegel-header (huidige regels ~750–792).
- Header behoudt: baannaam · `auto`-badge · bezetting `x/max` · avatars.
- `cursor-pointer` blijft; `onClick` opent de dialog.

### Hover-popover → read-only peek

- Behoudt: kop (dag/uur), subregel (baan · trainer), deelnemers (avatar + naam), bezetting `x/max bezet`.
- Verwijder: lock/vrijgeven-knop, "definitief aanbieden"-knop, en de "X" verwijder-knop per toewijzing.

### Dialog-inhoud

- **Kop:** `Maandag 18:00–19:00` (volledige dagnaam). Subregel: baan · trainer.
- **Bezetting:** `x/max bezet` met dezelfde kleuraccenten (groen / amber / blauw).
- **Per toewijzing:** groepsbadge of "Individueel" + `Vastgezet`/`auto`-badges; avatars + namen; actie-rij:
  - status `Proposed`: **Vastzetten/Vrijgeven** (voor groep: "Groep vastzetten") + **Definitief aanbieden**.
  - altijd: **Verwijder toewijzing**.
- **Lege staat:** "Nog niemand toegewezen."
- **Footer:** Sluiten-knop.

### Styling

shadcn Dialog (rounded-xl, wit), tennis-green accenten, identieke badge-kleuren als nu
(groen = vastgezet, blauw = auto, amber = voorstel). Avatar-helpers (`getInitials`, `getAvatarColor`)
worden hergebruikt — verplaatsen naar een gedeeld util zodat zowel page als dialog ze delen.

### i18n

Bestaande `planning`-keys hergebruiken (`lock`, `unlock`, `lockGroup`, `locked`, `offerDefinitively`,
`unassign`, ...). Nieuw toevoegen aan `messages/nl.json` onder `planning`:

- `slotDialogEmpty`: "Nog niemand toegewezen."
- `close`: "Sluiten"
- `occupied`: "{count}/{max} bezet" (of hergebruik van bestaande weergave)

## Buiten scope

- Deelnemer/groep verplaatsen naar een ander slot vanuit de dialog.
- Contactknoppen (e-mail/bellen/WhatsApp) in de dialog.
- Backend-wijzigingen (geen; endpoints bestaan al).

## Verificatie

- Klik op een gevulde tegel → dialog met deelnemers + juiste acties (proposed vs niet).
- Vastzetten/vrijgeven, definitief aanbieden en verwijderen werken vanuit de dialog en verversen de planning.
- Klik op een lege tegel → dialog met slot-info + lege staat.
- Hover toont read-only peek zonder knoppen.
- Tegel-header toont geen actieknoppen meer.
- Geen hardcoded NL-strings; alles via `useTranslations("planning")`.
- `bun run build` slaagt (typecheck).
