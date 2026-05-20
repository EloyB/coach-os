import { useCurrentFrame, useVideoConfig, interpolate, spring, Easing } from 'remotion';
import { BrandFrame } from '../components/BrandFrame';
import { AppWindow, WINDOW } from '../components/AppWindow';
import { SidebarMock } from '../components/SidebarMock';
import { Cursor } from '../components/Cursor';
import {
  StepIndicator,
  StepBasisinfo,
  StepPlanning,
  StepValidatie,
  WizardHeader,
  type ActiveField,
} from '../components/WizardSteps';
import { LessonsListEmpty, LessonsListPopulated } from '../components/LessonsListView';
import { COLORS, FONTS } from '../brand';

// "Aanmaken van een lessenreeks" — 270 frames @ 30fps = 9 seconds.
// Carousel slide 2. See plan at ~/.claude/plans/alright-for-one-of-fuzzy-clarke.md
//
// Timeline reference (all values in frames):
//   0–15    entry: window+sidebar scale in, list-empty visible, cursor approaches button
//   15–22   cursor presses "+ Nieuwe lesreeks"
//   22–32   cross-fade list-empty → step 1
//   32–106  step 1 fill montage (typing, dropdowns, dates, press Volgende)
//   106–118 cross-fade step 1 → step 2
//   118–178 step 2 dwell (slots cascade 124–160, breathing room, then press Volgende ~174)
//   178–188 cross-fade step 2 → step 3
//   188–198 step 3 (cursor → "Lesreeks aanmaken", press 192)
//   196–230 success badge springs in + holds
//   230–248 cross-fade step3 + badge → list-populated
//   248–270 new row scale-in + glow + hold

const NAME_FULL = 'Voorjaarsreeks 2026';

// Cursor waypoints — coords are in the outer 1080×1080 canvas. The 'tip'
// of the cursor lands on (x, y). Easing between waypoints is in/out cubic.
type Waypoint = { f: number; x: number; y: number; pressed?: boolean };

const WAYPOINTS: Waypoint[] = [
  { f: 0,   x: 1140, y: 1140 },
  { f: 12,  x: 875,  y: 250 },
  { f: 18,  x: 875,  y: 250 }, // hover
  { f: 20,  x: 875,  y: 250, pressed: true },
  { f: 26,  x: 875,  y: 250 },
  // wait through cross-fade
  { f: 32,  x: 875,  y: 250 },
  // Step 1 — Naam
  { f: 38,  x: 650,  y: 428 },
  // type happens 38..60
  { f: 60,  x: 650,  y: 428 },
  // Prijs
  { f: 66,  x: 650,  y: 496 },
  { f: 67,  x: 650,  y: 496, pressed: true },
  { f: 70,  x: 650,  y: 496 },
  // Max
  { f: 76,  x: 650,  y: 565 },
  { f: 77,  x: 650,  y: 565, pressed: true },
  { f: 80,  x: 650,  y: 565 },
  // Club select
  { f: 86,  x: 650,  y: 633 },
  { f: 87,  x: 650,  y: 633, pressed: true },
  { f: 90,  x: 650,  y: 633 },
  // hover club option
  { f: 94,  x: 540,  y: 685 },
  { f: 95,  x: 540,  y: 685, pressed: true },
  { f: 98,  x: 540,  y: 685 },
  // Start date
  { f: 102, x: 530,  y: 702 },
  { f: 103, x: 530,  y: 702, pressed: true },
  { f: 105, x: 530,  y: 702 },
  // End date
  { f: 108, x: 770,  y: 702 },
  { f: 109, x: 770,  y: 702, pressed: true },
  { f: 110, x: 770,  y: 702 },
  // Deadline
  { f: 112, x: 650,  y: 770 },
  { f: 113, x: 650,  y: 770, pressed: true },
  { f: 115, x: 650,  y: 770 },
  // Volgende (step 1)
  { f: 118, x: 833,  y: 844 },
  { f: 120, x: 833,  y: 844, pressed: true },
  { f: 124, x: 833,  y: 844 },
  // hold during cross-fade
  { f: 130, x: 833,  y: 844 },
  // Step 2 — Volgende button (shifted later: slots get more dwell time)
  { f: 172, x: 833,  y: 864 },
  { f: 174, x: 833,  y: 864, pressed: true },
  { f: 177, x: 833,  y: 864 },
  // Step 3 — Lesreeks aanmaken button
  { f: 188, x: 845,  y: 864 },
  { f: 192, x: 845,  y: 864, pressed: true },
  { f: 198, x: 845,  y: 864 },
  // drift off during success
  { f: 222, x: 950,  y: 920 },
  { f: 270, x: 950,  y: 920 },
];

