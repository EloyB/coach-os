import { COLORS, FONTS } from '../brand';

// Visual recreation of the auto-planner flow:
//   (1) the "before" detail-page header with the "Plan lessen" button
//   (2) the planning page (title + legend + day-time grid)
// All state is driven via props from the AutoPlanner composition.

const CalendarDaysIcon: React.FC<{ size?: number; color: string }> = ({ size = 13, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="4" width="18" height="18" rx="2" />
    <line x1="16" y1="2" x2="16" y2="6" />
    <line x1="8" y1="2" x2="8" y2="6" />
    <line x1="3" y1="10" x2="21" y2="10" />
    <circle cx="8" cy="14" r="0.5" />
    <circle cx="12" cy="14" r="0.5" />
    <circle cx="16" cy="14" r="0.5" />
  </svg>
);

const BackArrowIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <line x1="19" y1="12" x2="5" y2="12" />
    <polyline points="12 19 5 12 12 5" />
  </svg>
);

const RefreshIcon: React.FC<{ size?: number; color: string; spinning?: boolean; frame?: number }> = ({
  size = 14,
  color,
  spinning,
  frame = 0,
}) => {
  const rotate = spinning ? (frame * 12) % 360 : 0;
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color}
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      style={{ transform: `rotate(${rotate}deg)`, transformOrigin: 'center' }}
    >
      <polyline points="23 4 23 10 17 10" />
      <polyline points="1 20 1 14 7 14" />
      <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
    </svg>
  );
};

// ────────────────────────────────────────────────────────────────────────
//  Detail page header (before-click state)
// ────────────────────────────────────────────────────────────────────────

interface DetailHeaderProps {
  planLessonsHover?: boolean;
  planLessonsPressed?: boolean;
}

export const LessonSeriesDetailHeader: React.FC<DetailHeaderProps> = ({
  planLessonsHover,
  planLessonsPressed,
}) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        fontSize: 12,
        color: COLORS.ink3,
        marginBottom: 6,
        fontFamily: FONTS.sans,
      }}
    >
      <BackArrowIcon color={COLORS.ink3} />
      <span>Lessen</span>
    </div>
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 16 }}>
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <h1
            style={{
              fontSize: 24,
              fontWeight: 700,
              color: COLORS.ink,
              margin: 0,
              letterSpacing: -0.5,
              fontFamily: FONTS.sans,
            }}
          >
            Voorjaarsreeks 2026
          </h1>
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 5,
              fontSize: 11,
              fontWeight: 500,
              color: COLORS.tennisGreen,
              background: 'rgba(45,80,22,0.1)',
              padding: '3px 9px',
              borderRadius: 999,
              fontFamily: FONTS.sans,
            }}
          >
            <span style={{ width: 6, height: 6, background: COLORS.tennisGreen, borderRadius: '50%' }} />
            actief
          </span>
        </div>
        <p
          style={{
            fontSize: 12,
            color: COLORS.ink3,
            margin: '6px 0 0',
            fontFamily: FONTS.mono,
            letterSpacing: '0.03em',
          }}
        >
          01 mrt → 30 mei · TC De Linde · €49 · 12 inschrijvingen
        </p>
      </div>
      <button
        type="button"
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 6,
          padding: '9px 16px',
          background: planLessonsHover ? '#264411' : COLORS.tennisGreen,
          color: COLORS.white,
          fontSize: 13,
          fontWeight: 600,
          borderRadius: 8,
          border: 'none',
          transform: planLessonsPressed ? 'scale(0.96)' : 'scale(1)',
          fontFamily: FONTS.sans,
          boxShadow: '0 1px 2px rgba(0,0,0,0.08)',
          flexShrink: 0,
        }}
      >
        <CalendarDaysIcon color={COLORS.white} />
        Plan lessen
      </button>
    </div>
    {/* placeholder rows simulating the rest of the detail page */}
    <div
      style={{
        marginTop: 22,
        background: COLORS.white,
        borderRadius: 12,
        border: `1px solid ${COLORS.rule}`,
        boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
        padding: 18,
        display: 'flex',
        flexDirection: 'column',
        gap: 10,
      }}
    >
      <div style={{ height: 12, width: '40%', background: '#eeebe2', borderRadius: 4 }} />
      <div style={{ height: 10, width: '70%', background: '#f1ede4', borderRadius: 4 }} />
      <div style={{ height: 10, width: '55%', background: '#f1ede4', borderRadius: 4 }} />
      <div style={{ height: 10, width: '62%', background: '#f1ede4', borderRadius: 4 }} />
    </div>
  </div>
);

