export const TennisBall: React.FC<{
  size?: number;
  rotation?: number;
  withGlow?: boolean;
}> = ({ size = 80, rotation = 0, withGlow = false }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 100 100"
    style={{
      transform: `rotate(${rotation}deg)`,
      filter: withGlow ? "drop-shadow(0 0 20px rgba(208,255,20,0.6))" : "none",
    }}
  >
    <defs>
      <radialGradient id="ballGrad" cx="35%" cy="35%">
        <stop offset="0%" stopColor="#E5FF60" />
        <stop offset="65%" stopColor="#D0FF14" />
        <stop offset="100%" stopColor="#A8CC0E" />
      </radialGradient>
    </defs>
    <circle cx="50" cy="50" r="46" fill="url(#ballGrad)" />
    <path
      d="M 12 50 C 26 28, 40 28, 50 50 C 60 72, 74 72, 88 50"
      stroke="white"
      strokeWidth="3"
      fill="none"
      strokeLinecap="round"
      opacity="0.85"
    />
  </svg>
);

/**
 * Compute bouncing-ball position over time.
 * progress: 0..1 normalized through the bounce
 * Returns y offset (negative = up) for a ball that does N bounces with decay.
 */
export function bounceY(
  progress: number,
  bounces: number,
  peakHeight: number,
  decay = 0.55,
): number {
  const phase = Math.max(0, Math.min(bounces, progress * bounces));
  const idx = Math.floor(phase);
  const t = phase - idx;
  const heightThis = peakHeight * Math.pow(decay, idx);
  return -heightThis * 4 * t * (1 - t);
}
