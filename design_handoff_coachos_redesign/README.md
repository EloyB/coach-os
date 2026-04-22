# Handoff — CoachOS redesign

## Overview

This package contains a full visual and UX redesign of **CoachOS**, a Dutch-language SaaS for tennis- and padel-club coaches to manage lesson series, enrolments, schedules, trainers, and payments.

The redesign preserves the existing brand (tennis-green `#2D5016` + lime `#D0FF14`) but upgrades every screen's information hierarchy, density, and personality. It also covers pages and states the original product has but that were visually incomplete (Register, Student login, Settings, Public enrolment, Slot confirmation, the lesson-creation wizard).

## About the design files

Everything in this bundle is a **design reference created in HTML + React** — pixel-accurate prototypes showing intended look and behavior, not production code to copy directly.

The task is to **recreate these designs inside the CoachOS codebase**, using its established stack (assumed Blazor/.NET based on `TennisBeheer.*` file naming in the source — confirm before starting) and its existing component patterns. Do **not** ship the bundled `.jsx` files.

## Fidelity

**High-fidelity.** Colors, typography, spacing, borders, radii, and motion intent are final. Treat the HTML as the source of truth for visual specification; treat the four companion `.md` files (below) as the source of truth for backend and scope decisions.

## What's in this bundle

| File | Purpose |
|---|---|
| `README.md` | You are here. Orientation. |
| `BACKEND_CHANGES.md` | **Read first before writing BE code.** Maps every new UI affordance to its data requirement. |
| `FRONTEND_NOTES.md` | Which existing components/routes map to which redesigned screen, what to rename, what to delete. |
| `PHASING.md` | Ordered task list grouped into three phases (Phase 0 = pure FE, Phase 1 = small BE, Phase 2 = new features). Ship in order. |
| `OPEN_DECISIONS.md` | 5 yes/no questions that need a human before coding can start. Do not guess. |
| `CoachOS Design Critique.html` | The full design canvas. Open in a browser to explore. |
| `screens-*.jsx`, `annotations.jsx`, `design-canvas.jsx` | Component source for the canvas. Reference-only. |

## How to use this handoff

1. **Open `CoachOS Design Critique.html` in a browser.** Pan/zoom the canvas. Every screen is in there, labeled, with critique annotations beside it. The "Proposed" sections are what you're building toward.
2. **Read `OPEN_DECISIONS.md`** and get answers from the product owner before writing any code. Two of the questions block Phase 1.
3. **Read `PHASING.md`** top to bottom. Each phase has acceptance criteria. Work one phase at a time; do not jump ahead.
4. **Read `BACKEND_CHANGES.md`** for the row that matches the task you're on. Each row points at a specific DTO / endpoint / schema change. Implement the BE change, then the FE.
5. **Use `FRONTEND_NOTES.md`** to find the existing component to edit. Do not create a `DashboardNew.tsx` alongside the old `Dashboard.tsx`. Replace in place, behind a feature flag if rollout needs to be gradual.

## Brand / design system

- **Primary:** tennis-green `#2D5016` (sidebar, confirmed states, primary accents)
- **Accent:** lime `#D0FF14` (hero moments, CTAs on dark surfaces, `c/` monogram)
- **Ink:** `#161513` (dark hero cards, primary buttons, auth side panels)
- **Paper:** `#fdfcf9` surfaces, `#f5f4f1` canvas background, `#e7e4dc` rules
- **Warning:** `#F59E0B` (under-booked, capacity warnings), `#DC2626` (overdue, urgent)
- **Typography:** Inter (UI), JetBrains Mono (all numbers, dates, IDs, timestamps, slash-prefix labels like `/planning`, `/vraagt-actie`)
- **Radii:** 6px (small controls), 8–10px (inputs, pills), 12–14px (cards), 16px (full-bleed heroes)
- **Signature moves:**
  - `/mono-slash-prefix` uppercase labels above H1s
  - Dark-ink hero cards embedded in light pages (Confirmation slot, Dashboard stat strip, Planning summary)
  - Mono tabular numbers in every stat/table (`fontVariantNumeric: tabular-nums`)
  - `c/` monogram lockup (mono, lime-on-ink)
  - Faint "court-line" grid pattern (5–8% opacity) on dark surfaces

## Notes for Claude Code

- The existing codebase's UI kit takes priority over the HTML styling. Match spacing/radius tokens to whatever's already defined in the repo; don't hard-code the hex values from the design if equivalents exist.
- Dutch copy throughout. Don't translate to English unless asked.
- Every number in the UI is mono — this is non-negotiable. If the codebase doesn't have a `<Mono>` wrapper, add one.
- The critique annotations in the HTML canvas are for humans; don't leave them in the shipped product.
