import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP } from '../brand';
import { KineticText } from '../KineticText';
import { Stage } from '../Stage';
import { LAYOUTS, type PromoFormat } from '../layout';

// 0–4s. Chaos-woorden stapelen met jitter, smash-cut, dan "Of één middag."

export const HookScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const L = LAYOUTS[format];
  const chaosSize = L.heroFontSize * 0.62;

  // Payoff vanaf frame 78.
  const payoffAt = 78;
  const payoff = spring({ frame: frame - payoffAt, fps, config: SPRING_POP });
  const payoffOpacity = interpolate(frame, [payoffAt, payoffAt + 8], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <Stage format={format}>
      <AbsoluteFill
        style={{
          padding: L.pad,
          display: 'flex',
          alignItems: format === 'wide' ? 'center' : 'center',
          justifyContent: 'center',
        }}
      >
        {frame < payoffAt && (
          <KineticText
            fontSize={chaosSize}
            jitterFrom={44}
            exitAt={payoffAt - 10}
            lines={[
              { text: 'Drie weekenden.', at: 0, rotate: -1.2 },
              { text: 'Excel.', at: 10, rotate: 0.8 },
              { text: 'WhatsApp.', at: 20, rotate: -0.6 },
              { text: 'Betaalverzoekjes.', at: 30, rotate: 1.4 },
            ]}
          />
        )}
        {frame >= payoffAt && (
          <div
            style={{
              fontFamily: FONTS.sans,
              fontWeight: 800,
              fontSize: L.heroFontSize,
              letterSpacing: '-0.03em',
              color: COLORS.white,
              opacity: payoffOpacity,
              transform: `scale(${interpolate(payoff, [0, 1], [1.25, 1])})`,
              whiteSpace: format === 'wide' ? 'nowrap' : 'normal',
              textAlign: 'center',
              lineHeight: 1.05,
            }}
          >
            Of één middag<span style={{ color: COLORS.lime }}>.</span>
          </div>
        )}
      </AbsoluteFill>
    </Stage>
  );
};
