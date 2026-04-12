# FE Prompt — Student confirmation flow + non-responders panel

Hand this to the frontend developer. Backend is complete (cash-only, see `student-confirmation-cash-mvp.md`).

---

## Context

After the admin clicks "Bevestig planning" on the planning dashboard, the backend:
- flips the series to `AwaitingConfirmation`
- generates a unique token per solo student and per group leader
- emails each recipient a link like `{ConfirmationBaseUrl}/{rawToken}` (default `http://localhost:3000/confirmation`)
- tokens expire 72h after planning confirmation

Your job: build the public confirmation page, and add a "non-responders" panel to the planning dashboard so the admin can chase stragglers.

---

## 1. Public confirmation page — route `/confirmation/[token]`

Public (no auth). The token in the URL is the raw token — pass it verbatim in every API call. Do NOT hash client-side.

### Flow

**On page load** → `GET /public/confirmation/{token}`
- `200` → show slot details with two buttons: **Bevestigen**, **Afwijzen**
- `400 validation` "Deze link is verlopen." → show expired state
- `404` "Ongeldige of verlopen link." → show not-found state

**"Bevestigen" clicked**
- Only Cash is available for now. Render two radio options:
  - "Cash — betaal aan de club" (selected)
  - "Online" (disabled, label: "binnenkort beschikbaar")
- `POST /public/confirmation/{token}/confirm`
  - body: `{ paymentMethod: 2, redirectUrl: window.location.origin + "/confirmation/done" }`
  - `200 { isConfirmed: true }` → success page ("Bedankt, je plek is bevestigd")
  - `400 validation` "Deze bevestiging is al verwerkt." → already-handled state

**"Afwijzen" clicked**
- `POST /public/confirmation/{token}/decline` → `{ availableSlots: [...] }`
- Show "Kies een ander tijdslot" with the list. Each slot shows day + time + court + remainingCapacity.
- If the enrollment is a group, the BE already filtered for slots with room for the full group — no client-side filtering needed.
- User picks one → `POST /public/confirmation/{token}/pick-alternative`
  - body: `{ weeklyTemplateEntryId, paymentMethod: 2 }`
  - `200 { isConfirmed: true }` → success page "Je plek is verplaatst naar {day} {startTime}."
  - `400 validation` → toast the message

### Response shapes

```ts
// GET /public/confirmation/{token}
type AssignmentDetailsDto = {
  assignmentId: string
  seriesName: string
  dayOfWeek: number            // 0=sunday..6=saturday
  startTime: string            // "HH:mm"
  endTime: string              // "HH:mm"
  courtName: string | null
  studentName: string          // recipient (leader if group)
  pricePerPerson: number       // EUR
  totalPrice: number           // pricePerPerson * groupSize
  isGroup: boolean
  groupMemberNames: string[]   // e.g. ["Lucas Claes", "Lotte Claes"]
  status: "Pending" | "Confirmed" | "Declined"
  expiresAt: string            // ISO timestamp
}

// POST confirm / pick-alternative
type ConfirmResultDto = { isConfirmed: boolean, checkoutUrl: string | null }

// POST decline → { availableSlots: AvailableSlotDto[] }
// GET  available-slots → { slots: AvailableSlotDto[] }
type AvailableSlotDto = {
  weeklyTemplateEntryId: string
  dayOfWeek: number
  startTime: string
  endTime: string
  courtName: string | null
  remainingCapacity: number
}
```

### UX notes

- Design language: reuse the student-facing enrollment form styling (split layout, tennis-green / lime / off-white).
- Dutch only — strings in `messages/nl.json` under `confirmation` namespace.
- Countdown: "Verloopt op dd/MM om HH:mm" from `expiresAt`.
- Group case: "Je bevestigt voor Emma Claes + 2 leden (Lucas, Lotte). Totaal: €360."

---

## 2. Admin non-responders panel

Add a new section to the planning dashboard, **only visible when** `planningStatus === "AwaitingConfirmation"`.

Fetch on mount:

```
GET /lessonseries/{id}/planning/non-responders
→ NonResponderDto[]
```

```ts
type NonResponderDto = {
  assignmentId: string
  enrollmentId: string
  studentName: string
  studentEmail: string
  studentPhone: string | null
  isGroup: boolean
  groupSize: number
  dayOfWeek: number
  startTime: string
  endTime: string
  courtName: string | null
  expiresAt: string   // ISO
  isExpired: boolean
}
```

### Layout

- Panel title: **"Wachten op bevestiging (N)"** where N is the count
- Each row shows:
  - Name + "leider van groep ({groupSize})" if `isGroup`
  - Slot: "{dayName} {startTime}"
  - **Bellen** button (`tel:{phone}`) and **WhatsApp** button (`https://wa.me/{phone without +}`) — hide both when phone is null
  - **E-mail kopiëren** button that copies `studentEmail` to clipboard
  - Expiry badge:
    - `isExpired === true` → red "Verlopen"
    - otherwise → amber "Verloopt over {relative time}" (use `date-fns` `formatDistanceToNow` with nl locale)
- BE pre-sorts: expired first, then soonest expiry. Don't re-sort.
- Empty list → hide the panel entirely
- Refetch after any planning mutation (same query key as the rest of the planning dashboard)
- When the series flips to "Scheduled" (all tokens handled), panel disappears — show a small success toast "Iedereen heeft bevestigd — planning is definitief."

---

## 3. Planning dashboard status badge

Add one new value to the existing badge logic:

- `"AwaitingConfirmation"` → amber badge text **"Wacht op bevestiging"**

No other dashboard behaviour changes during `AwaitingConfirmation` — assign/move/generate are locked out by the BE anyway; admin can still read the current state.

---

## Files you'll touch

- `frontend/app/confirmation/[token]/page.tsx` (new, server component shell)
- `frontend/app/confirmation/[token]/confirmation-client.tsx` (new, client)
- `frontend/lib/api/confirmation.ts` (new, API client)
- `frontend/lib/api/planning.ts` (add `getNonResponders`)
- `frontend/components/dashboard/non-responders-panel.tsx` (new)
- `frontend/app/(dashboard)/dashboard/lessons/[id]/planning/page.tsx` (render panel conditionally + new badge value)
- `frontend/messages/nl.json` (new `confirmation` namespace + strings for "Wacht op bevestiging", "Verlopen", "Bellen", "WhatsApp", "E-mail kopiëren", "Verloopt over {time}", "Binnenkort beschikbaar", etc.)

---

## NOT in scope yet

- **Online payment (Mollie)** — BE returns `400 validation` if `paymentMethod: 1`. Render the option as disabled with "binnenkort beschikbaar".
- **Admin override**: no endpoint to force-confirm a non-responder from the admin side yet. Contact buttons (call/WhatsApp/email) are the manual workaround.

---

## Endpoint cheat sheet

| Method | Path | Auth | Body / Returns |
|---|---|---|---|
| GET | `/public/confirmation/{token}` | Public | `AssignmentDetailsDto` |
| POST | `/public/confirmation/{token}/confirm` | Public | `{ paymentMethod, redirectUrl? }` → `ConfirmResultDto` |
| POST | `/public/confirmation/{token}/decline` | Public | — → `{ availableSlots }` |
| GET | `/public/confirmation/{token}/available-slots` | Public | — → `{ slots }` |
| POST | `/public/confirmation/{token}/pick-alternative` | Public | `{ weeklyTemplateEntryId, paymentMethod, redirectUrl? }` → `ConfirmResultDto` |
| GET | `/lessonseries/{id}/planning/non-responders` | Admin | — → `NonResponderDto[]` |
