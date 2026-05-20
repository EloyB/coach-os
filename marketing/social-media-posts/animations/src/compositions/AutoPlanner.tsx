import { useCurrentFrame, useVideoConfig, interpolate, spring, Easing } from 'remotion';
import { BrandFrame } from '../components/BrandFrame';
import { AppWindow } from '../components/AppWindow';
import { SidebarMock } from '../components/SidebarMock';
import { Cursor } from '../components/Cursor';
import {
  LessonSeriesDetailHeader,
  PlanningPageHeader,
  PlanningLegend,
  ScheduleGrid,
  type Slot,
} from '../components/PlanningView';
import { COLORS, FONTS } from '../brand';

// "Automatisch plannen" — 300 frames @ 30fps = 10 seconds.
// Carousel slide 5, brand-chrome step 04 / 04.
//
// Timeline:
//   0–18    AppWindow scales in: detail page header for "Voorjaarsreeks 2026"
//   18–40   cursor → "Plan lessen" button → press
//   40–55   cross-fade detail page → planning page
//   55–75   spinner + "Genereren…" label
//   75–217  cascade fill: 12 players land in slots one by one (12 frames apart)
//   217–245 lime stat badge springs in over the populated grid
//   245–300 hold

const SLOTS: Slot[] = [
  {
    day: 'Ma',
    time: '17:00',
    court: 'Baan 1',
    capacity: 4,
    players: [
      { name: 'Sven', level: 'beginner' },
      { name: 'Marit', level: 'beginner' },
    ],
  },
  {
    day: 'Ma',
    time: '18:30',
    court: 'Baan 1',
    capacity: 4,
    players: [
      { name: 'Tom', level: 'intermediate' },
      { name: 'Eva', level: 'intermediate' },
    ],
  },
  {
    day: 'Wo',
    time: '17:00',
    court: 'Baan 2',
    capacity: 4,
    players: [
      { name: 'Lars', level: 'expert' },
      { name: 'Lieve', level: 'expert' },
    ],
  },
  {
    day: 'Wo',
    time: '18:30',
    court: 'Baan 2',
    capacity: 4,
    players: [
      { name: 'Wout', level: 'beginner' },
      { name: 'Anke', level: 'beginner' },
    ],
  },
  {
    day: 'Vr',
    time: '17:00',
    court: 'Baan 1',
    capacity: 4,
    players: [
      { name: 'Bart', level: 'intermediate' },
      { name: 'Ine', level: 'intermediate' },
    ],
  },
  {
    day: 'Vr',
    time: '18:30',
    court: 'Baan 2',
    capacity: 4,
    players: [
      { name: 'Joris', level: 'expert' },
      { name: 'Mila', level: 'expert' },
    ],
  },
];

const TOTAL_PLAYERS = SLOTS.reduce((s, slot) => s + slot.players.length, 0);
const TOTAL_CAPACITY = SLOTS.reduce((s, slot) => s + slot.capacity, 0);

type Waypoint = { f: number; x: number; y: number; pressed?: boolean };

