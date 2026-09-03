import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_SNAPPY } from './brand';

// Kinetic typography: regels die woord-voor-woord met spring-overshoot
// binnenkomen. Optionele chaos-jitter (hook-scène) en exit-wipe.

export interface KineticLine {
  text: string;
  /** frame waarop deze regel binnenkomt */
  at: number;
  /** lime accent op het laatste leesteken */
  limeDot?: boolean;
  /** lichte rotatie in graden voor chaos-gevoel */
  rotate?: number;
  color?: string;
}

export const KineticText: React.FC<{
  lines: KineticLine[];
  fontSize: number;
  /** vanaf dit frame schudt alles licht (chaos) */
  jitterFrom?: number;
  /** frame waarop alle regels samen naar links wegvegen */
  exitAt?: number;
  align?: 'flex-start' | 'center';
}> = ({ lines, fontSize, jitterFrom, exitAt, align = 'flex-start' }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const exitT = exitAt
    ? interpolate(frame, [exitAt, exitAt + 10], [0, 1], {
        extrapolateLeft: 'clamp',
        extrapolateRight: 'clamp',
      })
    : 0;

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: align,
        gap: fontSize * 0.18,
        transform: `translateX(${exitT * -160}px)`,
        opacity: 1 - exitT,
      }}
    >
      {lines.map((line, i) => {
        const s = spring({ frame: frame - line.at, fps, config: SPRING_SNAPPY });
        const y = interpolate(s, [0, 1], [fontSize * 0.7, 0]);
        const opacity = interpolate(frame, [line.at, line.at + 8], [0, 1], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        });
        const scale = interpolate(s, [0, 1], [1.18, 1]);

        let jitterX = 0;
        let jitterR = 0;
        if (jitterFrom !== undefined && frame >= jitterFrom) {
          const t = frame - jitterFrom;
          const amp = Math.min(1, t / 24);
          jitterX = Math.sin(t * 0.9 + i * 2.1) * 3.5 * amp;
          jitterR = Math.sin(t * 0.7 + i * 1.3) * 0.8 * amp;
        }

        const body = line.limeDot
          ? line.text.replace(/[.!?]$/, '')
          : line.text;
        const punct = line.limeDot ? line.text.slice(body.length) : '';

        return (
          <div
            key={i}
            style={{
              fontFamily: FONTS.sans,
              fontWeight: 800,
              fontSize,
              lineHeight: 1,
              letterSpacing: '-0.03em',
              color: line.color ?? COLORS.white,
              opacity,
              transform: [
                `translateY(${y}px)`,
                `translateX(${jitterX}px)`,
                `rotate(${(line.rotate ?? 0) + jitterR}deg)`,
                `scale(${scale})`,
              ].join(' '),
              whiteSpace: 'nowrap',
            }}
          >
            {body}
            {punct && <span style={{ color: COLORS.lime }}>{punct}</span>}
          </div>
        );
      })}
    </div>
  );
};

// Mono scène-tag, bv. "01 · LESSENREEKS"
export const SceneTag: React.FC<{ text: string; at?: number; fontSize?: number }> = ({
  text,
  at = 0,
  fontSize = 22,
}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [at, at + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const x = interpolate(frame, [at, at + 12], [-18, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  return (
    <div
      style={{
        fontFamily: FONTS.mono,
        fontSize,
        fontWeight: 700,
        color: COLORS.lime,
        letterSpacing: '0.28em',
        opacity,
        transform: `translateX(${x}px)`,
      }}
    >
      {text}
    </div>
  );
};

// Headline-blok onder een SceneTag: regels met spring-entry.
export const Headline: React.FC<{
  lines: string[];
  at: number;
  fontSize: number;
  lineHeight: number;
  maxWidth: number;
}> = ({ lines, at, fontSize, lineHeight, maxWidth }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  return (
    <div style={{ maxWidth, display: 'flex', flexDirection: 'column' }}>
      {lines.map((l, i) => {
        const start = at + i * 7;
        const s = spring({ frame: frame - start, fps, config: SPRING_SNAPPY });
        const y = interpolate(s, [0, 1], [34, 0]);
        const opacity = interpolate(frame, [start, start + 9], [0, 1], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        });
        return (
          <div
            key={i}
            style={{
              fontFamily: FONTS.sans,
              fontWeight: 800,
              fontSize,
              lineHeight,
              letterSpacing: '-0.025em',
              color: COLORS.white,
              opacity,
              transform: `translateY(${y}px)`,
            }}
          >
            {l}
          </div>
        );
      })}
    </div>
  );
};
