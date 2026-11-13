import { Fragment } from 'react';
import { COLORS, FONTS } from '../brand';

// Geport uit marketing/social-media-posts/animations/src/components/PlanningView.tsx
// (alleen de planning-pagina-delen die de promo nodig heeft; titel zonder em-dash).

export type Level = 'beginner' | 'intermediate' | 'expert';

export const LEVEL_COLORS: Record<Level, string> = {
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

const RefreshIcon: React.FC<{ size?: number; color: string; frame?: number }> = ({
  size = 14,
  color,
  frame = 0,
}) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke={color}
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    style={{ transform: `rotate(${(frame * 12) % 360}deg)`, transformOrigin: 'center' }}
  >
    <polyline points="23 4 23 10 17 10" />
    <polyline points="1 20 1 14 7 14" />
    <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
  </svg>
);

export const PlanningPageHeader: React.FC<{ generating?: boolean; spinnerFrame?: number }> = ({
  generating,
  spinnerFrame,
}) => (
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
      Planning · Voorjaarsreeks 2026
    </h1>
    {generating && (
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 6,
          padding: '5px 10px',
          background: 'rgba(208,255,20,0.18)',
          border: '1px solid rgba(45,80,22,0.18)',
          borderRadius: 999,
          fontSize: 11.5,
          fontWeight: 500,
          color: COLORS.tennisGreen,
          fontFamily: FONTS.sans,
        }}
      >
        <RefreshIcon size={12} color={COLORS.tennisGreen} frame={spinnerFrame} />
        Genereren…
      </div>
    )}
  </div>
);

export const PlanningLegend: React.FC<{
  assignedCount: number;
  totalEnrollments: number;
  totalSlots: number;
  totalCapacity: number;
}> = ({ assignedCount, totalEnrollments, totalSlots, totalCapacity }) => (
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

const SlotCell: React.FC<{ slot: Slot; playerVisibility: number[] }> = ({
  slot,
  playerVisibility,
}) => {
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
      }}
    >
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
        <span style={{ color: totalDone ? COLORS.tennisGreen : COLORS.ink3, fontWeight: 600 }}>
          {totalVisible}/{slot.capacity}
        </span>
      </div>
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

export const ScheduleGrid: React.FC<{ slots: Slot[]; playerVisibility: number[] }> = ({
  slots,
  playerVisibility,
}) => {
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
      <div />
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
      {TIMES.map((time) => (
        <Fragment key={time}>
          <div
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
        </Fragment>
      ))}
    </div>
  );
};
