import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, SPRING_SOFT } from './brand';

// Zwevende app-kaart: 3D-tilt, diepe schaduw, trage "adem"-float.
// enterAt/exitAt sturen de slide-in (met overshoot) en slide-out.

export const FloatingCard: React.FC<{
  width: number;
  height: number;
  enterAt?: number;
  exitAt?: number;
  /** kantelrichting in graden (positief = rechterkant naar achter) */
  tilt?: number;
  /** schaal bovenop layout-schaal, bv. voor subtiele camera-zoom */
  zoom?: number;
  children: React.ReactNode;
}> = ({ width, height, enterAt = 0, exitAt, tilt = 5, zoom = 1, children }) => {
  const frame = useCurrentFrame();
  const { fps, durationInFrames } = useVideoConfig();

  const enter = spring({ frame: frame - enterAt, fps, config: SPRING_SOFT });
  const enterY = interpolate(enter, [0, 1], [90, 0]);
  const enterOpacity = interpolate(frame, [enterAt, enterAt + 14], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const exitFrame = exitAt ?? durationInFrames - 14;
  const exitT = interpolate(frame, [exitFrame, exitFrame + 13], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const exitY = exitT * -70;
  const exitOpacity = 1 - exitT;

  // Trage adem-float: ~4px op een cyclus van ~5s.
  const breathe = Math.sin((frame / fps) * (Math.PI * 2) * 0.2) * 4;
  // Tilt vlakt licht af naarmate de kaart "landt".
  const liveTilt = tilt * interpolate(enter, [0, 1], [1.6, 1]);

  return (
    <div style={{ perspective: 1600 }}>
      <div
        style={{
          width,
          height,
          borderRadius: 22,
          background: COLORS.paper,
          border: `1px solid ${COLORS.rule}`,
          boxShadow: '0 50px 120px rgba(0,0,0,0.55), 0 12px 30px rgba(0,0,0,0.35)',
          overflow: 'hidden',
          opacity: enterOpacity * exitOpacity,
          transform: [
            `translateY(${enterY + exitY + breathe}px)`,
            `rotateY(${-liveTilt}deg)`,
            `rotateX(${liveTilt * 0.45}deg)`,
            `scale(${zoom})`,
          ].join(' '),
          transformStyle: 'preserve-3d',
        }}
      >
        {children}
      </div>
    </div>
  );
};
