import { AbsoluteFill, interpolate, useCurrentFrame } from 'remotion';
import { COLORS } from './brand';
import type { PromoFormat } from './layout';

// Donker podium met het court-line-motief van de social cards.
// De court-lijnen krijgen een trage parallax-drift zodat het beeld leeft.

const CourtLines: React.FC<{ format: PromoFormat; drift: number }> = ({ format, drift }) => {
  const wide = format === 'wide';
  // In 16:9 schalen we het vierkante motief op en centreren we het.
  const vb = '0 0 1080 1080';
  const size = wide ? 1400 : 1080;
  const left = wide ? (1920 - size) / 2 + drift : (1080 - size) / 2 + drift;
  const top = wide ? (1080 - size) / 2 : 240;
  return (
    <svg
      width={size}
      height={size}
      viewBox={vb}
      style={{ position: 'absolute', left, top, opacity: 0.07 }}
    >
      <g stroke="#ffffff" fill="none">
        <rect x={90} y={90} width={900} height={850} strokeWidth={2.5} />
        <line x1={540} y1={90} x2={540} y2={940} strokeWidth={3} />
        <line x1={285} y1={90} x2={285} y2={940} strokeWidth={1.2} />
        <line x1={795} y1={90} x2={795} y2={940} strokeWidth={1.2} />
        <line x1={285} y1={515} x2={795} y2={515} strokeWidth={1.2} />
        <circle cx={900} cy={175} r={92} strokeWidth={1.2} strokeOpacity={0.7} />
        <circle cx={900} cy={175} r={58} strokeWidth={1.2} strokeOpacity={0.7} />
        <circle cx={90} cy={515} r={7} fill="#ffffff" fillOpacity={0.7} stroke="none" />
        <circle cx={990} cy={515} r={7} fill="#ffffff" fillOpacity={0.7} stroke="none" />
      </g>
    </svg>
  );
};

export const Stage: React.FC<{
  format: PromoFormat;
  children: React.ReactNode;
}> = ({ format, children }) => {
  const frame = useCurrentFrame();
  // Heel trage drift voor parallax-gevoel (±10px over een hele scène).
  const drift = interpolate(frame, [0, 240], [-10, 10]);
  return (
    <AbsoluteFill style={{ background: COLORS.bg, overflow: 'hidden' }}>
      <CourtLines format={format} drift={drift} />
      {/* zachte vignette zodat de randen rustiger zijn */}
      <AbsoluteFill
        style={{
          background:
            'radial-gradient(ellipse at 50% 42%, rgba(0,0,0,0) 55%, rgba(0,0,0,0.35) 100%)',
          pointerEvents: 'none',
        }}
      />
      {children}
    </AbsoluteFill>
  );
};
