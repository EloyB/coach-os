# Group Scheduling / Planning System

## Context

After enrollment closes for a lesson series, the admin needs to assign enrollees to recurring weekly time slots. Enrollees can enroll as pre-formed groups (parent signs up 3 kids) or solo (optionally open to grouping). Each slot holds max 4 people. The system should auto-propose assignments based on availability preferences, then let the admin fine-tune before confirming.

---

## Data Model

### New Enums

| Enum | Values |
|------|--------|
| `SlotPreference` | Available=1, Preferred=2, Unavailable=3 |
| `ScheduleAssignmentStatus` | Proposed=1, Confirmed=2 |
| `PlanningStatus` | Enrollment=1, Planning=2, Scheduled=3 |

### New Entities

**TimeSlot** — recurring weekly slot for a series
- `LessonSerieId`, `DayOfWeek` (0-6), `StartTime`, `EndTime`, `CourtName`, `TrainerId`, `MaxCapacity` (default 4)
- Unique index on `(LessonSerieId, DayOfWeek, StartTime)`

**EnrollmentGroup** — links enrollees that must stay together
- `OrganizationId`, `LessonSerieId`, `Name` (auto: "Groep A"), `LeaderEnrollmentId`
- Navigation: `Members` (collection of Enrollment)

**TimeSlotPreference** — per enrollment availability
- `EnrollmentId`, `TimeSlotId`, `Preference` (SlotPreference)
- Unique index on `(EnrollmentId, TimeSlotId)`

**ScheduleAssignment** — algorithm output: who goes where
- `LessonSerieId`, `TimeSlotId`, `EnrollmentGroupId?`, `EnrollmentId?`, `Status`
- Exactly one of GroupId or EnrollmentId is set

### Modified Entities

**LessonSerie** — add: `PlanningStatus` (default Enrollment), nav props for TimeSlots, Groups, Assignments

**Enrollment** — add: `EnrollmentGroupId?`, `IsOpenToGrouping`, nav props for Group and TimeSlotPreferences

---

## Phases

### Phase 1: Data Model
Create entities, enums, EF configs, repository interfaces/implementations, migration.

**Files to create:**
- `Domain/Enums/SlotPreference.cs`, `ScheduleAssignmentStatus.cs`, `PlanningStatus.cs`
- `Domain/Entities/TimeSlot.cs`, `EnrollmentGroup.cs`, `TimeSlotPreference.cs`, `ScheduleAssignment.cs`
- `Domain/Interfaces/ITimeSlotRepository.cs`, `IEnrollmentGroupRepository.cs`, `IScheduleAssignmentRepository.cs`
- `Infrastructure/Persistence/Configurations/TimeSlotConfiguration.cs`, `EnrollmentGroupConfiguration.cs`, `TimeSlotPreferenceConfiguration.cs`, `ScheduleAssignmentConfiguration.cs`
- `Infrastructure/Repositories/TimeSlotRepository.cs`, `EnrollmentGroupRepository.cs`, `ScheduleAssignmentRepository.cs`

**Files to modify:**
- `Domain/Entities/LessonSerie.cs` — add PlanningStatus + nav props
- `Domain/Entities/Enrollment.cs` — add EnrollmentGroupId, IsOpenToGrouping + nav props
- `Infrastructure/Persistence/Configurations/EnrollmentConfiguration.cs` — add FK to group
- `Infrastructure/Persistence/ApplicationDbContext.cs` — add 4 DbSets
- `Infrastructure/DependencyInjection.cs` — register 3 new repos

---

### Phase 2: Time Slot Management (BE + FE)

Admin CRUD for weekly time slots on a series.

**Backend — new folder `Application/TimeSlots/`:**
- `DTOs/TimeSlotDto.cs`, `DTOs/SaveTimeSlotsRequest.cs`
- `Validators/SaveTimeSlotsRequestValidator.cs`
- `ITimeSlotService.cs`, `TimeSlotService.cs`
- Endpoints: `GET /lessonseries/{id}/timeslots`, `PUT /lessonseries/{id}/timeslots`, `GET /public/lessonseries/{id}/timeslots`

