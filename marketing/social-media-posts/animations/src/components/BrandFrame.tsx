import { AbsoluteFill } from 'remotion';
import { COLORS, FONTS } from '../brand';

// Shared visual chrome wrapping every animation — dark background,
// court-line decoration, monogram top-left, lime accent strip bottom.
// Drop your animated content as children; the chrome stays stable
// across all four feature clips for brand consistency.
//
// Optional `step` + `title` render a feature label to the right of the
// monogram so each carousel slide is self-identifying at thumb-scroll speed.

interface BrandFrameProps {
  children: React.ReactNode;
  step?: string;   // e.g. "01 / 04"
  title?: string;  // e.g. "Lesreeks aanmaken"
}

export const BrandFrame: React.FC<BrandFrameProps> = ({ children, step, title }) => {
  return (
    <AbsoluteFill
      style={{
        backgroundColor: COLORS.bg,
        fontFamily: FONTS.sans,
      }}
    >
      {/* Court-line decoration (very subtle) */}
      <svg
        style={{ position: 'absolute', inset: 0, opacity: 0.07 }}
        viewBox="0 0 1080 1080"
        width="1080"
        height="1080"
      >
        <g stroke={COLORS.white} fill="none">
          <rect x="90" y="90" width="900" height="850" strokeWidth="2.5" />
          <line x1="540" y1="90" x2="540" y2="940" strokeWidth="3" />
          <line x1="285" y1="90" x2="285" y2="940" strokeWidth="1.2" />
          <line x1="795" y1="90" x2="795" y2="940" strokeWidth="1.2" />
          <line x1="285" y1="515" x2="795" y2="515" strokeWidth="1.2" />
          <line x1="533" y1="90" x2="547" y2="90" strokeWidth="3" />
          <line x1="533" y1="940" x2="547" y2="940" strokeWidth="3" />
          <circle cx="900" cy="175" r="92" strokeWidth="1.2" strokeOpacity="0.7" />
          <circle cx="900" cy="175" r="58" strokeWidth="1.2" strokeOpacity="0.7" />
        </g>
      </svg>

      {/* Monogram top-left */}
      <div
        style={{
          position: 'absolute',
          top: 80,
          left: 80,
          width: 100,
          height: 100,
          background: COLORS.lime,
          borderRadius: 13,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <svg width="70" height="70" viewBox="0 0 60 60">
          <g stroke={COLORS.bg} strokeWidth="5" fill="none" strokeLinecap="round">
            <circle cx="30" cy="30" r="20" />
            <path d="M 10 30 C 16 24, 22 24, 30 30 C 38 36, 44 36, 50 30" />
          </g>
        </svg>
      </div>

      {/* Feature label — only rendered when title is provided */}
      {title && (
        <div
          style={{
            position: 'absolute',
            top: 80,
            left: 204,
            height: 100,
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            gap: 8,
          }}
        >
          {step && (
            <span
              style={{
                fontFamily: FONTS.mono,
                fontSize: 16,
                color: COLORS.lime,
                letterSpacing: '0.12em',
                fontWeight: 500,
              }}
            >
              {step}
            </span>
          )}
          <span
            style={{
              fontFamily: FONTS.sans,
              fontSize: 42,
              fontWeight: 700,
              color: COLORS.white,
              letterSpacing: -1,
              lineHeight: 1,
            }}
          >
            {title}
          </span>
        </div>
      )}

      {/* Lime accent strip — bottom */}
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          height: 48,
          background: COLORS.lime,
        }}
      />

      {/* Composition content goes here */}
      {children}
    </AbsoluteFill>
  );
};
