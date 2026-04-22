# Open decisions

**Stop. Get answers from the product owner before writing code.** Guessing on these will force a rewrite.

---

## Q1 — Stack confirmation

**Question:** Is the codebase Blazor Server (.NET) as inferred from `TennisBeheer.*` file naming? Or Blazor WASM, or something else (Next.js, SvelteKit, etc.)?

**Why it matters:** every component location, routing pattern, and state-management choice in `FRONTEND_NOTES.md` assumes Blazor Server. If the stack is different, remap before starting Phase 0.

**Blocks:** all FE work.

**Answer:** __________

---

## Q2 — Weekly-template schema for lesson wizard

**Question:** For a recurring lesson series (e.g. "every Monday 18:00 for 12 weeks, except Easter week"), how should lessons be stored?

**Option A — Materialize on series creation (current, assumed):**
- Creating a 12-week series INSERTs 36 `Lesson` rows immediately.
- Exceptions = delete/edit individual rows.
- ✅ Simple to query. ✅ Current behavior.
- ❌ UI has to diff 36 rows every render to detect "which weeks deviate from the template."

**Option B — Pattern + overrides:**
- New entity `SeriesPattern { series_id, day_of_week, start_time, duration, court_id, trainer_id, recurs_until }`.
- New entity `PatternException { pattern_id, week_number, type: "cancelled" | "modified", overrides? }`.
- `Lesson` rows still exist for attendance/capacity, but carry `generated_from_pattern_id`.
- ✅ UI collapse is trivial. ✅ Bulk edits (swap trainer for remaining weeks) are one UPDATE.
- ❌ Migration required for existing data. ❌ Two sources of truth to reconcile.

**Recommended:** Option B — the UI clarity gain is significant and bulk-edit becomes possible. But migrating existing lessons is non-trivial.

**Blocks:** Phase 2.3 (wizard step 3 redesign). Phase 0/1 do not depend on this.

**Answer:** __________

---

## Q3 — Trainer rating source

**Question:** Does CoachOS currently collect any post-lesson feedback or rating from students?

- **Yes, there's a `LessonFeedback` / `StudentReview` table** → aggregate `AVG(score)` per trainer, ship the rating chip in Phase 1.4.
- **No, nothing like that exists** → new feature. Decision: (a) ship `TrainerP` without the rating chip, (b) defer trainer-rating as a Phase 2 epic (review collection UI + moderation + aggregation), or (c) remove the chip from the design permanently.

**Blocks:** Phase 1.4 completeness. Not a hard blocker — ship Phase 1 without ratings if unclear.

**Answer:** __________

---

## Q4 — Online payment provider

**Question:** Which provider for the Confirmation "Online" payment option? The design shows "Payconiq" as a placeholder; is that the intended integration, or Stripe / Mollie / something else?

**Why it matters:** each provider has different webhook contracts, refund semantics, and Belgian/EU regulatory handling. Affects BE 2.2 scope significantly.

**Blocks:** Phase 2.2.

**Answer:** __________

---

## Q5 — Inbox severity thresholds

**Question:** The "Vraagt actie" inbox classifies items as `warn` (amber) vs `urgent` (red). What are the business rules?

Proposed defaults (coach/product to confirm):
- **Confirmation pending** → `urgent` if `<12h` to lesson, else `warn`
- **Series underbooked** → `urgent` if `<7 days` to start AND `<60% capacity`, else `warn`
- **Payment overdue** → `urgent` if `>14 days`, else `warn`
- **Reschedule request** → always `warn` (never urgent by itself)

**Blocks:** BE 1.1 implementation detail. Pick defaults and ship; tweak later.

**Answer:** __________

---

## Decisions already made (for the record)

- **Language:** Dutch throughout. No i18n in this phase.
- **Brand colors:** tennis-green `#2D5016` + lime `#D0FF14` are keeping their primary status. The ink `#161513` is a new neutral hero surface, not a replacement.
- **Mobile:** Student portal is the only screen expected to see heavy mobile use. Coach-side screens target desktop (responsive fallbacks welcome but not blocking).
- **Backward compatibility:** every Phase 1 DTO change is additive; no existing API consumer breaks.
