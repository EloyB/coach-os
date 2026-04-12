# Student Confirmation + Payment Flow — Backend Plan

## Context

After admin confirms planning, students must confirm their assigned slot and pay before it's finalized. This closes the loop: admin plans → students confirm → payments collected → lessons happen.

**Key decisions:**
- Leader confirms and pays for entire group (one email, one payment for group total)
- Mollie (iDEAL + Bancontact) for online payment, cash as alternative
- Student can decline and pick an alternative slot
- Tokens expire after 72h, admin notified of non-responders

---

## Flow

```
Admin clicks "Bevestig planning"
    │
    ▼
Assignments → AwaitingConfirmation
Series → AwaitingConfirmation
Emails sent (1 per solo, 1 per group leader)
    │
    ▼
Student clicks link in email
    │
    ├─ Confirms → picks payment method
    │     ├─ Cash → Confirmed immediately
    │     └─ Online → Mollie checkout → webhook → Confirmed
    │
    └─ Declines → sees available slots → picks new slot → payment
```

---

## Phase 1: Domain Changes

### New Enums

| File | Values |
|------|--------|
| `Domain/Enums/PaymentMethod.cs` | Online=1, Cash=2 |
| `Domain/Enums/ConfirmationResponse.cs` | Pending=1, Confirmed=2, Declined=3 |

### Modified Enums

| File | Add |
|------|-----|
| `Domain/Enums/ScheduleAssignmentStatus.cs` | AwaitingConfirmation=3, Declined=4 |
| `Domain/Enums/PlanningStatus.cs` | AwaitingConfirmation=4 |

### New Entity: `AssignmentConfirmationToken`

```
Domain/Entities/AssignmentConfirmationToken.cs
- OrganizationId, ScheduleAssignmentId, EnrollmentId
- TokenHash (SHA256, 64 chars), ExpiresAt, RespondedAt?
- Response (ConfirmationResponse)
- Nav: Organization, ScheduleAssignment, Enrollment
```

One token per solo enrollment, one token per group leader.

### Modified Entity: `Payment`

Add: `PaymentMethod? PaymentMethod` field.

### New Interfaces

| File | Key Methods |
|------|-------------|
| `Domain/Interfaces/IAssignmentConfirmationTokenRepository.cs` | GetByTokenHashAsync, GetByAssignmentIdAsync, AddRangeAsync, SaveChangesAsync |
| `Domain/Interfaces/IPaymentRepository.cs` | GetByIdAsync, GetByMolliePaymentIdAsync, AddAsync, SaveChangesAsync |
| `Domain/Interfaces/IMolliePaymentService.cs` | CreatePaymentAsync, GetPaymentStatusAsync |

### Extend `IEmailService`

Add: `SendScheduleConfirmationAsync(studentEmail, studentName, seriesName, dayOfWeek, startTime, endTime, courtName, confirmationUrl, ct)`

---

## Phase 2: Infrastructure

### New Files

| File | Description |
|------|-------------|
| `Infrastructure/Persistence/Configurations/AssignmentConfirmationTokenConfiguration.cs` | Unique index on TokenHash, FK to Assignment+Enrollment (Restrict) |
| `Infrastructure/Repositories/AssignmentConfirmationTokenRepository.cs` | Implementation |
| `Infrastructure/Repositories/PaymentRepository.cs` | Implementation |
| `Infrastructure/Mollie/MollieOptions.cs` | Config: ApiKey |
| `Infrastructure/Mollie/MolliePaymentService.cs` | Implements IMolliePaymentService using `Mollie.Api` NuGet |

### Modified Files

| File | Change |
|------|--------|
| `Infrastructure/Persistence/ApplicationDbContext.cs` | Add DbSet + config registration |
| `Infrastructure/Persistence/Configurations/PaymentConfiguration.cs` | Add PaymentMethod column |
| `Infrastructure/Email/EmailService.cs` | Add SendScheduleConfirmationAsync |
| `Infrastructure/Email/EmailTemplates.cs` | Add confirmation email template |
| `Infrastructure/DependencyInjection.cs` | Register new repos + MolliePaymentService + MollieOptions |

### NuGet

Add `Mollie.Api` to `CoachOS.Infrastructure.csproj`.

### Migration

`dotnet ef migrations add AddStudentConfirmationFlow`

---

## Phase 3: Application

### New Feature: `Application/StudentConfirmation/`

**Service interface + implementation:**

