# Phasing

Ship in order. Do not skip phases. Each task has acceptance criteria — a PR merges only when all boxes are checked.

---

## Phase 0 — visual overhaul (pure FE, no backend)

**Goal:** ship ~60% of the visual upgrade behind the existing API. Zero backend risk. Target 1–2 weeks.

- [ ] **0.1** Add design tokens to `tokens.css` (see `FRONTEND_NOTES.md`)
- [ ] **0.2** Add shared components: `<Mono>`, `<SlashLabel>`, `<InkHeroCard>`, `<StatStrip>`, `<OccupancyBar>`, `<CountdownStrip>`, `<CourtLines>`
- [ ] **0.3** Redesign `Login.razor` (ink side panel, testimonial, role segment)
- [ ] **0.4** Redesign `Register.razor` (two-group form, password-rule chips)
- [ ] **0.5** Redesign `Student/Login.razor` (role switcher, "Stap 1 van 2" card)
- [ ] **0.6** Redesign `Dashboard.razor` **without BE data** — use existing endpoints; stat strip shows whatever totals are already available; inbox/sparklines stubbed behind empty states
- [ ] **0.7** Redesign `Planning.razor` calendar visuals + ink summary bar (counts from existing data)
- [ ] **0.8** Redesign `Series/Index.razor` as table view (use existing `GET /series`)
- [ ] **0.9** Redesign `Trainers.razor` as card grid (hide load/rating blocks until Phase 1)
- [ ] **0.10** Redesign `Student/Dashboard.razor` with ink hero
- [ ] **0.11** Redesign `Public/Enroll.razor` with smart-default availability (FE only)
- [ ] **0.12** Redesign `Public/Confirm.razor` with countdown, ink slot card; remove disabled Online option

**Acceptance:** every existing user flow still works; no API contract changes; visual QA signs off against the design canvas.

---

## Phase 1 — occupancy, inbox, load metrics (additive BE)

**Goal:** light up the operational data the redesign promises. Target 2–3 weeks.

- [ ] **1.1** `GET /dashboard/inbox` — unified priority feed (BE 1.1)
- [ ] **1.2** Add `enrolled_count` / `booked_count` / `is_underbooked` to Series & Lesson DTOs (BE 1.2)
- [ ] **1.3** Add `weekly_capacity_hours` to Trainer; `GET /trainers/{id}/load` (BE 1.3)
- [ ] **1.4** Trainer rating — only if source exists (see `OPEN_DECISIONS.md` Q3)
- [ ] **1.5** `GET /dashboard/metrics?weeks=7` for sparklines (BE 1.5)
- [ ] **1.6** `PATCH /clubs/{id}` + `POST /trainers/{id}/resend-invite` (BE 1.6)
- [ ] **1.7** `.ics` generation + "Voeg toe aan agenda" on confirmation success (BE 1.7)
- [ ] **1.8** Reschedule request workflow end-to-end (BE 1.8) — includes new coach inbox handling, "Vraag ruil" on student portal, approve/decline UI

**Acceptance for each:** BE endpoint has integration test; FE consumes real data (remove Phase-0 stubs); feature-flagged off by default; reviewed with product.

---

## Phase 2 — new features

**Goal:** the items that are redesigns of *missing* capabilities. No fixed order; pick by business value.

- [ ] **2.1** Google SSO on Register/Login (BE Phase 2)
- [ ] **2.2** Payconiq (or other online) payment on Confirmation (BE Phase 2) — remove "cash-only" constraint
- [ ] **2.3** **Decide** weekly-template schema (`OPEN_DECISIONS.md` Q2), then ship Wizard step 3 collapse-by-template UI
- [ ] **2.4** Settings IA expansion — one feature per subnav item, tracked as separate epics:
  - [ ] Mijn profiel
  - [ ] Team & rollen
  - [ ] Standaardprijzen
  - [ ] E-mailsjablonen
  - [ ] Betaalmethodes
  - [ ] Notificaties
  - [ ] Facturatie
  - [ ] Integraties
  - [ ] Data export
- [ ] **2.5** Skill/level self-assessment on public enrolment

---

## Cross-phase hygiene

- [ ] Feature-flag Phase 0 + 1 under `Feature.RedesignV2`
- [ ] Keep the existing `/v1` API stable; all Phase 1 additions are additive
- [ ] Update OpenAPI spec with each BE change
- [ ] Visual regression tests (Playwright) on the 12 primary screens after Phase 0