**Frontend — weekly calendar view:**
- `lib/api/timeSlots.ts` — API client
- `components/dashboard/time-slot-calendar.tsx` — weekly calendar with time axis (vertical) and days (horizontal). Slots render as positioned blocks at their actual time/day. Click empty cell to add, click slot to edit. Trainer color-coded by left border.
- Embed in existing `/dashboard/lessons/[id]/page.tsx` as new tab
- Reference mockup: `docs/mockups/01-time-slot-builder.html`

---

### Phase 3: Enhanced Enrollment (BE + FE)

Extend enrollment to accept availability preferences and group enrollment.

**Backend — modify `Application/Enrollments/`:**
- Extend `SubmitEnrollmentRequest` with: `timeSlotPreferences`, `enrollmentType` (solo/group), `isOpenToGrouping`, `groupMembers`
- New DTOs: `TimeSlotPreferenceDto`, `GroupMemberDto`, `EnrollmentWithPreferencesDto`
- Extend `SubmitEnrollmentRequestValidator` for new fields
- Extend `EnrollmentService.SubmitEnrollmentAsync`:
  - Save TimeSlotPreference records
  - If group: create EnrollmentGroup + member Enrollments
  - If solo: set IsOpenToGrouping
  - Capacity check accounts for group size
- New method: `GetSeriesEnrollmentsWithPreferencesAsync`

**Frontend — modify `/enroll/[seriesId]/page.tsx`:**
- Availability grid: rows = time slots, 3 radio options per row (Voorkeur/Beschikbaar/Niet beschikbaar)
- Enrollment type toggle: Solo vs Groep
- Group mode: dynamic member fields (name + email, up to 3 extra)
- Solo mode: "Ik sta open voor groepsindeling" checkbox

---

### Phase 4: Planning Algorithm + API

**The algorithm** (`Application/Planning/SchedulingAlgorithm.cs`) — pure function, no dependencies:

1. **Build preference matrix** — for groups: intersection of all members' preferences; for solos: individual preferences
2. **Lock pre-formed groups** — if a group has only one viable slot, assign immediately
3. **Lock uncontested slots** — if only one unit wants a slot, assign (iterate until stable)
4. **Auto-group open solos** — cluster by overlapping preferred slots, form groups up to MaxCapacity
5. **Assign remaining solos** — best available slot with capacity (prefer Preferred > Available)
6. **Flag conflicts** — unplaceable enrollments, oversubscribed slots

**Backend — new folder `Application/Planning/`:**
- DTOs: `PlanningOverviewDto`, `ScheduleAssignmentDto`, `PlanningConflictDto`, `UpdateAssignmentsRequest`, `CreateGroupRequest`
- `IPlanningService.cs`, `PlanningService.cs`
- Endpoints:
  - `POST /lessonseries/{id}/planning/generate` — run algorithm
  - `GET /lessonseries/{id}/planning` — get current state
  - `PUT /lessonseries/{id}/planning/assignments` — admin overrides
  - `POST /lessonseries/{id}/planning/groups` — create/merge groups
  - `DELETE /lessonseries/{id}/planning/groups/{groupId}` — dissolve group
  - `POST /lessonseries/{id}/planning/confirm` — finalize

---

### Phase 5: Schedule Confirmation + Lesson Generation

When admin confirms, `PlanningService.ConfirmScheduleAsync`:
1. Calculate all dates from series StartDate→EndDate for each slot's DayOfWeek
2. For each assignment × each date: create a `Lesson` record (date, times, court, trainer from TimeSlot)
3. Set all assignments to Confirmed, series PlanningStatus to Scheduled
4. Single transaction

Note: Enrollments stay linked to the series via `LessonSerieId`. The `ScheduleAssignment` captures who is in which slot. Per-lesson attendance tracking is a future feature.

---

### Phase 6: Frontend — Planning Dashboard

**New page: `/dashboard/lessons/[id]/planning/page.tsx`**

