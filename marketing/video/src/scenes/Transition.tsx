import {
  AbsoluteFill,
  Easing,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { colors, fontStack } from "../ui/tokens";
import { TennisBall, bounceY } from "../ui/TennisBall";

export const Transition: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps, width, height } = useVideoConfig();

  // ─── Ball trajectory ────────────────────────────────────────────────────
  // Ball travels left → center across frames 0-30, bouncing twice
  const ballProgress = interpolate(frame, [0, 30], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.5, 0, 0.5, 1),
  });
  const ballX = interpolate(ballProgress, [0, 1], [-200, width / 2]);
  const groundY = height * 0.55;
  const ballYOffset = bounceY(ballProgress, 2.5, 280, 0.5);
  const ballY = groundY + ballYOffset;
  const ballSize = interpolate(ballProgress, [0, 0.7, 1], [120, 180, 220]);
  const ballRotation = ballProgress * 720;

  // Squash on impact at the end of the trajectory
  const impactWindow = frame >= 28 && frame <= 36;
  const squash = impactWindow ? 1 - 0.12 * Math.sin(((frame - 28) / 8) * Math.PI) : 1;

  // ─── Lime flash (radial expand from ball) ───────────────────────────────
  const flashStart = 30;
  const flashProgress = interpolate(frame, [flashStart, flashStart + 14], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.out(Easing.cubic),
  });
  const flashSize = flashProgress * Math.hypot(width, height) * 1.2;
  const flashOpacity = interpolate(
    frame,
    [flashStart, flashStart + 8, flashStart + 22],
    [0, 1, 0],
    { extrapolateLeft: "clamp", extrapolateRight: "clamp" },
  );

  // ─── Background dark fade-in (after flash starts) ───────────────────────
  const darkOpacity = interpolate(frame, [flashStart + 2, flashStart + 18], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  // ─── Wordmark emerges after flash ───────────────────────────────────────
  const wordmarkSpring = spring({
    frame: frame - 42,
    fps,
    config: { damping: 14, mass: 0.7, stiffness: 130 },
  });
  const wordmarkScale = interpolate(wordmarkSpring, [0, 1], [0.85, 1]);
  const wordmarkOpacity = interpolate(frame, [42, 55], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  // Hide the ball after flash kicks in (it "becomes" the wordmark)
  const ballVisible = frame < flashStart + 6;

  return (
    <AbsoluteFill style={{ background: "#F1EFEA", fontFamily: fontStack }}>
      {/* Dark layer fades on top of cream */}
      <AbsoluteFill style={{ background: colors.dark, opacity: darkOpacity }} />

      {/* Lime flash (circular expanding) */}
      {flashProgress > 0 && (
        <AbsoluteFill
          style={{
            justifyContent: "center",
            alignItems: "center",
            pointerEvents: "none",
          }}
        >
          <div
            style={{
              width: flashSize,
              height: flashSize,
              borderRadius: 999,
              background: colors.lime,
              opacity: flashOpacity,
              filter: "blur(8px)",
            }}
          />
        </AbsoluteFill>
      )}

      {/* Bouncing tennis ball */}
      {ballVisible && (
        <div
          style={{
            position: "absolute",
            left: ballX,
            top: ballY,
            transform: `translate(-50%, -50%) scaleY(${squash}) scaleX(${2 - squash})`,
          }}
        >
          <TennisBall size={ballSize} rotation={ballRotation} withGlow />
        </div>
      )}

      {/* Wordmark assembly */}
      <AbsoluteFill
        style={{
          justifyContent: "center",
          alignItems: "center",
          flexDirection: "column",
          opacity: wordmarkOpacity,
        }}
      >
        <div
          style={{
            color: "white",
            fontSize: 124,
            fontWeight: 700,
            letterSpacing: "-0.04em",
            transform: `scale(${wordmarkScale})`,
            lineHeight: 1,
          }}
        >
          Coach<span style={{ color: colors.lime }}>OS</span>
        </div>
        <div
          style={{
            marginTop: 18,
            fontSize: 22,
            fontWeight: 400,
            color: "rgba(255,255,255,0.65)",
            letterSpacing: "-0.01em",
            opacity: interpolate(frame, [50, 60], [0, 1], {
              extrapolateLeft: "clamp",
              extrapolateRight: "clamp",
            }),
          }}
        >
          Een planning die zichzelf doet.
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