const WAYPOINTS: Waypoint[] = [
  { f: 0,   x: 1140, y: 1140 },
  // Approach "Plan lessen" button (top-right of detail header)
  { f: 16,  x: 913,  y: 265 },
  { f: 26,  x: 913,  y: 265 },
  { f: 28,  x: 913,  y: 265, pressed: true },
  { f: 34,  x: 913,  y: 265 },
  // Drift away during page transition
  { f: 55,  x: 990,  y: 380 },
  // Drift further off during cascade
  { f: 100, x: 1040, y: 700 },
  { f: 200, x: 1080, y: 950 },
  { f: 300, x: 1080, y: 950 },
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

const StatBadge: React.FC<{ progress: number; scale: number; assignedCount: number }> = ({
  progress,
  scale,
  assignedCount,
}) => (
  <div
    style={{
      position: 'absolute',
      left: 0,
      right: 0,
      bottom: 80,
      display: 'flex',
      justifyContent: 'center',
      pointerEvents: 'none',
      zIndex: 50,
      opacity: progress,
    }}
  >
    <div
      style={{
        background: COLORS.tennisLime,
        color: COLORS.ink,
        padding: '18px 28px',
        borderRadius: 16,
        fontSize: 22,
        fontWeight: 700,
        fontFamily: FONTS.sans,
        display: 'flex',
        alignItems: 'center',
        gap: 14,
        boxShadow: '0 20px 50px -10px rgba(208,255,20,0.55), 0 8px 20px rgba(0,0,0,0.25)',
        transform: `scale(${scale})`,
      }}
    >
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke={COLORS.ink} strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round">
        <polyline points="20 6 9 17 4 12" />
      </svg>
      <span>Planning klaar · {assignedCount} spelers ingepland</span>
    </div>
  </div>
);

export const AutoPlanner: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const cur = cursorAt(frame, WAYPOINTS);

  // ─── Page state opacities
  const detailOpacity = interpolate(frame, [40, 55], [1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const planningOpacity = interpolate(frame, [40, 55], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // ─── Plan lessen button state
  const planButtonHover = frame >= 14 && frame < 38;
  const planButtonPressed = frame >= 28 && frame < 34;

  // ─── Spinner active during initial generation period
  const generating = frame >= 55 && frame < 78;

  // ─── Per-player visibility (cascade)
  const PLAYER_INTERVAL = 12; // frames between player landings
  const PLAYER_FADE = 10;
  const CASCADE_START = 75;
  const playerVisibility: number[] = [];
  for (let i = 0; i < TOTAL_PLAYERS; i++) {
    const start = CASCADE_START + i * PLAYER_INTERVAL;
    const v = interpolate(frame, [start, start + PLAYER_FADE], [0, 1], {
      extrapolateLeft: 'clamp',
      extrapolateRight: 'clamp',
      easing: Easing.bezier(0.22, 1, 0.36, 1),
    });
    playerVisibility.push(v);
  }
  const assignedCount = playerVisibility.filter((v) => v > 0.5).length;

  // ─── Stat badge springs in at f=217
  const badgeScale = spring({
    frame: frame - 217,
    fps,
    config: { damping: 13, mass: 0.7, stiffness: 130 },
    durationInFrames: 28,
  });
  const badgeProgress = interpolate(frame, [217, 232], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <BrandFrame step="05 / 05" title="Automatisch plannen">
      <AppWindow entryFrame={0}>
        <SidebarMock />
        <div
          style={{
            position: 'relative',
            flex: 1,
            background: COLORS.canvas,
            padding: '28px 32px',
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
          }}
        >
          {/* Detail page (before-click) */}
          {detailOpacity > 0 && (
            <div style={{ position: 'absolute', inset: 0, padding: '28px 32px', opacity: detailOpacity }}>
              <LessonSeriesDetailHeader
                planLessonsHover={planButtonHover}
                planLessonsPressed={planButtonPressed}
              />
            </div>
          )}

          {/* Planning page */}
          {planningOpacity > 0 && (
            <div
              style={{
                position: 'absolute',
                inset: 0,
                padding: '28px 32px',
                opacity: planningOpacity,
                display: 'flex',
                flexDirection: 'column',
              }}
            >
              <PlanningPageHeader generating={generating} spinnerFrame={frame} />
              <PlanningLegend
                assignedCount={assignedCount}
                totalEnrollments={TOTAL_PLAYERS}
                totalSlots={SLOTS.length}
                totalCapacity={TOTAL_CAPACITY}
              />
              <ScheduleGrid slots={SLOTS} playerVisibility={playerVisibility} />
            </div>
          )}

          {/* Stat badge over the populated grid */}
          {badgeProgress > 0 && (
            <StatBadge progress={badgeProgress} scale={badgeScale} assignedCount={TOTAL_PLAYERS} />
          )}
        </div>
      </AppWindow>

      <Cursor x={cur.x} y={cur.y} pressed={cur.pressed} />
    </BrandFrame>
  );
};
