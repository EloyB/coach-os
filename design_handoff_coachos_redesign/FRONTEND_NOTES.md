# Frontend notes

Which existing routes/components map to which redesigned screen, and how to avoid parallel implementations.

**Assumed stack:** Blazor Server / Razor components (inferred from `TennisBeheer.*` project naming — confirm).

---

## Route & component mapping

| Redesigned screen | Existing route (likely) | Existing component to edit in place | Notes |
|---|---|---|---|
| Login | `/auth/login` | `Pages/Auth/Login.razor` | Swap entire layout to 44/56 split; preserve form-post binding |
| Register | `/auth/register` | `Pages/Auth/Register.razor` | Split into two visual groups (org / personal) inside one form |
| Student login | `/student/login` | `Pages/Student/Login.razor` | Add role-switcher pill; keep magic-link POST |
| Dashboard ("Vandaag") | `/` or `/dashboard` | `Pages/Dashboard.razor` | Rename title to "Vandaag"; replace stat tiles with ink stat strip + inbox + timeline + sparklines |
| Planning | `/series/{id}/planning` or `/calendar` | `Pages/Series/Planning.razor` | Keep routing; replace grid implementation; add ink summary bar on top |
| Lesreeksen | `/series` | `Pages/Series/Index.razor` | Replace card grid with table; add view-switcher (Tabel / Kaarten / Kalender) — only "Tabel" functional in Phase 0 |
| Trainers | `/trainers` | `Pages/Trainers.razor` | Replace list with 2-col card grid; add load/rating fields from BE 1.3/1.4 |
| Student portal | `/student` | `Pages/Student/Dashboard.razor` | Ink hero card for next lesson; list of upcoming lessons below |
| Settings (Tennis clubs) | `/settings/clubs` | `Pages/Settings/Clubs.razor` | Add subnav; enrich list rows with stats from BE 1.2 aggregate |
| Public enrolment | `/enroll/{token}` | `Pages/Public/Enroll.razor` | Hero + sticky summary footer; smart-default availability FE-only |
| Slot confirmation | `/confirm/{token}` | `Pages/Public/Confirm.razor` | Countdown strip, ink slot card, payment method radio list |
| Lesson wizard step 3 | `/series/new/confirm` | `Pages/Series/Wizard/Step3.razor` | Collapse 12×week grid into 1 template + exceptions list — **depends on BE Phase 2** |

**Confirm all paths before editing.** If the repo uses a different structure, map the design to whatever exists. Do not create new routes unless explicitly called for.

---

## Do not create parallel implementations

❌ `Dashboard.razor` + `DashboardV2.razor`
✅ Replace `Dashboard.razor`, gate with `@if (FeatureFlags.RedesignV2)` if a gradual rollout is needed

Reasoning: two implementations double the maintenance load and make bugfixes ambiguous.

---

## Component library additions

Create these shared components **once**, reuse everywhere:

| Component | Location (suggested) | Used by |
|---|---|---|
| `<Mono>` | `Shared/Typography/Mono.razor` | Every number, date, timestamp, ID |
| `<SlashLabel>` | `Shared/Typography/SlashLabel.razor` | Section headers: `/planning`, `/vraagt-actie` |
| `<InkHeroCard>` | `Shared/Cards/InkHeroCard.razor` | Confirmation slot, Dashboard summary, Student next-lesson |
| `<StatStrip>` | `Shared/Cards/StatStrip.razor` | Dashboard top strip, Planning summary bar, Wizard summary |
| `<OccupancyBar>` | `Shared/OccupancyBar.razor` | Lesreeksen table, Trainer load, Planning capacity |
| `<Sparkline>` | `Shared/Charts/Sparkline.razor` | Dashboard right rail |
| `<CountdownStrip>` | `Shared/CountdownStrip.razor` | Confirmation expiry |
| `<CourtLines>` | `Shared/Decoration/CourtLines.razor` | Faint SVG pattern on ink surfaces |

Design tokens go in `wwwroot/css/tokens.css`:

```css
:root {
  --ink: #161513;
  --paper: #fdfcf9;
  --canvas: #f5f4f1;
  --rule: #e7e4dc;
  --green: #2D5016;
  --lime: #D0FF14;
  --warn: #F59E0B;
  --urgent: #DC2626;
  --font-ui: 'Inter', system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, monospace;
  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 14px;
  --radius-xl: 16px;
}
```

If the codebase already has a token file, extend it; don't duplicate.

---

## Copy (Dutch) — non-negotiable strings

Preserve these exactly as written in the HTML — they're product-tested, not translations:

- `Vraagt actie` (Dashboard priority row label)
- `Plan bevestigen` / `Weigeren` (Confirmation CTAs)
- `Ik kom` / `Vraag ruil` (Student portal CTAs)
- `Reeks aanmaken` (Wizard final CTA)
- `Magische link per e-mail` (Student login alt)
- `bijna vol` / `ruimte over` / `gezonde belasting` (Trainer load labels)
- `/slash-prefix/` mono labels (always lowercase)

---

## Things to remove

- Current Dashboard's four colored stat cards (Inschrijvingen / Trainers / Reeksen / Lessen deze week) — replaced by ink stat strip + priority row
- Current Lesreeksen card grid — replaced by table view (keep grid code as dead branch only if view-switcher needs it)
- Current login's three feature bullets — replaced by single testimonial quote
- `AwaitingConfirmation` / `Proposed` status pills on Student portal — replaced with human copy ("Wacht op bevestiging" / "Ruil aangeboden")
- Disabled "Online (binnenkort)" payment option on Confirmation — remove until it ships (see BACKEND Phase 2)

---

## Accessibility reminders

- Every status indicator that uses color (green/amber/red dots, lime accents) must have a text equivalent
- Mono numeric fields: set `font-variant-numeric: tabular-nums` so digits line up in tables
- Hero ink surfaces: ensure text contrast ≥ 4.5:1 against `#161513`
- Confirmation countdown must also announce via `aria-live="polite"` as it ticks