function cursorAt(frame: number, waypoints: Waypoint[]): { x: number; y: number; pressed: boolean } {
  if (frame <= waypoints[0].f) return { x: waypoints[0].x, y: waypoints[0].y, pressed: !!waypoints[0].pressed };
  for (let i = 1; i < waypoints.length; i++) {
    const prev = waypoints[i - 1];
    const curr = waypoints[i];
    if (frame <= curr.f) {
      const span = curr.f - prev.f;
      const t = span === 0 ? 1 : (frame - prev.f) / span;
      const eased = Easing.bezier(0.25, 0.1, 0.25, 1)(t);
      return {
        x: prev.x + (curr.x - prev.x) * eased,
        y: prev.y + (curr.y - prev.y) * eased,
        pressed: !!curr.pressed,
      };
    }
  }
  const last = waypoints[waypoints.length - 1];
  return { x: last.x, y: last.y, pressed: !!last.pressed };
}

function getActiveField(frame: number): ActiveField {
  if (frame >= 38 && frame < 64) return 'name';
  if (frame >= 64 && frame < 74) return 'price';
  if (frame >= 74 && frame < 84) return 'max';
  if (frame >= 84 && frame < 100) return 'club';
  if (frame >= 100 && frame < 107) return 'start';
  if (frame >= 107 && frame < 111) return 'end';
  if (frame >= 111 && frame < 118) return 'deadline';
  return null;
}

const SuccessBadge: React.FC<{ progress: number; scale: number }> = ({ progress, scale }) => (
  <div
    style={{
      position: 'absolute',
      top: 540,
      left: 0,
      right: 0,
      display: 'flex',
      justifyContent: 'center',
      zIndex: 50,
      opacity: progress,
      transform: `scale(${scale})`,
      pointerEvents: 'none',
    }}
  >
    <div
      style={{
        background: COLORS.tennisLime,
        color: COLORS.ink,
        padding: '18px 30px',
        borderRadius: 16,
        fontSize: 24,
        fontWeight: 700,
        fontFamily: FONTS.sans,
        display: 'flex',
        alignItems: 'center',
        gap: 14,
        boxShadow: '0 20px 50px -10px rgba(208,255,20,0.55), 0 8px 20px rgba(0,0,0,0.25)',
      }}
    >
      <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke={COLORS.ink} strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round">
        <polyline points="20 6 9 17 4 12" />
      </svg>
      <span>Klaar in no-time</span>
    </div>
  </div>
);

