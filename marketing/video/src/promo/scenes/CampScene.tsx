import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP, SPRING_SOFT } from '../brand';
import { SceneShell } from '../SceneShell';
import { FloatingCard } from '../FloatingCard';
import { type PromoFormat } from '../layout';

// 02 · KAMPEN — ~5s. Een kamp-kaart met dag-rijen die in-staggeren, elk met
// kampuren en trainer-chips (eigen uren), plus een live inschrijvingen-teller.

interface TrainerStint {
  name: string;
  hours: string;
}

interface CampDay {
  day: string;
  date: string;
  hours: string;
  trainers: TrainerStint[];
  /** frame waarop deze dag-rij binnenkomt */
  at: number;
}

const DAYS: CampDay[] = [
  {
    day: 'MA',
    date: '14 apr',
    hours: '09:00 - 16:00',
    trainers: [
      { name: 'Jan J.', hours: '09:00 - 12:00' },
      { name: 'Pieter M.', hours: '12:00 - 16:00' },
    ],
    at: 20,
  },
  {
    day: 'DI',
    date: '15 apr',
    hours: '09:00 - 16:00',
    trainers: [{ name: 'Jan J.', hours: '09:00 - 16:00' }],
    at: 32,
  },
  {
    day: 'WO',
    date: '16 apr',
    hours: '10:00 - 15:00',
    trainers: [{ name: 'Sophie D.', hours: '10:00 - 15:00' }],
    at: 44,
  },
];

export const CampScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  return (
    <SceneShell format={format} tag="02 · KAMPEN" headline={['Of een meerdaags', 'kamp.']}>
      <FloatingCard width={620} height={560} enterAt={4} tilt={5}>
        <CampMock />
      </FloatingCard>
    </SceneShell>
  );
};

const CampMock: React.FC = () => {
  const frame = useCurrentFrame();

  // Live inschrijvingen-teller: tikt op van 14 naar 20.
  const enrolled = Math.round(
    interpolate(frame, [78, 118], [14, 20], {
      extrapolateLeft: 'clamp',
      extrapolateRight: 'clamp',
    }),
  );
  const spotsLeft = 20 - enrolled;
  const full = enrolled >= 20;

  return (
    <div
      style={{
        width: '100%',
        height: '100%',
        background: COLORS.paper,
        padding: 28,
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      {/* Header */}
      <Reveal at={6} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <span
          style={{
            fontFamily: FONTS.mono,
            fontSize: 12,
            letterSpacing: '0.12em',
            color: COLORS.ink3,
          }}
        >
          PAASKAMP 2026 · TC COACHOS
        </span>
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 7,
            background: COLORS.canvas,
            borderRadius: 999,
            padding: '5px 11px',
            fontFamily: FONTS.sans,
            fontSize: 12,
            fontWeight: 600,
            color: COLORS.ink2,
          }}
        >
          <span style={{ width: 7, height: 7, borderRadius: 999, background: COLORS.tennisGreen }} />
          {full ? 'Bijna vol' : 'Inschrijvingen open'}
        </span>
      </Reveal>

      <Reveal at={8} style={{ marginTop: 14 }}>
        <div style={{ fontFamily: FONTS.sans, fontSize: 26, fontWeight: 800, color: COLORS.ink, letterSpacing: '-0.02em' }}>
          Paaskamp Gevorderden
        </div>
        <div style={{ fontFamily: FONTS.mono, fontSize: 13, color: COLORS.ink3, marginTop: 4 }}>
          3 dagen · 14 tot 16 april · €95
        </div>
      </Reveal>

      {/* Dag-rijen */}
      <div style={{ marginTop: 18, display: 'flex', flexDirection: 'column', gap: 10, flex: 1 }}>
        {DAYS.map((day) => (
          <DayRow key={day.day} day={day} />
        ))}
      </div>

      {/* Footer-teller */}
      <Reveal
        at={70}
        style={{
          marginTop: 16,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          background: COLORS.ink,
          borderRadius: 12,
          padding: '12px 16px',
          color: COLORS.white,
        }}
      >
        <div>
          <div style={{ fontFamily: FONTS.mono, fontSize: 10, letterSpacing: '0.12em', color: COLORS.tennisLime }}>
            INGESCHREVEN
          </div>
          <div style={{ fontFamily: FONTS.sans, fontSize: 22, fontWeight: 800, color: COLORS.tennisLime }}>
            {enrolled} <span style={{ fontWeight: 600 }}>/ 20</span>
          </div>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontFamily: FONTS.mono, fontSize: 10, letterSpacing: '0.12em', color: 'rgba(255,255,255,0.6)' }}>
            PLEKKEN VRIJ
          </div>
          <div style={{ fontFamily: FONTS.sans, fontSize: 22, fontWeight: 800 }}>{spotsLeft}</div>
        </div>
      </Reveal>
    </div>
  );
};