// ────────────────────────────────────────────────────────────────────────
//  Planning page
// ────────────────────────────────────────────────────────────────────────

export type Level = 'beginner' | 'intermediate' | 'expert';

const LEVEL_COLORS: Record<Level, string> = {
  beginner: '#D0FF14',
  intermediate: '#7AB8FF',
  expert: '#FF8B7A',
};

export interface Player {
  name: string;
  level: Level;
}

export interface Slot {
  day: 'Ma' | 'Wo' | 'Vr';
  time: '17:00' | '18:30';
  court: string;
  capacity: number;
  players: Player[];
}

const DAYS: ('Ma' | 'Wo' | 'Vr')[] = ['Ma', 'Wo', 'Vr'];
const TIMES: ('17:00' | '18:30')[] = ['17:00', '18:30'];

const PlayerChip: React.FC<{ player: Player; visibility: number }> = ({ player, visibility }) => (
  <div
    style={{
      display: 'flex',
      alignItems: 'center',
      gap: 6,
      padding: '4px 8px',
      background: 'rgba(255,255,255,0.7)',
      borderRadius: 6,
      fontSize: 11.5,
      color: COLORS.ink,
      fontWeight: 500,
      fontFamily: FONTS.sans,
      opacity: visibility,
      transform: `translateY(${(1 - visibility) * 8}px) scale(${0.92 + visibility * 0.08})`,
    }}
  >
    <span
      style={{
        width: 8,
        height: 8,
        borderRadius: '50%',
        background: LEVEL_COLORS[player.level],
        flexShrink: 0,
      }}
    />
    <span>{player.name}</span>
  </div>
);

interface SlotCellProps {
  slot: Slot;
  // visibility per player in the slot (0..1)
  playerVisibility: number[];
}

const SlotCell: React.FC<SlotCellProps> = ({ slot, playerVisibility }) => {
  const totalVisible = playerVisibility.filter((v) => v > 0.4).length;
  const totalDone = totalVisible >= slot.players.length;
  const tintAlpha = (totalVisible / slot.capacity) * 0.16;
  return (
    <div
      style={{
        background: `rgba(208,255,20,${tintAlpha})`,
        border: `1px solid ${totalDone ? COLORS.tennisGreen : COLORS.rule}`,
        borderStyle: totalVisible === 0 ? 'dashed' : 'solid',
        borderRadius: 10,
        padding: 8,
        height: 120,
        display: 'flex',
        flexDirection: 'column',
        gap: 4,
        transition: 'none',
      }}
    >
      {/* Court label + capacity */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          fontSize: 10,
          fontFamily: FONTS.mono,
          color: COLORS.ink3,
          letterSpacing: '0.04em',
        }}
      >
        <span>{slot.court}</span>
        <span
          style={{
            color: totalDone ? COLORS.tennisGreen : COLORS.ink3,
            fontWeight: 600,
          }}
        >
          {totalVisible}/{slot.capacity}
        </span>
      </div>
      {/* Player chips */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4, flex: 1 }}>
        {slot.players.map((p, i) => {
          const v = playerVisibility[i] ?? 0;
          if (v <= 0) return null;
          return <PlayerChip key={p.name} player={p} visibility={v} />;
        })}
      </div>
    </div>
  );
};

interface PlanningHeaderProps {
  generating?: boolean;
  spinnerFrame?: number;
}

