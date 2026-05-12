import { interpolate, useCurrentFrame, Easing } from 'remotion';
import { COLORS, FONTS } from '../brand';

// iOS-style phone bezel + screen, centered on the 1080×1080 canvas.
// Sized to sit between the BrandFrame's title chrome (y≤180) and the
// lime accent strip (y≥1032). Exports its outer/inner coordinates so the
// composition can position the Cursor over taps.

export const PHONE = {
  // Outer bezel
  x: 350,
  y: 200,
  width: 380,
  height: 780,
  bezel: 10,
  borderRadius: 42,
} as const;

export const SCREEN = {
  // Inner screen (where content is rendered)
  x: PHONE.x + PHONE.bezel, // 360
  y: PHONE.y + PHONE.bezel, // 210
  width: PHONE.width - PHONE.bezel * 2, // 360
  height: PHONE.height - PHONE.bezel * 2, // 760
  borderRadius: PHONE.borderRadius - PHONE.bezel, // 32
} as const;

// Status bar height inside the screen
export const STATUS_BAR_HEIGHT = 28;

const SignalIcon: React.FC = () => (
  <svg width="16" height="11" viewBox="0 0 18 12" fill="currentColor">
    <rect x="0" y="8" width="3" height="4" rx="0.5" />
    <rect x="5" y="6" width="3" height="6" rx="0.5" />
    <rect x="10" y="3" width="3" height="9" rx="0.5" />
    <rect x="15" y="0" width="3" height="12" rx="0.5" />
  </svg>
);

const WifiIcon: React.FC = () => (
  <svg width="15" height="11" viewBox="0 0 16 12" fill="currentColor">
    <path d="M8 10.5 a1.4 1.4 0 1 0 0.001 0z" />
    <path d="M5 8 c1.5 -1.5 4.5 -1.5 6 0" stroke="currentColor" strokeWidth="1.4" fill="none" strokeLinecap="round" />
    <path d="M2.5 5.5 c3 -3 8 -3 11 0" stroke="currentColor" strokeWidth="1.4" fill="none" strokeLinecap="round" />
    <path d="M0 3 c4.5 -4 11.5 -4 16 0" stroke="currentColor" strokeWidth="1.4" fill="none" strokeLinecap="round" />
  </svg>
);

const BatteryIcon: React.FC = () => (
  <svg width="25" height="12" viewBox="0 0 27 13" fill="none">
    <rect x="0.5" y="0.5" width="22" height="12" rx="3" stroke="currentColor" strokeOpacity="0.5" fill="none" />
    <rect x="2.5" y="2.5" width="18" height="8" rx="1.5" fill="currentColor" />
    <rect x="24" y="3.5" width="2.5" height="6" rx="1" fill="currentColor" opacity="0.5" />
  </svg>
);

const StatusBar: React.FC = () => (
  <div
    style={{
      height: STATUS_BAR_HEIGHT,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '0 22px',
      color: COLORS.ink,
      fontFamily: FONTS.sans,
      fontSize: 14,
      fontWeight: 600,
      flexShrink: 0,
    }}
  >
    <span>9:41</span>
    <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
      <SignalIcon />
      <WifiIcon />
      <BatteryIcon />
    </div>
  </div>
);

interface PhoneFrameProps {
  children: React.ReactNode;
  entryFrame?: number;
  // Set false to omit the iOS status bar (useful for full-bleed states)
  showStatusBar?: boolean;
}

export const PhoneFrame: React.FC<PhoneFrameProps> = ({
  children,
  entryFrame = 0,
  showStatusBar = true,
}) => {
  const frame = useCurrentFrame();
  const r = frame - entryFrame;
  const opacity = interpolate(r, [0, 14], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });
  const scale = interpolate(r, [0, 20], [0.92, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });
  return (
    <div
      style={{
        position: 'absolute',
        top: PHONE.y,
        left: PHONE.x,
        width: PHONE.width,
        height: PHONE.height,
        background: '#0a0a0a',
        borderRadius: PHONE.borderRadius,
        padding: PHONE.bezel,
        opacity,
        transform: `scale(${scale})`,
        transformOrigin: 'center center',
        boxShadow:
          '0 30px 60px -20px rgba(0,0,0,0.7), 0 14px 28px -10px rgba(0,0,0,0.45), inset 0 0 0 1.5px rgba(255,255,255,0.06)',
      }}
    >
      <div
        style={{
          width: '100%',
          height: '100%',
          background: COLORS.white,
          borderRadius: SCREEN.borderRadius,
          overflow: 'hidden',
          display: 'flex',
          flexDirection: 'column',
          fontFamily: FONTS.sans,
        }}
      >
        {showStatusBar && <StatusBar />}
        {children}
      </div>
    </div>
  );
};
