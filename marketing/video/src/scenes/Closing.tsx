import {
  AbsoluteFill,
  Easing,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { colors, fontStack, monoStack } from "../ui/tokens";
import { TennisBall, bounceY } from "../ui/TennisBall";

export const Closing: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps, width, height } = useVideoConfig();

  // Wordmark spring entrance
  const wordmarkSpring = spring({
    frame,
    fps,
    config: { damping: 14, mass: 0.7, stiffness: 120 },
  });
  const wordmarkScale = interpolate(wordmarkSpring, [0, 1], [0.9, 1]);
  const wordmarkOpacity = interpolate(frame, [0, 16], [0, 1], { extrapolateRight: "clamp" });

  // Tagline
  const taglineOpacity = interpolate(frame, [16, 32], [0, 1], { extrapolateRight: "clamp" });
  const taglineY = interpolate(frame, [16, 32], [12, 0], {
    extrapolateRight: "clamp",
    easing: Easing.out(Easing.cubic),
  });

  // CTA spring
  const ctaSpring = spring({
    frame: frame - 30,
    fps,
    config: { damping: 14, mass: 0.7, stiffness: 130 },
  });
  const ctaScale = interpolate(ctaSpring, [0, 1], [0.9, 1]);
  const ctaOpacity = interpolate(frame, [30, 46], [0, 1], { extrapolateRight: "clamp" });

  // Bouncing ball travels across underneath the CTA
  const ballProgress = interpolate(frame, [40, 88], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.4, 0, 0.6, 1),
  });
  const ballX = interpolate(ballProgress, [0, 1], [-80, width + 80]);
  const groundY = height * 0.78;
  const ballOffset = bounceY(ballProgress, 3, 90, 0.6);
  const ballY = groundY + ballOffset;
  const ballRotation = ballProgress * 720;
  const ballVisible = ballProgress > 0 && ballProgress < 1;

  // Subtle glow pulse on CTA after 50 frames
  const glow = frame > 50 ? (Math.sin((frame - 50) / 8) + 1) / 2 : 0;

  return (
    <AbsoluteFill style={{ background: colors.dark, fontFamily: fontStack }}>
      <AbsoluteFill
        style={{
          background:
            "radial-gradient(circle at 50% 55%, rgba(45,80,22,0.42) 0%, rgba(10,10,10,0) 65%)",
          pointerEvents: "none",
        }}
      />

      {/* Court line decoration */}
      <CourtLines />

      <AbsoluteFill
        style={{
          justifyContent: "center",
          alignItems: "center",
          flexDirection: "column",
          gap: 0,
        }}
      >
        <div
          style={{
            color: "white",
            fontSize: 132,
            fontWeight: 700,
            letterSpacing: "-0.04em",
            opacity: wordmarkOpacity,
            transform: `scale(${wordmarkScale})`,
            lineHeight: 1,
          }}
        >
          Coach<span style={{ color: colors.lime }}>OS</span>
        </div>

        <div
          style={{
            marginTop: 26,
            color: "rgba(255,255,255,0.72)",
            fontSize: 24,
            fontWeight: 400,
            letterSpacing: "-0.01em",
            textAlign: "center",
            opacity: taglineOpacity,
            transform: `translateY(${taglineY}px)`,
            maxWidth: 720,
            lineHeight: 1.4,
          }}
        >
          Plan je seizoen in een middag.
          <br />
          Daarna doet de software de rest.
        </div>

        <div
          style={{
            marginTop: 48,
            display: "inline-flex",
            alignItems: "center",
            gap: 14,
            padding: "16px 28px",
            background: colors.lime,
            color: colors.green,
            borderRadius: 999,
            fontSize: 18,
            fontWeight: 800,
            letterSpacing: "-0.01em",
            opacity: ctaOpacity,
            transform: `scale(${ctaScale})`,
            boxShadow: `0 0 ${50 * glow}px rgba(208,255,20,${0.5 * glow}), 0 12px 30px rgba(208,255,20,0.18)`,
          }}
        >
          <span>Probeer 30 dagen gratis</span>
          <span
            style={{
              fontFamily: monoStack,
              fontSize: 15,
              opacity: 0.7,
              fontWeight: 700,
            }}
          >
            coach-os.be
          </span>
        </div>
      </AbsoluteFill>

      {/* Bouncing tennis ball */}
      {ballVisible && (
        <div
          style={{
            position: "absolute",
            left: ballX,
            top: ballY,
            transform: "translate(-50%, -50%)",
          }}
        >
          <TennisBall size={48} rotation={ballRotation} />
        </div>
      )}
    </AbsoluteFill>
  );
};

const CourtLines: React.FC = () => {
  const frame = useCurrentFrame();
  const dash = interpolate(frame, [0, 90], [0, 100], { extrapolateRight: "clamp" });
  return (
    <AbsoluteFill style={{ opacity: 0.06 }}>
      <svg width="100%" height="100%" viewBox="0 0 1920 1080" preserveAspectRatio="none">
        <g
          stroke={colors.lime}
          strokeWidth="1.5"
          fill="none"
          strokeDasharray="6 8"
          strokeDashoffset={dash}
        >
          <rect x="160" y="120" width="1600" height="840" />
          <line x1="160" y1="540" x2="1760" y2="540" />
          <line x1="960" y1="120" x2="960" y2="960" />
          <rect x="160" y="320" width="320" height="440" />
          <rect x="1440" y="320" width="320" height="440" />
        </g>
      </svg>
    </AbsoluteFill>
  );
};