export const PlanningPageHeader: React.FC<PlanningHeaderProps> = ({ generating, spinnerFrame }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        fontSize: 12,
        color: COLORS.ink3,
        marginBottom: 6,
        fontFamily: FONTS.sans,
      }}
    >
      <BackArrowIcon color={COLORS.ink3} />
      <span>Terug naar lesreeks</span>
    </div>
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12 }}>
      <h1
        style={{
          fontSize: 22,
          fontWeight: 700,
          color: COLORS.ink,
          margin: 0,
          letterSpacing: -0.4,
          fontFamily: FONTS.sans,
        }}
      >
        Planning — Voorjaarsreeks 2026
      </h1>
      {generating && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            padding: '5px 10px',
            background: 'rgba(208,255,20,0.18)',
            border: `1px solid rgba(45,80,22,0.18)`,
            borderRadius: 999,
            fontSize: 11.5,
            fontWeight: 500,
            color: COLORS.tennisGreen,
            fontFamily: FONTS.sans,
          }}
        >
          <RefreshIcon size={12} color={COLORS.tennisGreen} spinning frame={spinnerFrame} />
          Genereren…
        </div>
      )}
    </div>
  </div>
);

interface LegendProps {
  assignedCount: number;
  totalEnrollments: number;
  totalSlots: number;
  totalCapacity: number;
}

export const PlanningLegend: React.FC<LegendProps> = ({
  assignedCount,
  totalEnrollments,
  totalSlots,
  totalCapacity,
}) => (
  <div
    style={{
      marginTop: 10,
      padding: '8px 14px',
      background: COLORS.white,
      border: `1px solid ${COLORS.rule}`,
      borderRadius: 8,
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center',
      fontFamily: FONTS.mono,
      fontSize: 11,
      color: COLORS.ink2,
      letterSpacing: '0.04em',
    }}
  >
    <span>
      {totalEnrollments} inschrijvingen · {totalSlots} tijdsloten · {totalCapacity} plaatsen
    </span>
    <span style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
        <span
          style={{
            display: 'inline-block',
            width: 8,
            height: 8,
            borderRadius: '50%',
            background: COLORS.tennisGreen,
          }}
        />
        Toegewezen
      </span>
      <span style={{ color: COLORS.tennisGreen, fontWeight: 600 }}>
        {assignedCount} / {totalEnrollments}
      </span>
    </span>
  </div>
);

interface ScheduleGridProps {
  slots: Slot[];
  // For each player (flat index), 0..1 visibility
  playerVisibility: number[];
}

export const ScheduleGrid: React.FC<ScheduleGridProps> = ({ slots, playerVisibility }) => {
  // Index players globally across slots in row-then-column order matching the cascade
  // (see AutoPlanner composition for the canonical ordering)
  let playerIndex = 0;
  return (
    <div
      style={{
        marginTop: 14,
        display: 'grid',
        gridTemplateColumns: '50px repeat(3, 1fr)',
        gridTemplateRows: '24px 130px 130px',
        gap: 8,
      }}
    >
      {/* Empty top-left corner */}
      <div />
      {/* Day headers */}
      {DAYS.map((d) => (
        <div
          key={d}
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: 10.5,
            fontFamily: FONTS.mono,
            color: COLORS.ink3,
            textTransform: 'uppercase',
            letterSpacing: '0.08em',
            fontWeight: 700,
          }}
        >
          {d === 'Ma' ? 'Maandag' : d === 'Wo' ? 'Woensdag' : 'Vrijdag'}
        </div>
      ))}
      {/* Rows */}
      {TIMES.map((time) => (
        <>
          <div
            key={time}
            style={{
              display: 'flex',
              alignItems: 'flex-start',
              justifyContent: 'flex-end',
              paddingTop: 6,
              paddingRight: 6,
              fontSize: 10.5,
              fontFamily: FONTS.mono,
              color: COLORS.ink2,
              fontWeight: 600,
            }}
          >
            {time}
          </div>
          {DAYS.map((day) => {
            const slot = slots.find((s) => s.day === day && s.time === time);
            if (!slot) {
              return <div key={day + time} />;
            }
            const visibilities = slot.players.map(() => playerVisibility[playerIndex++] ?? 0);
            return <SlotCell key={day + time} slot={slot} playerVisibility={visibilities} />;
          })}
        </>
      ))}
    </div>
  );
};