Layout: Same weekly calendar as the time slot builder (time vertical, days horizontal), but slots now show assigned people inside them. Right sidebar with unassigned enrollees, groups, and capacity bars.

```
+--------------------------------------------------+
| <- Terug  [Opnieuw genereren] [Bevestigen]       |
+--------------------------------------------------+
|     | Ma      | Di | Wo       | Do | Vr    | Za  |
|-----+---------+----+----------+----+-------+-----|
|09:00| [Grp A] |    |          |    |       |     |
|     | EC LC LC|    |          |    |       |     |  SIDEBAR:
|10:00|         |    |          |    |       |     |  - Niet toegewezen (2)
|10:30| [NW JM] |    |          |    |       |     |  - Groepen
|     | 2/4     |    |          |    |       |     |  - Capaciteit bars
|12:00|         |    |          |    |       |     |
|13:00|         |    | [AD SJ]  |    |       |     |
|     |         |    | ⚠ 2/4   |    |       |     |
|14:30|         |    | [Grp B]  |    |       |     |
|     |         |    | TH ML    |    |       |     |
|16:00|         |    |          |    | [LP]  |     |
|     |         |    |          |    | 1/4   |     |
+--------------------------------------------------+
```

- Color coding: green border = auto-resolved, amber border = suggestion, red = conflict
- Click slot to see detail panel / reassign
- Right sidebar: unassigned people with their availability badges, group management, capacity overview
- Reference mockup: `docs/mockups/03-planning-dashboard.html`

**Files:**
- `lib/api/planning.ts` — API client
- `components/dashboard/planning-calendar.tsx` — weekly calendar with assigned slots
- `components/dashboard/planning-sidebar.tsx` — unassigned, groups, capacity
- `components/dashboard/planning-slot-detail.tsx` — click-to-expand slot detail
- `components/dashboard/group-management-dialog.tsx` — create/merge groups
- Modify `/dashboard/lessons/[id]/page.tsx` — add planning navigation + PlanningStatus badge

---

## API Contract Summary

| Method | Path | Auth | Body |
|--------|------|------|------|
| GET | `/lessonseries/{id}/timeslots` | Yes | — |
| PUT | `/lessonseries/{id}/timeslots` | Yes | `SaveTimeSlotsRequest` |
| GET | `/public/lessonseries/{id}/timeslots` | No | — |
| POST | `/public/lessonseries/{id}/enroll` | No | Extended `SubmitEnrollmentRequest` |
| POST | `/lessonseries/{id}/planning/generate` | Yes | — |
| GET | `/lessonseries/{id}/planning` | Yes | — |
| PUT | `/lessonseries/{id}/planning/assignments` | Yes | `UpdateAssignmentsRequest` |
| POST | `/lessonseries/{id}/planning/groups` | Yes | `CreateGroupRequest` |
| DELETE | `/lessonseries/{id}/planning/groups/{gid}` | Yes | — |
| POST | `/lessonseries/{id}/planning/confirm` | Yes | — |

---

## Phase Dependencies

```
Phase 1 (Data Model)
  ├── Phase 2 (Time Slots BE+FE) ──────────┐
  └── Phase 3 (Enhanced Enrollment BE+FE) ──┤
                                             ├── Phase 4 (Algorithm + API)
                                             │     ├── Phase 5 (Confirm + Lesson Gen)
                                             │     └── Phase 6 (Planning Dashboard FE)
```

Phases 2 and 3 can run in parallel after Phase 1.

---

## Testing

- `Tests/Services/SchedulingAlgorithmTests.cs` — pure algorithm unit tests (no mocks needed)
  - All groups, all solos, mixed, empty, oversubscribed, single-option forced assignments
- `Tests/Services/PlanningServiceTests.cs` — service tests with mocked repos
- `Tests/Services/TimeSlotServiceTests.cs` — CRUD validation
- Seed script: update `backend/Scripts/seed-demo-data.sh` to create time slots and enrollments with preferences

---

## Dutch Translations (messages/nl.json)

New sections: `timeSlots`, `planning`, `enrollment` (extended) — covering all UI labels for slot builder, availability grid, planning dashboard, group management.
