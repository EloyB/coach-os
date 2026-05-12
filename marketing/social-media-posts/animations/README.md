# CoachOS Animations

Remotion project for generating Instagram carousel MP4 animations.

## Tech

- [Remotion 4](https://www.remotion.dev/) — programmatic video via React
- TypeScript
- Output: 1080×1080 MP4 @ 30fps, H.264 codec (Instagram-native)

## First-time setup

You only need to do this once. Requires Node 18+ and a working `npm`.

```bash
cd "/Users/eloyboone/Documents/Studio-Swyft/coach-os/marketing/social-media-posts/animations"
npm install
```

Remotion will prompt to download a Chromium build the first time you render — that's normal. Allow it.

## Preview animations live (interactive editor)

```bash
npm run studio
```

Opens the Remotion Studio in your browser at `http://localhost:3000`. Scrub through frames, see live edits as you change code. Best way to iterate on timing and copy.

## Render an animation to MP4

```bash
npm run render:auto-planner
```

Output lands in `out/auto-planner.mp4`. ~30 seconds on a modern Mac.

Other compositions (will exist after you sign off on the auto-planner):

```bash
npm run render:create-lessenreeks
npm run render:enrollment-form
npm run render:student-enrollment

# Or render all four at once:
npm run render:all
```

## How a composition is structured

Each animation is a single React component in `src/compositions/`. The `BrandFrame` wrapper provides the dark background, court-line decoration, monogram, and lime accent strip — every composition uses it for visual consistency. Inside the frame, you compose animated content using:

- `useCurrentFrame()` — current frame number in the clip
- `interpolate(frame, [from, to], [0, 1])` — smooth value over a range of frames
- `spring()` — bouncy easing for "pop in" moments

Edit `src/brand.ts` to change brand colors / fonts globally.

Edit `src/Root.tsx` to register new compositions.

## Posting workflow

1. Render the MP4: `npm run render:auto-planner`
2. AirDrop or transfer `out/auto-planner.mp4` to your phone
3. In Instagram, create new carousel post → upload videos + static cards in order
4. Add caption from the matching `.md` file in `../drafts/`

## Troubleshooting

**"Chromium download failed"** — happens behind firewalls; rerun `npm install` on a different network or set `REMOTION_BROWSER_EXECUTABLE` to your local Chrome path.

**Fonts look wrong** — the inline animations fall back to system fonts if Inter isn't installed locally. Install Inter from [rsms.me/inter](https://rsms.me/inter/) for pixel-perfect rendering matching the rest of the brand.

**Render is slow** — drop `Config.setConcurrency(4)` in `remotion.config.ts` to a lower number if your Mac chokes; raise it if you have a beefier CPU.
