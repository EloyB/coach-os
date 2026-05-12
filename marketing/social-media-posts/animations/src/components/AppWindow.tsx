import { interpolate, useCurrentFrame, Easing } from 'remotion';
import { COLORS } from '../brand';

// Inner window chrome that frames the real-looking app UI.
// Sits inside the BrandFrame's chrome (lime monogram + court lines + lime strip).
// Coordinates are exported so the Cursor and any positioned overlays can
// share the same coordinate space without hardcoding offsets.

export const WINDOW = {
  x: 80,
  y: 200,
  width: 920,
  height: 760,
  borderRadius: 22,
} as const;

interface AppWindowProps {
  children: React.ReactNode;
  entryFrame?: number;
}

export const AppWindow: React.FC<AppWindowProps> = ({ children, entryFrame = 0 }) => {
  const frame = useCurrentFrame();
  const r = frame - entryFrame;
  const opacity = interpolate(r, [0, 12], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });
  const scale = interpolate(r, [0, 18], [0.98, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });
  return (
    <div
      style={{
        position: 'absolute',
        top: WINDOW.y,
        left: WINDOW.x,
        width: WINDOW.width,
        height: WINDOW.height,
        background: COLORS.paper,
        borderRadius: WINDOW.borderRadius,
        border: `1px solid ${COLORS.rule}`,
        boxShadow:
          '0 30px 60px -20px rgba(0,0,0,0.55), 0 12px 24px -12px rgba(0,0,0,0.35)',
        opacity,
        transform: `scale(${scale})`,
        transformOrigin: 'center center',
        overflow: 'hidden',
        display: 'flex',
      }}
    >
      {children}
    </div>
  );
};
