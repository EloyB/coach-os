import {
  AbsoluteFill,
  Easing,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { colors, fontStack, monoStack } from "../ui/tokens";
import { PhoneFrame } from "../ui/PhoneFrame";

const STUDENTS = [
  { name: "Sofie Janssens",   email: "sofie@example.be",   enterAt: 50 },
  { name: "Tom Verstraeten",  email: "tom@example.be",     enterAt: 56 },
  { name: "Marlies De Smet",  email: "marlies@example.com", enterAt: 62 },
  { name: "Pieter Claes",     email: "pieter@example.be",  enterAt: 68 },
  { name: "Anna Peeters",     email: "anna@example.be",    enterAt: 74 },
  { name: "Bram Vermeulen",   email: "bram@example.be",    enterAt: 80 },
  { name: "Lisa Maes",        email: "lisa@example.be",    enterAt: 86 },
];

const FINAL_COUNT = 14;
const LINK = "coach-os.be/enroll/k4f9-2j";

export const Enrollments: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const headingOpacity = interpolate(frame, [0, 18], [0, 1], { extrapolateRight: "clamp" });
  const headingY = interpolate(frame, [0, 18], [20, 0], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) });

  const phoneSpring = spring({ frame: frame - 6, fps, config: { damping: 16, mass: 0.7, stiffness: 120 } });
  const phoneY = interpolate(phoneSpring, [0, 1], [40, 0]);
  const phoneOpacity = interpolate(frame, [6, 24], [0, 1], { extrapolateRight: "clamp" });

  const cardSpring = spring({ frame: frame - 10, fps, config: { damping: 16, mass: 0.7, stiffness: 120 } });
  const cardY = interpolate(cardSpring, [0, 1], [40, 0]);
  const cardOpacity = interpolate(frame, [10, 28], [0, 1], { extrapolateRight: "clamp" });

  // Message "send" ping at frame ~32
  const sendStart = 32;
  const sentCheckScale = interpolate(frame, [sendStart, sendStart + 10], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.out(Easing.cubic),
  });

  // Counter ticks up — quick at first, slowing to 14 as students populate
  const counter = Math.round(
    interpolate(frame, [44, 110], [0, FINAL_COUNT], {
      extrapolateLeft: "clamp",
      extrapolateRight: "clamp",
      easing: Easing.out(Easing.quad),
    }),
  );

  const visibleCount = STUDENTS.filter((s) => frame >= s.enterAt).length;

  return (
    <AbsoluteFill
      style={{
        background: colors.dark,
        fontFamily: fontStack,
        padding: "60px 80px",
        display: "flex",
        flexDirection: "column",
        gap: 24,
      }}
    >
      <AbsoluteFill
        style={{
          background:
            "radial-gradient(circle at 50% 30%, rgba(45,80,22,0.32) 0%, rgba(10,10,10,0) 60%)",
          pointerEvents: "none",
        }}
      />

      {/* Heading */}
      <div style={{ opacity: headingOpacity, transform: `translateY(${headingY}px)` }}>
        <div style={{ fontFamily: monoStack, fontSize: 12, fontWeight: 700, color: colors.lime, letterSpacing: "0.18em" }}>
          02 / INSCHRIJVINGEN
        </div>
        <h1 style={{ fontSize: 52, fontWeight: 700, color: "white", letterSpacing: "-0.02em", margin: 0, marginTop: 6, lineHeight: 1.05 }}>
          Eén link. <span style={{ color: colors.lime }}>{FINAL_COUNT} inschrijvingen.</span>
        </h1>
      </div>

      {/* Two-column layout */}
      <div
        style={{
          flex: 1,
          display: "grid",
          gridTemplateColumns: "minmax(320px, 0.7fr) 1.3fr",
          gap: 50,
          alignItems: "center",
        }}
      >
        {/* Phone with WhatsApp */}
        <div
          style={{
            display: "flex",
            justifyContent: "center",
            opacity: phoneOpacity,
            transform: `translateY(${phoneY}px)`,
          }}
        >
          <PhoneFrame width={300} height={580}>
            {/* WhatsApp header */}
            <div
              style={{
                background: "#075E54",
                color: "white",
                padding: "10px 14px 12px",
                display: "flex",
                alignItems: "center",
                gap: 10,
              }}
            >
              <div
                style={{
                  width: 32,
                  height: 32,
                  borderRadius: 999,
                  background: "#25D366",
                  display: "grid",
                  placeItems: "center",
                  fontSize: 12,
                  fontWeight: 700,
                  color: "white",
                }}
              >
                TC
              </div>
              <div>
                <div style={{ fontSize: 13, fontWeight: 600 }}>TC Wilrijk · Gevorderden</div>
                <div style={{ fontSize: 10.5, opacity: 0.7 }}>14 deelnemers</div>
              </div>
            </div>
            <div
              style={{
                flex: 1,
                background: "#ECE5DD",
                padding: 12,
                display: "flex",
                flexDirection: "column",
                justifyContent: "flex-end",
              }}
            >
              <div
                style={{
                  alignSelf: "flex-end",
                  maxWidth: "85%",
                  background: "#DCF8C6",
                  padding: "10px 12px",
                  borderRadius: 12,
                  fontSize: 12.5,
                  color: "#0F1513",
                  lineHeight: 1.4,
                  fontFamily:
                    '"SF Pro Text", -apple-system, BlinkMacSystemFont, system-ui, sans-serif',
                }}
              >
                Hey iedereen — schrijf je hier in voor de voorjaarsreeks:
                <div
                  style={{
                    marginTop: 8,
                    background: "white",
                    padding: 9,
                    borderRadius: 6,
                    fontSize: 11,
                    color: "#075E54",
                    wordBreak: "break-all",
                    fontFamily: monoStack,
                  }}
                >
                  {LINK}
                </div>
                <div
                  style={{
                    fontSize: 10,
                    color: "#7a7a7a",
                    marginTop: 4,
                    textAlign: "right",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "flex-end",
                    gap: 4,
                  }}
                >
                  14:32
                  <span style={{ color: frame >= sendStart ? "#34B7F1" : "#9CA3AF", transform: `scale(${frame >= sendStart ? sentCheckScale : 1})` }}>
                    ✓✓
                  </span>
                </div>
              </div>
            </div>
          </PhoneFrame>
        </div>

        {/* Dashboard card */}
        <div
          style={{
            background: "white",
            borderRadius: 14,
            boxShadow: "0 30px 80px rgba(0,0,0,0.45)",
            padding: 24,
            opacity: cardOpacity,
            transform: `translateY(${cardY}px)`,
            display: "flex",
            flexDirection: "column",
            gap: 16,
            maxHeight: 600,
            overflow: "hidden",
          }}
        >
          {/* Card header */}
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <div>
              <div style={{ fontFamily: monoStack, fontSize: 11, fontWeight: 600, color: colors.inkDim, letterSpacing: "0.08em", textTransform: "uppercase" }}>
                Volwassenen — Gevorderd
              </div>
              <div style={{ fontSize: 22, fontWeight: 700, color: colors.ink, letterSpacing: "-0.01em", marginTop: 2 }}>
                Inschrijvingen
              </div>
            </div>
            <div
              style={{
                display: "flex",
                alignItems: "baseline",
                gap: 6,
                padding: "8px 16px",
                background: colors.green,
                color: colors.lime,
                borderRadius: 999,
                fontFamily: monoStack,
                fontWeight: 800,
              }}
            >
              <span style={{ fontSize: 22, lineHeight: 1 }}>{counter}</span>
              <span style={{ fontSize: 13, opacity: 0.7 }}>/ 16</span>
            </div>
          </div>

          {/* Enrollment rows */}
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {STUDENTS.slice(0, visibleCount).map((s, i) => {
              const slotSpring = spring({
                frame: frame - s.enterAt,
                fps,
                config: { damping: 14, mass: 0.5, stiffness: 180 },
              });
              const isLatest = i === visibleCount - 1 && frame < s.enterAt + 18;
              return (
                <div
                  key={s.name}
                  style={{
                    background: isLatest ? "rgba(208,255,20,0.10)" : "white",
                    border: `1px solid ${isLatest ? colors.lime : colors.border}`,
                    borderRadius: 8,
                    padding: "10px 14px",
                    display: "flex",
                    alignItems: "center",
                    gap: 12,
                    transform: `translateY(${interpolate(slotSpring, [0, 1], [-10, 0])}px) scaleY(${slotSpring})`,
                    opacity: slotSpring,
                    transformOrigin: "top",
                    boxShadow: isLatest ? `0 0 0 4px rgba(208,255,20,${0.25 * (1 - (frame - s.enterAt) / 18)})` : "none",
                  }}
                >
                  <div
                    style={{
                      width: 30,
                      height: 30,
                      borderRadius: 999,
                      background: colors.green,
                      color: colors.lime,
                      display: "grid",
                      placeItems: "center",
                      fontSize: 11,
                      fontWeight: 700,
                    }}
                  >
                    {s.name.split(" ").map((p) => p[0]).join("").slice(0, 2)}
                  </div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: 13, fontWeight: 600, color: colors.ink }}>{s.name}</div>
                    <div style={{ fontSize: 11, color: colors.inkDim }}>{s.email}</div>
                  </div>
                  <div
                    style={{
                      padding: "3px 10px",
                      background: "#D1FAE5",
                      color: "#065F46",
                      fontSize: 10.5,
                      fontWeight: 700,
                      borderRadius: 999,
                    }}
                  >
                    Bevestigd
                  </div>
                </div>
              );
            })}
            {visibleCount < STUDENTS.length && counter > visibleCount && (
              <div
                style={{
                  fontFamily: monoStack,
                  fontSize: 11,
                  color: colors.inkHint,
                  textAlign: "center",
                  marginTop: 4,
                }}
              >
                + {Math.max(0, counter - visibleCount)} meer
              </div>
            )}
          </div>
        </div>
      </div>
    </AbsoluteFill>
  );
};