export const CreateLesreeks: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // Cursor
  const cur = cursorAt(frame, WAYPOINTS);

  // Cursor blink at ~2 Hz
  const cursorBlink = Math.floor(frame / 15) % 2 === 0;

  // ────────────────────────────────────────────────────────────────────
  //  State opacities (cross-fades)
  // ────────────────────────────────────────────────────────────────────
  // list-empty: visible 0..32 (fade out 22..32 ends ramp)
  const listEmptyOpacity = interpolate(frame, [22, 32], [1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  // step1: fade in 22..32, fade out 106..118
  const step1Opacity =
    interpolate(frame, [22, 32], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }) *
    interpolate(frame, [106, 118], [1, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  // step2: fade in 106..118, fade out 178..188
  const step2Opacity =
    interpolate(frame, [106, 118], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }) *
    interpolate(frame, [178, 188], [1, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  // step3: fade in 178..188, fade out 230..248
  const step3Opacity =
    interpolate(frame, [178, 188], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }) *
    interpolate(frame, [230, 248], [1, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  // list-populated: fade in 230..248, stays
  const listPopOpacity = interpolate(frame, [230, 248], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // ────────────────────────────────────────────────────────────────────
  //  Form values (Step 1)
  // ────────────────────────────────────────────────────────────────────
  // Name: type chars from frame 38..60 (22 frames, 19 chars)
  const charsTyped = Math.max(0, Math.floor(interpolate(frame, [38, 60], [0, NAME_FULL.length], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })));
  const nameValue = NAME_FULL.slice(0, charsTyped);

  // Price: ticks 0..150 between frames 66..74
  const priceValue = frame < 66 ? null : Math.round(interpolate(frame, [66, 74], [0, 150], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }));

  // Max: ticks 0..15 between frames 76..82
  const maxValue = frame < 76 ? null : Math.round(interpolate(frame, [76, 82], [0, 120], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }));

  // Club: dropdown open 87..98; selected at 98
  const showClubDropdown = frame >= 87 && frame < 98;
  const clubValue = frame >= 98 ? 'TC De Linde' : '';

  // Dates appear after each click
  const startDate = frame >= 104 ? '01-03-2026' : '';
  const endDate = frame >= 110 ? '30-05-2026' : '';
  const deadline = frame >= 114 ? '22-02-2026' : '';

  // Active field for focus styling
  const activeField = getActiveField(frame);

  // Volgende (step 1) press
  const volgende1Pressed = frame >= 120 && frame < 124;
  const volgende1Hover = frame >= 116 && frame < 130;

  // Step 2 slots cascade — 11 slots over frames 124..160 (slower cascade)
  const slotsRevealed = Math.max(
    0,
    Math.floor(interpolate(frame, [124, 160], [0, 11], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })),
  );
  const volgende2Pressed = frame >= 174 && frame < 178;

  // Step 3 button press
  const aanmakenPressed = frame >= 192 && frame < 198;

  // Success badge spring (starts at 196 — right when button is pressed)
  const badgeScale = spring({
    frame: frame - 196,
    fps,
    config: { damping: 13, mass: 0.7, stiffness: 130 },
    durationInFrames: 24,
  });
  const badgeOpacity =
    interpolate(frame, [196, 208], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }) *
    interpolate(frame, [228, 244], [1, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });

  // Step-indicator line progress (for the step transitions)
  let s1Step = 1 as 1 | 2 | 3;
  let s1Line = 0;
  if (frame < 106) {
    s1Step = 1;
    s1Line = 0;
  } else if (frame < 118) {
    s1Step = 1;
    s1Line = interpolate(frame, [106, 118], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  } else {
    s1Step = 2;
  }
  // step2 indicator
  let s2Step = 2 as 1 | 2 | 3;
  let s2Line = 0;
  if (frame < 178) {
    s2Step = 2;
  } else if (frame < 188) {
    s2Step = 2;
    s2Line = interpolate(frame, [178, 188], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  } else {
    s2Step = 3;
  }

  // List populated: row scale-in + glow
  const rowScaleProgress = interpolate(frame, [242, 260], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });
  const rowGlowProgress =
    interpolate(frame, [246, 256], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }) *
    interpolate(frame, [262, 270], [1, 0.55], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });

  return (
    <BrandFrame step="02 / 05" title="Lessenreeks aanmaken">
      <AppWindow entryFrame={0}>
        <SidebarMock />

        {/* Main content area — overlay each state with opacity */}
        <div
          style={{
            position: 'relative',
            flex: 1,
            background: COLORS.canvas,
            overflow: 'hidden',
          }}
        >
          {/* List empty */}
          <div
            style={{
              position: 'absolute',
              inset: 0,
              opacity: listEmptyOpacity,
              display: 'flex',
            }}
          >
            <LessonsListEmpty
              newButtonHover={frame >= 14 && frame < 30}
              newButtonPressed={frame >= 19 && frame < 25}
            />
          </div>

          {/* Step 1 */}
          <div
            style={{
              position: 'absolute',
              inset: 0,
              opacity: step1Opacity,
              padding: '28px 32px',
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
            }}
          >
            <WizardHeader />
            <StepIndicator currentStep={s1Step} lineProgress={s1Line} />
            <div style={{ maxWidth: 480, width: '100%', alignSelf: 'center' }}>
              <StepBasisinfo
                values={{
                  name: nameValue,
                  price: priceValue,
                  maxRegistrations: maxValue,
                  tennisClub: clubValue,
                  startDate,
                  endDate,
                  deadline,
                }}
                activeField={activeField}
                cursorBlink={cursorBlink}
                showClubDropdown={showClubDropdown}
                buttonPressed={volgende1Pressed}
                buttonHover={volgende1Hover}
              />
            </div>
          </div>

          {/* Step 2 */}
          <div
            style={{
              position: 'absolute',
              inset: 0,
              opacity: step2Opacity,
              padding: '28px 32px',
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
            }}
          >
            <WizardHeader />
            <StepIndicator currentStep={s2Step} lineProgress={s2Line} />
            <StepPlanning slotsRevealed={slotsRevealed} buttonPressed={volgende2Pressed} />
          </div>

          {/* Step 3 */}
          <div
            style={{
              position: 'absolute',
              inset: 0,
              opacity: step3Opacity,
              padding: '28px 32px',
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
            }}
          >
            <WizardHeader />
            <StepIndicator currentStep={3} lineProgress={0} />
            <StepValidatie buttonPressed={aanmakenPressed} />
          </div>

          {/* List populated */}
          <div
            style={{
              position: 'absolute',
              inset: 0,
              opacity: listPopOpacity,
              display: 'flex',
            }}
          >
            <LessonsListPopulated glowProgress={rowGlowProgress} scaleProgress={rowScaleProgress} />
          </div>
        </div>
      </AppWindow>

      {/* Success badge — rendered over the AppWindow, inside BrandFrame */}
      {badgeOpacity > 0 && <SuccessBadge progress={badgeOpacity} scale={badgeScale} />}

      {/* Cursor — sits above everything */}
      <Cursor x={cur.x} y={cur.y} pressed={cur.pressed} />
    </BrandFrame>
  );
};
