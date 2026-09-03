import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP } from '../brand';
import { FloatingCard } from '../FloatingCard';
import { SceneShell } from '../SceneShell';
import {
  PlanningLegend,
  PlanningPageHeader,
  ScheduleGrid,
  type Slot,
} from '../mocks/PlannerMock';
import type { PromoFormat } from '../layout';

// 16–24s. Heromoment: 12 spelers cascaden in het grid, badge veert erin.

const SLOTS: Slot[] = [
  {
    day: 'Ma', time: '17:00', court: 'Baan 1', capacity: 2,
    players: [
      { name: 'Sven', level: 'beginner' },
      { name: 'Marit', level: 'beginner' },
    ],
  },
  {
    day: 'Wo', time: '17:00', court: 'Baan 1', capacity: 2,
    players: [
      { name: 'Tom', level: 'intermediate' },
      { name: 'Eva', level: 'intermediate' },
    ],
  },
  {
    day: 'Vr', time: '17:00', court: 'Baan 2', capacity: 2,
    players: [
      { name: 'Lars', level: 'expert' },
      { name: 'Lieve', level: 'expert' },
    ],
  },
  {
    day: 'Ma', time: '18:30', court: 'Baan 2', capacity: 2,
    players: [
      { name: 'Wout', level: 'beginner' },
      { name: 'Anke', level: 'beginner' },
    ],
  },
  {
    day: 'Wo', time: '18:30', court: 'Baan 3', capacity: 2,
    players: [
      { name: 'Nina', level: 'intermediate' },
      { name: 'Jef', level: 'intermediate' },
    ],
  },
  {
    day: 'Vr', time: '18:30', court: 'Baan 3', capacity: 2,
    players: [
      { name: 'Roos', level: 'expert' },
      { name: 'Stan', level: 'expert' },
    ],
  },
];

const TOTAL_PLAYERS = SLOTS.reduce((acc, s) => acc + s.players.length, 0); // 12

const CASCADE_START = 64;
const CASCADE_INTERVAL = 9;

export const PlannerScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // Spinner tot de cascade start.
  const generating = frame >= 14 && frame < CASCADE_START;

  // Per speler 0..1 zichtbaarheid, gespreid.
  const playerVisibility = Array.from({ length: TOTAL_PLAYERS }, (_, i) => {
    const start = CASCADE_START + i * CASCADE_INTERVAL;
    return interpolate(frame, [start, start + 10], [0, 1], {
      extrapolateLeft: 'clamp',
      extrapolateRight: 'clamp',
    });
  });
  const assigned = playerVisibility.filter((v) => v > 0.4).length;

  // Stat-badge na de cascade.
  const badgeAt = CASCADE_START + TOTAL_PLAYERS * CASCADE_INTERVAL + 14; // ~186
  const badge = spring({ frame: frame - badgeAt, fps, config: SPRING_POP });
  const badgeOpacity = interpolate(frame, [badgeAt, badgeAt + 8], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // Subtiele camera-zoom over de hele scène.
  const zoom = interpolate(frame, [0, 230], [1, 1.045]);

  return (
    <SceneShell
      format={format}
      tag="04 · AUTO-PLANNER"
      headline={['De planner', 'doet de puzzel.']}
    >
      <div style={{ position: 'relative' }}>
        <FloatingCard width={700} height={505} enterAt={4} tilt={4.5} zoom={zoom}>
          <div style={{ padding: 26, display: 'flex', flexDirection: 'column' }}>
            <PlanningPageHeader generating={generating} spinnerFrame={frame} />
            <PlanningLegend
              assignedCount={assigned}
              totalEnrollments={TOTAL_PLAYERS}
              totalSlots={SLOTS.length}
              totalCapacity={12}
            />
            <ScheduleGrid slots={SLOTS} playerVisibility={playerVisibility} />
          </div>
        </FloatingCard>

        {/* Stat-badge over de kaart */}
        <div
          style={{
            position: 'absolute',
            right: -38,
            top: -40,
            background: COLORS.ink,
            border: `1.5px solid ${COLORS.lime}`,
            color: COLORS.white,
            borderRadius: 14,
            padding: '14px 20px',
            display: 'flex',
            flexDirection: 'column',
            gap: 3,
            boxShadow: '0 24px 60px rgba(0,0,0,0.5)',
            opacity: badgeOpacity,
            transform: `rotate(2deg) scale(${interpolate(badge, [0, 1], [0.7, 1])})`,
          }}
        >
          <span
            style={{
              fontFamily: FONTS.sans,
              fontSize: 22,
              fontWeight: 800,
              color: COLORS.lime,
              letterSpacing: '-0.02em',
            }}
          >
            12 spelers ingepland
          </span>
          <span style={{ fontFamily: FONTS.mono, fontSize: 12.5, color: 'rgba(255,255,255,0.7)' }}>
            0 conflicten · in 0,8 sec
          </span>
        </div>
      </div>
    </SceneShell>
  );
};
