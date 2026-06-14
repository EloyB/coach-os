import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP, SPRING_SNAPPY } from '../brand';
import { Stage } from '../Stage';
import { LAYOUTS, type PromoFormat } from '../layout';

// 00 · OPENER ("alles komt samen"). Motion-graphics-stijl: verspreide chaos-chips
// drijven los rond, vloeien dan samen naar het midden en lossen op in het
// CoachOS-merk + "Alles op één plek". Vervangt de tekstuele hook.

interface Chip {
  label: string;
  /** scatter-positie in % t.o.v. het midden (0,0 = midden) */
  x: number;
  y: number;
  rotate: number;
  at: number;
}

const CHIPS: Chip[] = [
  { label: 'Excel', x: -32, y: -26, rotate: -7, at: 0 },
  { label: 'WhatsApp', x: 30, y: -30, rotate: 6, at: 5 },
  { label: 'Mailtjes', x: -38, y: 8, rotate: 4, at: 10 },
  { label: 'Betaalverzoekjes', x: 34, y: 12, rotate: -5, at: 15 },
  { label: 'Losse briefjes', x: -22, y: 30, rotate: 8, at: 20 },
  { label: 'Telefoontjes', x: 24, y: 32, rotate: -8, at: 25 },
];

// Tijdlijn (frames): chaos → convergeren → resolve.
const CONVERGE_AT = 52;
const CONVERGE_END = 82;
const RESOLVE_AT = 74;

export const ConsolidationScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const L = LAYOUTS[format];

  // Resolve: merk + label veren in nadat de chips zijn samengevloeid.
  const mark = spring({ frame: frame - RESOLVE_AT, fps, config: SPRING_POP });
  const markOpacity = interpolate(frame, [RESOLVE_AT, RESOLVE_AT + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const labelAt = RESOLVE_AT + 14;
  const labelSpring = spring({ frame: frame - labelAt, fps, config: SPRING_SNAPPY });
  const labelOpacity = interpolate(frame, [labelAt, labelAt + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const markSize = format === 'wide' ? 150 : 124;

  return (
    <Stage format={format}>
      <AbsoluteFill style={{ display: 'grid', placeItems: 'center', padding: L.pad }}>
        {/* Chaos-chips laag */}
        <div style={{ position: 'absolute', inset: 0, display: 'grid', placeItems: 'center' }}>
          {CHIPS.map((chip) => (
            <ChaosChip key={chip.label} chip={chip} format={format} />
          ))}
        </div>

        {/* Resolve: CoachOS-merk + label */}
        <div
          style={{
            position: 'relative',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: format === 'wide' ? 30 : 24,
          }}
        >
          <div
            style={{
              width: markSize,
              height: markSize,
              borderRadius: 22,
              background: COLORS.lime,
              display: 'grid',
              placeItems: 'center',
              opacity: markOpacity,
              transform: `scale(${interpolate(mark, [0, 1], [0.4, 1])})`,
              boxShadow: '0 30px 90px rgba(208,255,20,0.22)',
            }}
          >
            <svg width={markSize * 0.58} height={markSize * 0.58} viewBox="0 0 120 120" fill="none">
              <g stroke={COLORS.bg} strokeWidth={8} strokeLinecap="round">
                <circle cx={60} cy={60} r={32} />
                <path d="M 30 60 C 39 47, 48 47, 60 60 C 72 73, 81 73, 90 60" />
              </g>
            </svg>
          </div>

          <div
            style={{
              fontFamily: FONTS.sans,
              fontWeight: 800,
              fontSize: format === 'wide' ? 64 : 52,
              letterSpacing: '-0.03em',
              color: COLORS.white,
              opacity: labelOpacity,
              transform: `translateY(${interpolate(labelSpring, [0, 1], [34, 0])}px)`,
              textAlign: 'center',
              lineHeight: 1.05,
            }}
          >
            Alles op <span style={{ color: COLORS.lime }}>één plek</span>.
          </div>
        </div>
      </AbsoluteFill>
    </Stage>
  );
};

const ChaosChip: React.FC<{ chip: Chip; format: PromoFormat }> = ({ chip, format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // Spread-factor: hoe ver de chips uitwaaieren (px). Tall is smaller/hoger.
  const spreadX = format === 'wide' ? 7.6 : 5.4;
  const spreadY = format === 'wide' ? 7.2 : 8.2;

  // Entry.
  const enter = spring({ frame: frame - chip.at, fps, config: SPRING_SNAPPY });
  const enterOpacity = interpolate(frame, [chip.at, chip.at + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // Trage drift in de chaos-fase.
  const drift = Math.sin((frame / fps) * Math.PI * 2 * 0.4 + chip.at) * 5;

  // Convergeren naar het midden + krimpen + uitfaden.
  const conv = interpolate(frame, [CONVERGE_AT, CONVERGE_END], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const ease = conv * conv * (3 - 2 * conv); // smoothstep

  const startX = chip.x * spreadX;
  const startY = chip.y * spreadY;
  const x = interpolate(ease, [0, 1], [startX, 0]) * interpolate(enter, [0, 1], [1.15, 1]);
  const y = interpolate(ease, [0, 1], [startY + drift, 0]);
  const scale = interpolate(ease, [0, 1], [1, 0.2]) * interpolate(enter, [0, 1], [0.7, 1]);
  const rotate = interpolate(ease, [0, 1], [chip.rotate, 0]);
  const opacity = enterOpacity * interpolate(conv, [0.6, 1], [1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        position: 'absolute',
        opacity,
        transform: `translate(${x}px, ${y}px) rotate(${rotate}deg) scale(${scale})`,
        display: 'inline-flex',
        alignItems: 'center',
        gap: 12,
        background: COLORS.paper,
        border: `1px solid ${COLORS.rule}`,
        borderRadius: 12,
        padding: '14px 20px',
        boxShadow: '0 18px 40px rgba(0,0,0,0.35)',
        whiteSpace: 'nowrap',
      }}
    >
      <span style={{ width: 10, height: 10, borderRadius: 3, background: COLORS.tennisGreen }} />
      <span style={{ fontFamily: FONTS.sans, fontWeight: 700, fontSize: 28, color: COLORS.ink }}>
        {chip.label}
      </span>
    </div>
  );
};
