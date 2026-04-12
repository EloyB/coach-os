# Student Confirmation + Payment Flow — Cash MVP

## Status

Backend complete for the **cash-only** path. Mollie integration deferred.

| Round | Scope | Status |
|-------|-------|--------|
| 1 | Domain + Infrastructure skeleton | ✅ done |
| 2 | Token generation + email on `Confirm planning` | ✅ done |
| 3 | Student-facing endpoints (cash path) | ✅ done |
| 4 | Mollie integration | ⏭️ deferred |
| 5 | Admin non-responders endpoint | ✅ done |
| FE | Confirmation page + non-responders panel | ⬜ pending |

---

## What's been built

### Domain

- `Domain/Enums/PaymentMethod.cs` — `Online=1`, `Cash=2`
- `Domain/Enums/ConfirmationResponse.cs` — `Pending=1`, `Confirmed=2`, `Declined=3`
- `Domain/Enums/ScheduleAssignmentStatus.cs` — extended with `AwaitingConfirmation=3`, `Declined=4`
- `Domain/Enums/PlanningStatus.cs` — extended with `AwaitingConfirmation=4`
- `Domain/Entities/AssignmentConfirmationToken.cs` — stores `TokenHash` (SHA256), `ExpiresAt`, `Response`
- `Domain/Entities/Payment.cs` — added `Method` (nullable `PaymentMethod`)
- `Domain/Interfaces/IAssignmentConfirmationTokenRepository.cs`
- `Domain/Interfaces/IPaymentRepository.cs`

### Infrastructure

- EF config + unique index on `TokenHash`
- `AssignmentConfirmationTokenRepository`, `PaymentRepository`
- `Payments` DbSet already existed; `AssignmentConfirmationTokens` added
- Migration: `AddStudentConfirmationFlow`
- Email: `EmailTemplates.ScheduleConfirmation` + `IEmailService.SendScheduleConfirmationAsync`
- Config: `App:ConfirmationBaseUrl` (default `http://localhost:3000/confirmation`)

### Application

**Modified:** `PlanningService.ConfirmScheduleAsync`
- Flips series → `AwaitingConfirmation` (was `Scheduled`)
- Flips assignments → `AwaitingConfirmation` (was `Confirmed`)
- Generates one token per contact point:
  - Solo → token for the enrolled student
  - Group → token for the leader only
- Hashes raw token with SHA256 (64-char hex); raw token only appears in the email URL
- 72h expiry
- Sends confirmation email via `SendScheduleConfirmationAsync`

**New feature:** `Application/StudentConfirmation/`
- `IStudentConfirmationService` + impl
- DTOs: `AssignmentDetailsDto`, `ConfirmRequest`, `ConfirmResultDto`, `AvailableSlotDto`, `PickAlternativeRequest`
- Methods: `GetByTokenAsync`, `ConfirmAsync`, `DeclineAsync`, `GetAvailableSlotsAsync`, `PickAlternativeAsync`
- Token replay protection (Pending → Confirmed/Declined is one-way)
- Pricing: `series.Price × groupSize`
- Series auto-finalizes (`AwaitingConfirmation → Scheduled`) when every token is resolved

### API

**Public (AllowAnonymous):**

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/public/confirmation/{token}` | Assignment details |
| POST | `/public/confirmation/{token}/confirm` | Confirm + pay (cash) |
| POST | `/public/confirmation/{token}/decline` | Decline, returns available slots |
| GET | `/public/confirmation/{token}/available-slots` | Alternatives |
| POST | `/public/confirmation/{token}/pick-alternative` | Pick new slot + pay |

**Admin:**

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/lessonseries/{id}/planning/non-responders` | Students with pending/expired tokens |

### Tests

95 passing. Existing `PlanningService` tests updated: the "confirm" test now asserts `AwaitingConfirmation` instead of `Scheduled`.

---

## State machine

```
Enrollment ──admin generates──▶ Planning
                                   │
                                   │ admin clicks "Bevestig"
                                   ▼
                          AwaitingConfirmation
                                   │
                                   │ every token resolved
                                   ▼
                               Scheduled
```

Per assignment:

```
Proposed ──admin confirms──▶ AwaitingConfirmation
                                  │
                   student cash──▶ Confirmed
                        decline──▶ Declined
                   pick alt + pay─▶ (new Confirmed row)
```

---

## Configuration

`appsettings.json`:

```json
"App": {
  "ConfirmationBaseUrl": "http://localhost:3000/confirmation"
}
```

In production: set to the public FE URL (e.g. `https://app.coachos.be/confirmation`).

---

## FE work remaining

Full prompt is in the handoff message. Short version:

1. **Public page** `/confirmation/[token]` — shows assignment details, two buttons (Bevestigen/Afwijzen), cash-only for now, decline flow offers alternative slots.
2. **Admin panel** on planning dashboard — shown only when `planningStatus === "AwaitingConfirmation"`, lists non-responders with call/WhatsApp/email-copy buttons, sorted expired-first.
3. **Status badge** — add "AwaitingConfirmation" → "Wacht op bevestiging" (amber).

---

## Mollie integration (deferred)

When we're ready:

- Add `Mollie.Api` NuGet to Infrastructure
- `IMolliePaymentService` + impl
- Extend `ConfirmAsync` / `PickAlternativeAsync` online branch: create `Payment(Pending, Online)`, call Mollie, return `CheckoutUrl` in `ConfirmResultDto`
- New `POST /webhooks/mollie` endpoint: fetch status from Mollie API, mark Payment Paid, mark token/assignment Confirmed
- Config: `Mollie:ApiKey`
- Public webhook URL required (ngrok in dev)

The current `ConfirmResultDto.CheckoutUrl` field is already there (always null today) so the FE contract won't break when Mollie lands.

---

## Apply locally

```bash
cd backend
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API
dotnet test CoachOS.slnx
```

---

## Gotchas to remember

- Tokens are single-use. Re-clicking a link after confirming shows "Deze bevestiging is al verwerkt."
- The raw token only exists in the email URL. Lose the email = admin has to manually reassign or regenerate planning.
- `TryFinalizeSeriesAsync` runs after every confirm / pick-alternative. Expired tokens count as "handled" — so one non-responder doesn't block the series forever.
- Group leader pays for the whole group in one transaction. Individual members never see a payment screen.
- Capacity check on `pick-alternative` excludes Declined assignments, so a leader who declines then picks their original slot back would succeed (edge case — acceptable).
