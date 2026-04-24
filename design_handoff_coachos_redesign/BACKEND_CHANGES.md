# Backend changes

Every row maps a UI change to its data requirement. Rows are grouped by phase so you can open one PR per row.

**Assumed stack:** .NET / EF Core / REST (confirm before starting — see `OPEN_DECISIONS.md` Q1).

**Conventions**
- `Dto` = wire format returned by endpoint
- `Entity` = EF entity / DB schema
- All new endpoints follow existing auth (cookie/JWT — match what's there)
- All new DTO fields are additive; no removals in Phase 0/1

---

## Phase 0 — no backend changes

The following redesigns ship with the existing API as-is. Listed for completeness.

| UI | Existing endpoint | Notes |
|---|---|---|
| Login visual overhaul | `POST /auth/login` | Unchanged |
| Register visual overhaul | `POST /auth/register` | Unchanged (SSO = Phase 2) |
| Student login visual overhaul | `POST /auth/magic-link` | Unchanged; confirm it exists |
| Lesreeksen → table view | `GET /series` | Reuse existing list |
| Trainers card grid | `GET /trainers` | Reuse existing list |
| Planning calendar visual overhaul | `GET /lessons?from=&to=` | Reuse; no new fields needed for Phase 0 |
| Student portal visual overhaul | `GET /student/me/lessons` | Reuse |
| Enrolment smart defaults, running total, password-rule chips | — | Pure FE logic |
| Confirmation countdown strip, greeting | `GET /confirm/{token}` | Use existing `expires_at` |
| All typography/color/spacing changes | — | FE only |

---

## Phase 1 — additive backend

Each row = one PR, with DB migration (if any), DTO change, endpoint, and tests.

### 1.1 — Unified inbox for Dashboard "Vraagt actie"

**UI:** `DashboardP` → priority row showing 4 items: pending confirmations, under-booked series, reschedule requests, overdue payments.

**New endpoint**
```
GET /dashboard/inbox?limit=10
→ 200 { items: InboxItem[], updated_at: Instant }

InboxItem {
  type: "confirmation_pending" | "series_underbooked" | "reschedule_request" | "payment_overdue"
  ref_type: "Student" | "Series" | "Lesson" | "Payment"
  ref_id: Guid
  title: string           // "Els Verhaegen"
  body: string            // "heeft nog niet bevestigd voor morgen 18:00"
  meta: string            // "verloopt over 6u" | "€42 verlies"
  severity: "warn" | "urgent"
  created_at: Instant
}
```

**Implementation:** Union-query across four existing tables. Most logic already exists in scattered endpoints — consolidate.

**Tests:** coverage of each `type`, empty state, severity sort order.

---

### 1.2 — Occupancy on every listing

**UI:** Series tiles, Planning slots, Student portal cards, Lesreeksen table → show `enrolled / capacity` with a progress bar.

**DTO changes (additive)**
```diff
 SeriesDto {
   id, name, trainer_id, starts_on, ends_on, price_cents,
+  enrolled_count: int,      // SUM(enrolments WHERE series_id = X AND state = 'active')
+  total_capacity: int,      // SUM(lessons.capacity WHERE series_id = X) — or series-level cap
 }

 LessonDto {
   id, series_id, starts_at, ends_at, court_id, trainer_id, capacity,
+  booked_count: int,        // COUNT(attendees WHERE lesson_id = X AND state IN ('confirmed','pending'))
+  is_underbooked: bool,     // booked_count / capacity < 0.6 (config)
 }
```

**Endpoints affected:** `GET /series`, `GET /series/{id}`, `GET /lessons`, `GET /student/me/lessons`.

**Implementation:** One `.Include(x => x.Attendees).Select(...)` addition per query. Performance: watch for N+1 — use grouped aggregate subquery.

**Tests:** verify counts across confirmed/pending/declined states; ensure canceled lessons excluded.

---

### 1.3 — Trainer load + weekly capacity

**UI:** `TrainersP` cards → "Lesuren/w 14/16", capacity bar, "bijna vol" / "ruimte over" label.

**Schema addition**
```diff
 Trainer {
   id, first_name, last_name, email, status,
+  weekly_capacity_hours: int = 16,   // coach sets in Settings
 }
```
Migration: add column with default `16`.

**New endpoint**
```
GET /trainers/{id}/load?week=2026-W17
→ 200 { hours_booked: decimal, hours_capacity: int, series_count: int }
```
Or extend `GET /trainers` to include a `current_week_load` block. Prefer the extension for list performance.

**Tests:** DST edge (weeks bridging DST change), trainers with zero lessons.

---

### 1.4 — Trainer rating source

**UI:** `TrainersP` → "4.8 ★" field.

**Open question — see `OPEN_DECISIONS.md` Q3.** Either:
- Reuse existing post-lesson feedback (if it exists) → aggregate, no schema change.
- Create new `TrainerReview` table → blocks this feature for Phase 2.

Do not fabricate a rating. If no source exists, ship without the rating chip.

---

### 1.5 — Dashboard sparklines

**UI:** Dashboard right rail → three mini charts (lessons/week, occupancy, outstanding).

**New endpoint**
```
GET /dashboard/metrics?weeks=7
→ 200 {
  lessons: { week: "2026-W11", value: 24 }[],
  occupancy_pct: { week: "2026-W11", value: 70 }[],
  outstanding_cents: { week: "2026-W11", value: 20000 }[],
}
```

**Implementation:** Aggregate from existing lesson + payment tables. Cache per org for 5 min.

---

### 1.6 — Club edit + invite resend

**Missing CRUD endpoints.**
```
PATCH /clubs/{id}      body: { name?, address?, postal_code?, city? }
POST  /trainers/{id}/resend-invite   → 204
```
Invite resend: generates new token, expires old, sends email. Rate-limit: 1/5min per trainer.

---

### 1.7 — .ics generation on confirmation

**UI:** Confirmation success → "Voeg toe aan agenda" button.

**New endpoint**
```
GET /confirm/{token}/calendar.ics
→ 200 Content-Type: text/calendar
```
Generate a single-event VEVENT with: summary (series name), location (court + club), start/end, organizer (trainer email), UID = lesson_id, SEQUENCE = 0.

**Library:** `Ical.Net` or manually-built string (one event is trivial).

---

### 1.8 — Reschedule / swap request workflow

**UI:** Student portal "Vraag ruil" button; Coach Dashboard inbox item; approve/decline.

**New entity**
```
RescheduleRequest {
  id, student_id, lesson_id,
  requested_alternative_lesson_id?,   // nullable: "any other slot"
  reason: string,
  state: "pending" | "approved" | "declined" | "withdrawn",
  created_at, resolved_at?, resolved_by_coach_id?
}
```

**Endpoints**
```
POST /student/lessons/{id}/reschedule   body: { alternative_lesson_id?, reason }
PATCH /coach/reschedule-requests/{id}   body: { state: "approved" | "declined", note? }
```

Approved state: swap enrolment on lessons + send both parties a confirmation email. Declined: email student. Integrates into Inbox (1.1).

**Tests:** capacity guard on alternative, concurrent-approval race, withdrawn state.

---

## Phase 2 — new features / bigger investments

| Feature | UI | Scope |
|---|---|---|
| Google SSO | Register / Login | OAuth 2.0 client, user-linking table, callback route. Standard .NET OpenIdConnect middleware. |
| Payconiq payment | Confirmation | Merchant account, webhook for payment status, new `Payment.method` enum value, reconciliation job. |
| Weekly-template schema | Lesson wizard step 3 | **See `OPEN_DECISIONS.md` Q2.** Likely introduce `SeriesPattern` + `PatternException` entities and `Lesson.generated_from_pattern_id` FK. Migration regenerates existing lesson rows. |
| Extra Settings categories | Settings subnav | Each ("Team & rollen", "Standaardprijzen", "E-mailsjablonen", "Betaalmethodes", "Notificaties", "Facturatie", "Integraties", "Data export") is a separate feature. Design shows the IA; implementation is out of scope for this handoff. |
| Skill/level self-assessment on public enrolment | EnrollP | New entity `StudentAssessment` + admin-configurable rubric. |

---

## General notes for Claude Code

- **Do not** introduce a new ORM, API framework, or auth library. Match what's already in the repo.
- **Do not** break existing API consumers. All DTO changes in Phase 1 are additive.
- Feature-flag Phase 1 changes behind `Feature.RedesignV2` so product can A/B if desired.
- Every new endpoint: add integration test + OpenAPI annotation.
- Every DB migration: include a `Down()` that is actually tested.
