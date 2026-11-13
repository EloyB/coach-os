import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP } from '../brand';
import { FloatingCard } from '../FloatingCard';
import { SceneShell } from '../SceneShell';
import type { PromoFormat } from '../layout';

// 4–10s. Wizard vult zich in cascade, daarna floept de deellink eruit.

const FIELDS: { label: string; value: string; at: number }[] = [
  { label: 'Naam reeks', value: 'Voorjaarsreeks 2026', at: 26 },
  { label: 'Club', value: 'TC De Linde', at: 42 },
  { label: 'Prijs per speler', value: '€ 49', at: 58 },
];

const SLOTS: { label: string; at: number }[] = [
  { label: 'Ma 17:00', at: 74 },
  { label: 'Wo 17:00', at: 84 },
  { label: 'Vr 18:30', at: 94 },
];

const Field: React.FC<{ label: string; value: string; at: number }> = ({ label, value, at }) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [at, at + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const y = interpolate(frame, [at, at + 10], [10, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  // Waarde "typt" zich in.
  const chars = Math.round(
    interpolate(frame, [at + 4, at + 16], [0, value.length], {
      extrapolateLeft: 'clamp',
      extrapolateRight: 'clamp',
    }),
  );
  return (
    <div style={{ opacity, transform: `translateY(${y}px)` }}>
      <div
        style={{
          fontFamily: FONTS.mono,
          fontSize: 11,
          color: COLORS.ink3,
          letterSpacing: '0.06em',
          textTransform: 'uppercase',
          marginBottom: 6,
        }}
      >
        {label}
      </div>
      <div
        style={{
          background: COLORS.white,
          border: `1px solid ${COLORS.rule}`,
          borderRadius: 10,
          padding: '12px 14px',
          fontFamily: FONTS.sans,
          fontSize: 16,
          fontWeight: 600,
          color: COLORS.ink,
          minHeight: 46,
        }}
      >
        {value.slice(0, chars)}
        {chars < value.length && <span style={{ color: COLORS.tennisGreen }}>|</span>}
      </div>
    </div>
  );
};

export const SeriesScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const linkAt = 116;
  const linkSpring = spring({ frame: frame - linkAt, fps, config: SPRING_POP });
  const copiedAt = 144;
  const copied = frame >= copiedAt;
  const copiedSpring = spring({ frame: frame - copiedAt, fps, config: SPRING_POP });

  return (
    <SceneShell
      format={format}
      tag="01 · LESSENREEKS"
      headline={['Zet je reeks op.', 'Deel één link.']}
    >
      <div style={{ position: 'relative' }}>
        <FloatingCard width={620} height={560} enterAt={4} tilt={5}>
          <div style={{ padding: 28, display: 'flex', flexDirection: 'column', gap: 18 }}>
            <div
              style={{
                fontFamily: FONTS.sans,
                fontSize: 20,
                fontWeight: 700,
                color: COLORS.ink,
                letterSpacing: -0.4,
              }}
            >
              Nieuwe lessenreeks
            </div>
            {FIELDS.map((f) => (
              <Field key={f.label} {...f} />
            ))}
            <div>
              <div
                style={{
                  fontFamily: FONTS.mono,
                  fontSize: 11,
                  color: COLORS.ink3,
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  marginBottom: 6,
                }}
              >
                Tijdsloten
              </div>
              <div style={{ display: 'flex', gap: 10 }}>
                {SLOTS.map((s) => {
                  const v = interpolate(frame, [s.at, s.at + 10], [0, 1], {
                    extrapolateLeft: 'clamp',
                    extrapolateRight: 'clamp',
                  });
                  return (
                    <div
                      key={s.label}
                      style={{
                        padding: '9px 14px',
                        borderRadius: 999,
                        background: 'rgba(208,255,20,0.18)',
                        border: '1px solid rgba(45,80,22,0.25)',
                        fontFamily: FONTS.mono,
                        fontSize: 13,
                        fontWeight: 600,
                        color: COLORS.tennisGreen,
                        opacity: v,
                        transform: `translateY(${(1 - v) * 10}px) scale(${0.9 + v * 0.1})`,
                      }}
                    >
                      {s.label}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </FloatingCard>

        {/* Deellink-kaartje dat over de hoek floept */}
        <div
          style={{
            position: 'absolute',
            right: -46,
            bottom: 26,
            background: COLORS.ink,
            color: COLORS.white,
            borderRadius: 14,
            padding: '16px 20px',
            display: 'flex',
            alignItems: 'center',
            gap: 14,
            boxShadow: '0 24px 60px rgba(0,0,0,0.5)',
            opacity: interpolate(frame, [linkAt, linkAt + 8], [0, 1], {
              extrapolateLeft: 'clamp',
              extrapolateRight: 'clamp',
            }),
            transform: `translateY(${interpolate(linkSpring, [0, 1], [40, 0])}px) rotate(-2deg) scale(${interpolate(linkSpring, [0, 1], [0.8, 1])})`,
          }}
        >
          <span style={{ fontFamily: FONTS.mono, fontSize: 15, letterSpacing: '0.02em' }}>
            coach-os.be/r/voorjaar-2026
          </span>
          <span
            style={{
              fontFamily: FONTS.sans,
              fontSize: 13,
              fontWeight: 700,
              color: copied ? COLORS.lime : 'rgba(255,255,255,0.55)',
              transform: copied ? `scale(${interpolate(copiedSpring, [0, 1], [1.4, 1])})` : 'none',
              whiteSpace: 'nowrap',
            }}
          >
            {copied ? 'Gekopieerd ✓' : 'Kopieer'}
          </span>
        </div>
      </div>
    </SceneShell>
  );
};
