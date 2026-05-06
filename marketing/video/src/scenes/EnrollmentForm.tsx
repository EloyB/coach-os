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

const NAME = "Sofie Janssens";
const EMAIL = "sofie@example.be";

type Pref = "yes" | "ok" | "no";

const TIMESLOTS = [
  { day: "MA", time: "19:00–20:30", court: "Court 3", pref: "yes" as Pref, enterAt: 92 },
  { day: "WO", time: "19:00–20:30", court: "Padel 1", pref: "ok" as Pref,  enterAt: 100 },
  { day: "DO", time: "19:30–21:00", court: "Court 4", pref: "no" as Pref,  enterAt: 108 },
];

const PREF_COLORS: Record<Pref, { bg: string; border: string; text: string; label: string }> = {
  yes: { bg: "#D1FAE5", border: "#10B981", text: "#065F46", label: "Voorkeur" },
  ok:  { bg: "#FEF3C7", border: "#F59E0B", text: "#92400E", label: "Kan ook" },
  no:  { bg: "#FECACA", border: "#EF4444", text: "#991B1B", label: "Niet" },
};

export const EnrollmentForm: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const headingOpacity = interpolate(frame, [0, 18], [0, 1], { extrapolateRight: "clamp" });
  const headingY = interpolate(frame, [0, 18], [20, 0], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) });

  const phoneSpring = spring({ frame: frame - 8, fps, config: { damping: 16, mass: 0.7, stiffness: 110 } });
  const phoneY = interpolate(phoneSpring, [0, 1], [40, 0]);
  const phoneOpacity = interpolate(frame, [8, 26], [0, 1], { extrapolateRight: "clamp" });

  // Typing
  const nameLen = interpolate(frame, [30, 56], [0, NAME.length], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const typedName = NAME.slice(0, Math.floor(nameLen));
  const cursorName = frame >= 30 && frame < 56;

  const emailLen = interpolate(frame, [60, 88], [0, EMAIL.length], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  const typedEmail = EMAIL.slice(0, Math.floor(emailLen));
  const cursorEmail = frame >= 60 && frame < 88;

  // Submit button
  const submitStart = 120;
  const submitHover = frame >= submitStart && frame < submitStart + 6;
  const submitPress = frame >= submitStart + 6 && frame < submitStart + 14;
  const submitScale = submitPress ? 0.96 : submitHover ? 1.03 : 1;
  const submitted = frame >= submitStart + 14;

  // Success overlay
  const successSpring = spring({
    frame: frame - (submitStart + 14),
    fps,
    config: { damping: 14, mass: 0.6, stiffness: 160 },
  });

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
          03 / INSCHRIJFFORMULIER
        </div>
        <h1 style={{ fontSize: 52, fontWeight: 700, color: "white", letterSpacing: "-0.02em", margin: 0, marginTop: 6, lineHeight: 1.05 }}>
          Voorkeuren in <span style={{ color: colors.lime }}>30 seconden</span>.
        </h1>
      </div>

      {/* Phone center */}
      <div
        style={{
          flex: 1,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          position: "relative",
        }}
      >
        <div
          style={{
            opacity: phoneOpacity,
            transform: `translateY(${phoneY}px)`,
            position: "relative",
          }}
        >
          <PhoneFrame width={400} height={780}>
            {/* Header */}
            <div style={{ padding: "8px 22px 12px", borderBottom: "1px solid #ECE9E2" }}>
              <div style={{ fontFamily: monoStack, fontSize: 10.5, fontWeight: 700, color: colors.green, letterSpacing: "0.1em" }}>
                TC WILRIJK
              </div>
              <div style={{ fontSize: 18, fontWeight: 700, color: colors.ink, letterSpacing: "-0.01em", marginTop: 2 }}>
                Voorjaar 2026 · Gevorderd
              </div>
            </div>

            <div style={{ padding: "16px 22px", display: "flex", flexDirection: "column", gap: 14, flex: 1 }}>
              <FormField label="Naam" value={typedName} cursor={cursorName} placeholder="Volledige naam" />
              <FormField label="E-mail" value={typedEmail} cursor={cursorEmail} placeholder="naam@example.be" />

              {/* Availability grid */}
              <div>
                <FieldLabel>Beschikbaarheid</FieldLabel>
                <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                  {TIMESLOTS.map((s) => {
                    const visible = frame >= s.enterAt;
                    if (!visible) return null;
                    const slotSpring = spring({
                      frame: frame - s.enterAt,
                      fps,
                      config: { damping: 14, mass: 0.5, stiffness: 180 },
                    });
                    const c = PREF_COLORS[s.pref];
                    return (
                      <div
                        key={s.day + s.time}
                        style={{
                          padding: "10px 12px",
                          background: "#FAFAF8",
                          border: `1px solid ${colors.border}`,
                          borderRadius: 8,
                          display: "flex",
                          alignItems: "center",
                          gap: 8,
                          transform: `translateY(${interpolate(slotSpring, [0, 1], [-6, 0])}px)`,
                          opacity: slotSpring,
                        }}
                      >
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{ fontSize: 12, fontWeight: 700, color: colors.ink, fontFamily: monoStack }}>
                            {s.day} · {s.time}
                          </div>
                          <div style={{ fontSize: 10.5, color: colors.inkDim, marginTop: 1 }}>
                            {s.court}
                          </div>
                        </div>
                        <div
                          style={{
                            padding: "4px 10px",
                            background: c.bg,
                            border: `1.5px solid ${c.border}`,
                            color: c.text,
                            borderRadius: 999,
                            fontSize: 10.5,
                            fontWeight: 700,
                            whiteSpace: "nowrap",
                          }}
                        >
                          {c.label}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>

              <div style={{ flex: 1 }} />

              {/* Submit button */}
              <div
                style={{
                  height: 48,
                  background: submitted ? "#10B981" : colors.green,
                  color: submitted ? "white" : colors.lime,
                  borderRadius: 12,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  fontSize: 15,
                  fontWeight: 700,
                  transform: `scale(${submitScale})`,
                  boxShadow: submitHover ? "0 8px 20px rgba(45,80,22,0.4)" : "0 2px 6px rgba(0,0,0,0.1)",
                  gap: 10,
                }}
              >
                {submitted ? (
                  <>
                    <span style={{ transform: `scale(${successSpring})` }}>
                      <CheckIcon size={18} />
                    </span>
                    Ingeschreven
                  </>
                ) : (
                  "Inschrijven →"
                )}
              </div>
            </div>
          </PhoneFrame>
        </div>
      </div>
    </AbsoluteFill>
  );
};

const FieldLabel: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div
    style={{
      fontFamily: monoStack,
      fontSize: 10.5,
      fontWeight: 700,
      color: colors.inkDim,
      letterSpacing: "0.08em",
      textTransform: "uppercase",
      marginBottom: 6,
    }}
  >
    {children}
  </div>
);

const FormField: React.FC<{
  label: string;
  value: string;
  cursor: boolean;
  placeholder: string;
}> = ({ label, value, cursor, placeholder }) => (
  <div>
    <FieldLabel>{label}</FieldLabel>
    <div
      style={{
        height: 40,
        padding: "0 14px",
        display: "flex",
        alignItems: "center",
        background: "white",
        border: `1.5px solid ${value || cursor ? colors.green : colors.border}`,
        borderRadius: 10,
        fontSize: 13.5,
        color: value ? colors.ink : "#9CA3AF",
        boxShadow: cursor ? "0 0 0 3px rgba(45,80,22,0.10)" : "none",
      }}
    >
      {value || placeholder}
      {cursor && (
        <span
          style={{
            display: "inline-block",
            width: 1.5,
            height: 16,
            background: colors.green,
            marginLeft: 1,
          }}
        />
      )}
    </div>
  </div>
);

const CheckIcon: React.FC<{ size?: number }> = ({ size = 16 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12" />
  </svg>
);