| Method | What it does |
|--------|-------------|
| `GetAssignmentByTokenAsync(token)` | Hash token → look up → return slot details (series, day, time, court, price) |
| `ConfirmAsync(token, paymentMethod, redirectUrl)` | Cash: create Payment(Paid), mark confirmed. Online: create Payment(Pending), call Mollie, return checkoutUrl |
| `DeclineAsync(token)` | Mark declined, return available slots |
| `PickAlternativeAsync(token, newSlotId, paymentMethod, redirectUrl)` | Create new assignment, mark old as Declined, proceed to payment |
| `HandleMollieWebhookAsync(molliePaymentId)` | Verify with Mollie API, mark Payment Paid, mark token Confirmed |

**DTOs:**

- `AssignmentDetailsDto` — seriesName, dayOfWeek, startTime, endTime, courtName, studentName, price, groupMembers[]
- `ConfirmRequest` — paymentMethod (int), redirectUrl (string)
- `ConfirmResultDto` — isConfirmed (bool), checkoutUrl? (string)
- `AvailableSlotDto` — weeklyTemplateEntryId, dayOfWeek, startTime, endTime, courtName, remainingCapacity
- `PickAlternativeRequest` — weeklyTemplateEntryId, paymentMethod, redirectUrl

**Validators:** ConfirmRequestValidator, PickAlternativeRequestValidator

### Modify `PlanningService.ConfirmScheduleAsync`

Current: sets assignments to Confirmed, series to Scheduled.

New:
1. Set assignments to `AwaitingConfirmation`
2. Set series to `AwaitingConfirmation`
3. For each assignment:
   - Solo: generate token for the enrollment, send email
   - Group: generate token for the leader enrollment, send email to leader
4. Store tokens (hashed) with 72h expiry

### DI

Register `IStudentConfirmationService → StudentConfirmationService`

---

## Phase 4: API Endpoints

All public (AllowAnonymous, rate-limited with "public" policy):

| Endpoint | Route | Description |
|----------|-------|-------------|
| `GetAssignmentDetailsEndpoint` | `GET /public/confirmation/{token}` | Student sees their assigned slot |
| `ConfirmAssignmentEndpoint` | `POST /public/confirmation/{token}/confirm` | Confirm + pay |
| `DeclineAssignmentEndpoint` | `POST /public/confirmation/{token}/decline` | Decline, get alternatives |
| `GetAvailableSlotsEndpoint` | `GET /public/confirmation/{token}/available-slots` | Available slots after decline |
| `PickAlternativeEndpoint` | `POST /public/confirmation/{token}/pick-alternative` | Pick new slot + pay |
| `MollieWebhookEndpoint` | `POST /webhooks/mollie` | Mollie payment callback |

---

## Phase 5: Token Expiry Handling

Add an endpoint for admin to check non-responders:

`GET /lessonseries/{id}/planning/non-responders` → returns list of students who haven't responded within 72h.

Admin can then manually reassign or extend deadlines. (Background job is nice-to-have but not MVP — admin can check manually.)

---

## Configuration

Add to `appsettings.json` / environment:
```json
{
  "Mollie": {
    "ApiKey": "test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  },
  "App": {
    "ConfirmationBaseUrl": "http://localhost:5317/confirmation"
  }
}
```

---

## API Contract for FE

**Student confirmation page (public):**
- `GET /public/confirmation/{token}` → `AssignmentDetailsDto`
- `POST /public/confirmation/{token}/confirm` → `{ isConfirmed, checkoutUrl? }`
- `POST /public/confirmation/{token}/decline` → `{ availableSlots[] }`
- `GET /public/confirmation/{token}/available-slots` → `{ slots[] }`
- `POST /public/confirmation/{token}/pick-alternative` → `{ isConfirmed, checkoutUrl? }`

**Admin:**
- `GET /lessonseries/{id}/planning/non-responders` → list of non-responders

**Mollie redirect:** after online payment, Mollie redirects student back to `{redirectUrl}?payment=success` or `?payment=failed`. FE shows appropriate message.

---

## Verification

1. Unit tests for `StudentConfirmationService` (mock repos + mock Mollie)
2. Smoke test: create series → enroll → generate → confirm planning → use token to confirm with cash → verify assignment confirmed
3. Mollie: test with Mollie test API key (creates test payments that auto-complete)
4. Token expiry: create token with past date → verify non-responders endpoint returns it
