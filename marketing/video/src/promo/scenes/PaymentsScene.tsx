import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP } from '../brand';
import { FloatingCard } from '../FloatingCard';
import { SceneShell } from '../SceneShell';
import type { PromoFormat } from '../layout';

// 24–30s. Rijen springen van "open" naar "betaald", teller loopt op,
// iDEAL- en Bancontact-badges zweven naast de kaart.

const ROWS: { name: string; method: 'Bancontact' | 'iDEAL'; paidAt: number }[] = [
  { name: 'Sven Janssens', method: 'Bancontact', paidAt: 38 },
  { name: 'Marit De Smet', method: 'Bancontact', paidAt: 52 },
  { name: 'Tom Vermeulen', method: 'iDEAL', paidAt: 66 },
  { name: 'Eva Hendrickx', method: 'Bancontact', paidAt: 80 },
  { name: 'Lars Peeters', method: 'iDEAL', paidAt: 94 },
  { name: 'Lieve Goossens', method: 'Bancontact', paidAt: 108 },
];

const PRICE = 49;

const CheckIcon: React.FC<{ size?: number }> = ({ size = 13 }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth={3}
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <polyline points="20 6 9 17 4 12" />
  </svg>
);

const MethodBadge: React.FC<{
  label: string;
  color: string;
  at: number;
  side: 'top' | 'bottom';
}> = ({ label, color, at, side }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const s = spring({ frame: frame - at, fps, config: SPRING_POP });
  const opacity = interpolate(frame, [at, at + 8], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const breathe = Math.sin((frame / fps) * Math.PI * 2 * 0.25 + (side === 'top' ? 0 : 2)) * 5;
  return (
    <div
      style={{
        position: 'absolute',
        right: -52,
        ...(side === 'top' ? { top: 148 } : { bottom: 96 }),
        background: COLORS.white,
        borderRadius: 12,
        padding: '12px 18px',
        fontFamily: FONTS.sans,
        fontSize: 17,
        fontWeight: 800,
        color,
        boxShadow: '0 24px 60px rgba(0,0,0,0.5)',
        opacity,
        transform: `translateY(${interpolate(s, [0, 1], [30, 0]) + breathe}px) rotate(${side === 'top' ? 2.5 : -2}deg) scale(${interpolate(s, [0, 1], [0.75, 1])})`,
      }}
    >
      {label}
    </div>
  );
};

export const PaymentsScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const paidCount = ROWS.filter((r) => frame >= r.paidAt).length;
  const counter = Math.round(
    interpolate(frame, [38, 118], [0, ROWS.length * PRICE], {
      extrapolateLeft: 'clamp',
      extrapolateRight: 'clamp',
    }),
  );

  return (
    <SceneShell
      format={format}
      tag="04 · BETALINGEN"
      headline={['Betaald. Zonder', 'achternajagen.']}
    >
      <div style={{ position: 'relative' }}>
        <FloatingCard width={620} height={510} enterAt={4} tilt={5}>
          <div style={{ padding: 26, display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
              <div>
                <div
                  style={{
                    fontFamily: FONTS.mono,
                    fontSize: 11,
                    color: COLORS.ink3,
                    letterSpacing: '0.08em',
                    textTransform: 'uppercase',
                  }}
                >
                  Ontvangen
                </div>
                <div
                  style={{
                    fontFamily: FONTS.mono,
                    fontSize: 40,
                    fontWeight: 800,
                    color: COLORS.tennisGreen,
                    letterSpacing: '-0.02em',
                    lineHeight: 1.05,
                  }}
                >
                  € {counter.toLocaleString('nl-BE')}
                </div>
              </div>
              <div
                style={{
                  fontFamily: FONTS.sans,
                  fontSize: 13,
                  fontWeight: 600,
                  color: COLORS.ink2,
                  paddingBottom: 6,
                }}
              >
                {paidCount} van {ROWS.length} betaald
              </div>
            </div>

            <div style={{ height: 1, background: COLORS.rule }} />

            <div style={{ display: 'flex', flexDirection: 'column', gap: 9 }}>
              {ROWS.map((r) => {
                const paid = frame >= r.paidAt;
                const s = spring({ frame: frame - r.paidAt, fps, config: SPRING_POP });
                return (
                  <div
                    key={r.name}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 12,
                      padding: '10px 14px',
                      borderRadius: 10,
                      background: paid ? 'rgba(208,255,20,0.12)' : COLORS.white,
                      border: `1px solid ${paid ? 'rgba(45,80,22,0.3)' : COLORS.rule}`,
                    }}
                  >
                    <div
                      style={{
                        width: 26,
                        height: 26,
                        borderRadius: 999,
                        display: 'grid',
                        placeItems: 'center',
                        background: paid ? COLORS.tennisGreen : COLORS.canvas,
                        color: paid ? COLORS.lime : COLORS.ink3,
                        transform: paid ? `scale(${interpolate(s, [0, 1], [0.5, 1])})` : 'none',
                        fontFamily: FONTS.mono,
                        fontSize: 11,
                      }}
                    >
                      {paid ? <CheckIcon /> : '·'}
                    </div>
                    <span
                      style={{
                        flex: 1,
                        fontFamily: FONTS.sans,
                        fontSize: 14.5,
                        fontWeight: 600,
                        color: COLORS.ink,
                      }}
                    >
                      {r.name}
                    </span>
                    <span
                      style={{
                        fontFamily: FONTS.mono,
                        fontSize: 11.5,
                        color: paid ? COLORS.tennisGreen : COLORS.ink3,
                        fontWeight: 600,
                      }}
                    >
                      {paid ? r.method : 'open'}
                    </span>
                    <span
                      style={{
                        fontFamily: FONTS.mono,
                        fontSize: 13,
                        fontWeight: 700,
                        color: COLORS.ink,
                        minWidth: 46,
                        textAlign: 'right',
                      }}
                    >
                      € {PRICE}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        </FloatingCard>

        <MethodBadge label="Bancontact" color="#005498" at={56} side="top" />
        <MethodBadge label="iDEAL" color="#CC0066" at={70} side="bottom" />
      </div>
    </SceneShell>
  );
};