const DayRow: React.FC<{ day: CampDay }> = ({ day }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const enter = spring({ frame: frame - day.at, fps, config: SPRING_SOFT });
  const opacity = interpolate(frame, [day.at, day.at + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const x = interpolate(enter, [0, 1], [-18, 0]);

  return (
    <div style={{ display: 'flex', gap: 12, opacity, transform: `translateX(${x}px)` }}>
      <div
        style={{
          width: 56,
          flexShrink: 0,
          background: COLORS.canvas,
          borderRadius: 9,
          padding: '8px 0',
          textAlign: 'center',
        }}
      >
        <div style={{ fontFamily: FONTS.mono, fontSize: 10, letterSpacing: '0.12em', color: COLORS.ink3 }}>{day.day}</div>
        <div style={{ fontFamily: FONTS.sans, fontSize: 15, fontWeight: 700, color: COLORS.ink, lineHeight: 1.1 }}>
          {day.date}
        </div>
      </div>
      <div style={{ flex: 1, background: COLORS.canvas, borderRadius: 9, padding: '9px 12px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
          <span style={{ width: 7, height: 7, borderRadius: 2, background: COLORS.tennisGreen }} />
          <span style={{ fontFamily: FONTS.mono, fontSize: 12, fontWeight: 600, color: COLORS.ink }}>{day.hours}</span>
        </div>
        <div style={{ marginTop: 8, display: 'flex', flexWrap: 'wrap', gap: 7 }}>
          {day.trainers.map((t, i) => (
            <TrainerChip key={t.name + t.hours} trainer={t} at={day.at + 6 + i * 5} />
          ))}
        </div>
      </div>
    </div>
  );
};

const TrainerChip: React.FC<{ trainer: TrainerStint; at: number }> = ({ trainer, at }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const pop = spring({ frame: frame - at, fps, config: SPRING_POP });
  const opacity = interpolate(frame, [at, at + 6], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 7,
        background: COLORS.white,
        border: `1px solid ${COLORS.rule}`,
        borderRadius: 999,
        padding: '4px 10px',
        opacity,
        transform: `scale(${interpolate(pop, [0, 1], [0.8, 1])})`,
      }}
    >
      <span style={{ width: 6, height: 6, borderRadius: 999, background: COLORS.tennisGreen }} />
      <span style={{ fontFamily: FONTS.sans, fontSize: 12, fontWeight: 700, color: COLORS.ink }}>{trainer.name}</span>
      <span style={{ fontFamily: FONTS.mono, fontSize: 11, color: COLORS.ink3 }}>{trainer.hours}</span>
    </span>
  );
};

const Reveal: React.FC<{ at: number; style?: React.CSSProperties; children: React.ReactNode }> = ({
  at,
  style,
  children,
}) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const enter = spring({ frame: frame - at, fps, config: SPRING_SOFT });
  const opacity = interpolate(frame, [at, at + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const y = interpolate(enter, [0, 1], [12, 0]);
  return <div style={{ opacity, transform: `translateY(${y}px)`, ...style }}>{children}</div>;
};
