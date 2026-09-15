import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP, SPRING_SNAPPY } from '../brand';
import { Stage } from '../Stage';
import { LAYOUTS, type PromoFormat } from '../layout';

// 30–35s. Monogram veert in, dan "1 middag · 0 Excel · 1 tool." en coach-os.be.

const SEGMENTS = ['1 middag', '0 Excel', '1 tool'];

export const OutroScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const L = LAYOUTS[format];
  const size = L.heroFontSize * 0.72;

  const logo = spring({ frame: frame - 8, fps, config: SPRING_POP });
  const logoOpacity = interpolate(frame, [8, 16], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const urlAt = 92;
  const urlOpacity = interpolate(frame, [urlAt, urlAt + 12], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const stripAt = 108;
  const strip = spring({ frame: frame - stripAt, fps, config: SPRING_SNAPPY });

  return (
    <Stage format={format}>
      <AbsoluteFill
        style={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 44,
          padding: L.pad,
        }}
      >
        {/* Monogram */}
        <div
          style={{
            width: 130,
            height: 130,
            borderRadius: 18,
            background: COLORS.lime,
            display: 'grid',
            placeItems: 'center',
            opacity: logoOpacity,
            transform: `scale(${interpolate(logo, [0, 1], [0.6, 1])})`,
            boxShadow: '0 30px 80px rgba(208,255,20,0.18)',
          }}
        >
          <svg width={76} height={76} viewBox="0 0 120 120" fill="none">
            <g stroke={COLORS.bg} strokeWidth={8} strokeLinecap="round">
              <circle cx={60} cy={60} r={32} />
              <path d="M 30 60 C 39 47, 48 47, 60 60 C 72 73, 81 73, 90 60" />
            </g>
          </svg>
        </div>

        {/* Kernframe */}
        <div
          style={{
            display: 'flex',
            flexDirection: format === 'wide' ? 'row' : 'column',
            alignItems: 'center',
            gap: format === 'wide' ? 30 : 12,
          }}
        >
          {SEGMENTS.map((seg, i) => {
            const at = 34 + i * 12;
            const s = spring({ frame: frame - at, fps, config: SPRING_SNAPPY });
            const opacity = interpolate(frame, [at, at + 8], [0, 1], {
              extrapolateLeft: 'clamp',
              extrapolateRight: 'clamp',
            });
            return (
              <div
                key={seg}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: format === 'wide' ? 30 : 12,
                }}
              >
                <span
                  style={{
                    fontFamily: FONTS.sans,
                    fontWeight: 800,
                    fontSize: size,
                    letterSpacing: '-0.03em',
                    color: COLORS.white,
                    opacity,
                    transform: `translateY(${interpolate(s, [0, 1], [40, 0])}px)`,
                    whiteSpace: 'nowrap',
                    lineHeight: 1.1,
                  }}
                >
                  {seg}
                  {i === SEGMENTS.length - 1 && <span style={{ color: COLORS.lime }}>.</span>}
                </span>
                {i < SEGMENTS.length - 1 && format === 'wide' && (
                  <span
                    style={{
                      width: 14,
                      height: 14,
                      borderRadius: 999,
                      background: COLORS.lime,
                      opacity,
                    }}
                  />
                )}
              </div>
            );
          })}
        </div>

        {/* URL */}
        <div
          style={{
            fontFamily: FONTS.mono,
            fontSize: 30,
            fontWeight: 700,
            letterSpacing: '0.22em',
            color: COLORS.white,
            opacity: urlOpacity,
          }}
        >
          coach-os.be
        </div>
      </AbsoluteFill>

      {/* Lime accentstrip schuift onderaan binnen */}
      <div
        style={{
          position: 'absolute',
          left: 0,
          right: 0,
          bottom: 0,
          height: 46,
          background: COLORS.lime,
          transform: `translateY(${interpolate(strip, [0, 1], [46, 0])}px)`,
        }}
      />
    </Stage>
  );
};
