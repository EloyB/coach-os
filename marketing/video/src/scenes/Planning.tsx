import {
  AbsoluteFill,
  Easing,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { colors, fontStack, monoStack } from "../ui/tokens";

const DAYS = ["MA", "DI", "WO", "DO", "VR"];
const HOURS = [17, 18, 19, 20, 21];
const ROW_H = 64;

type Slot = {
  day: number;
  startHour: number;
  endHour: number;
  label: string;
  kind: "auto" | "proposed" | "grouped";
  enterAt: number;
};

const SLOTS: Slot[] = [
  { day: 0, startHour: 17,   endHour: 18,   label: "Jeugd",       kind: "auto",     enterAt: 26 },
  { day: 0, startHour: 19,   endHour: 20.5, label: "Volw. Gev.",  kind: "auto",     enterAt: 30 },
  { day: 1, startHour: 18,   endHour: 19.5, label: "Volw. Gev.",  kind: "auto",     enterAt: 34 },
  { day: 2, startHour: 17.5, endHour: 18.5, label: "Mini",        kind: "grouped",  enterAt: 38 },
  { day: 2, startHour: 19,   endHour: 20.5, label: "Padel",       kind: "auto",     enterAt: 42 },
  { day: 3, startHour: 17,   endHour: 18,   label: "Jeugd",       kind: "auto",     enterAt: 46 },
  { day: 3, startHour: 19.5, endHour: 21,   label: "Competitie",  kind: "proposed", enterAt: 50 },
  { day: 4, startHour: 18,   endHour: 19,   label: "Mini",        kind: "grouped",  enterAt: 54 },
  { day: 4, startHour: 20,   endHour: 21.5, label: "Padel",       kind: "auto",     enterAt: 58 },
  { day: 4, startHour: 17,   endHour: 18,   label: "Volw. Beg.",  kind: "auto",     enterAt: 62 },
];

const KIND_COLORS = {
  auto:     { bg: "#F0FDF4", border: "#86EFAC", text: "#14532D" },
  proposed: { bg: "#FFFBEB", border: "#FCD34D", text: "#78350F" },
  grouped:  { bg: "#EFF6FF", border: "#93C5FD", text: "#1E3A8A" },
};

export const Planning: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const headingY = interpolate(frame, [0, 18], [20, 0], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) });
  const headingOpacity = interpolate(frame, [0, 18], [0, 1], { extrapolateRight: "clamp" });

  const chromeSpring = spring({ frame: frame - 8, fps, config: { damping: 16, mass: 0.7, stiffness: 110 } });
  const chromeY = interpolate(chromeSpring, [0, 1], [40, 0]);
  const chromeOpacity = interpolate(frame, [8, 26], [0, 1], { extrapolateRight: "clamp" });

  const filledSlots = SLOTS.filter((s) => frame >= s.enterAt).length;

  // Counter ticks 0 → 10 once slots are placed
  const counterStart = 70;
  const counter = Math.round(
    interpolate(frame, [counterStart, counterStart + 18], [0, 10], {
      extrapolateLeft: "clamp",
      extrapolateRight: "clamp",
      easing: Easing.out(Easing.cubic),
    }),
  );

  const checkSpring = spring({
    frame: frame - 88,
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
      <div
        style={{
          opacity: headingOpacity,
          transform: `translateY(${headingY}px)`,
        }}
      >
        <div
          style={{
            fontFamily: monoStack,
            fontSize: 12,
            fontWeight: 700,
            color: colors.lime,
            letterSpacing: "0.18em",
          }}
        >
          01 / PLANNING
        </div>
        <h1
          style={{
            fontSize: 52,
            fontWeight: 700,
            color: "white",
            letterSpacing: "-0.02em",
            margin: 0,
            marginTop: 6,
            lineHeight: 1.05,
          }}
        >
          Automatisch ingedeeld <span style={{ color: colors.lime }}>in 0,8s</span>
        </h1>
      </div>

      {/* Calendar card */}
      <div
        style={{
          opacity: chromeOpacity,
          transform: `translateY(${chromeY}px)`,
          flex: 1,
          background: "white",
          borderRadius: 14,
          boxShadow: "0 30px 80px rgba(0,0,0,0.45)",
          overflow: "hidden",
          display: "flex",
          flexDirection: "column",
        }}
      >
        {/* Header */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: `64px repeat(${DAYS.length}, 1fr)`,
            background: "#F9FAFB",
            borderBottom: "1px solid #E5E7EB",
          }}
        >
          <div />
          {DAYS.map((d, i) => (
            <div
              key={d}
              style={{
                padding: "14px 0",
                textAlign: "center",
                fontFamily: monoStack,
                fontSize: 12,
                fontWeight: 700,
                color: "#374151",
                borderLeft: i > 0 ? "1px solid #E5E7EB" : "none",
                letterSpacing: "0.06em",
              }}
            >
              {d}
            </div>
          ))}
        </div>

        {/* Body */}
        <div
          style={{
            position: "relative",
            flex: 1,
            display: "grid",
            gridTemplateColumns: `64px repeat(${DAYS.length}, 1fr)`,
          }}
        >
          {/* Hours column */}
          <div>
            {HOURS.map((h, hi) => (
              <div
                key={h}
                style={{
                  height: ROW_H,
                  padding: "6px 8px",
                  fontFamily: monoStack,
                  fontSize: 11,
                  color: "#9CA3AF",
                  textAlign: "right",
                  borderTop: hi > 0 ? "1px solid #F3F4F6" : "none",
                }}
              >
                {String(h).padStart(2, "0")}:00
              </div>
            ))}
          </div>

          {/* Day columns */}
          {DAYS.map((_, dayIdx) => (
            <div
              key={dayIdx}
              style={{
                position: "relative",
                borderLeft: "1px solid #F3F4F6",
              }}
            >
              {HOURS.map((_, hi) => (
                <div
                  key={hi}
                  style={{
                    height: ROW_H,
                    borderTop: hi > 0 ? "1px solid #F3F4F6" : "none",
                    background: hi % 2 === 0 ? "transparent" : "rgba(15,21,19,0.012)",
                  }}
                />
              ))}
              {/* Slots */}
              {SLOTS.filter((s) => s.day === dayIdx).map((slot) => {
                const visible = frame >= slot.enterAt;
                if (!visible) return null;
                const slotSpring = spring({
                  frame: frame - slot.enterAt,
                  fps,
                  config: { damping: 12, mass: 0.5, stiffness: 180 },
                });
                const top = (slot.startHour - HOURS[0]) * ROW_H + 3;
                const height = (slot.endHour - slot.startHour) * ROW_H - 6;
                const c = KIND_COLORS[slot.kind];
                return (
                  <div
                    key={`${dayIdx}-${slot.startHour}`}
                    style={{
                      position: "absolute",
                      left: 4,
                      right: 4,
                      top,
                      height,
                      background: c.bg,
                      border: `1.5px solid ${c.border}`,
                      borderRadius: 8,
                      padding: "8px 10px",
                      transform: `scale(${slotSpring})`,
                      transformOrigin: "top center",
                      boxShadow: "0 4px 10px rgba(15,21,19,0.06)",
                      display: "flex",
                      flexDirection: "column",
                      justifyContent: "space-between",
                    }}
                  >
                    <div
                      style={{
                        fontSize: 13,
                        fontWeight: 700,
                        color: c.text,
                        lineHeight: 1.1,
                      }}
                    >
                      {slot.label}
                    </div>
                    <div
                      style={{
                        fontFamily: monoStack,
                        fontSize: 10.5,
                        color: c.text,
                        opacity: 0.75,
                      }}
                    >
                      {fmt(slot.startHour)}–{fmt(slot.endHour)}
                    </div>
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      </div>

      {/* Counter pill bottom-right */}
      <div
        style={{
          position: "absolute",
          bottom: 80,
          right: 80,
          display: "flex",
          alignItems: "center",
          gap: 14,
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 12,
            padding: "12px 20px",
            background: colors.lime,
            color: colors.dark,
            borderRadius: 999,
            fontWeight: 700,
            fontSize: 18,
            boxShadow: "0 14px 40px rgba(208,255,20,0.35)",
            transform: `scale(${checkSpring})`,
            opacity: interpolate(frame, [86, 100], [0, 1], { extrapolateRight: "clamp" }),
          }}
        >
          <CheckIcon />
          <span style={{ fontFamily: monoStack, fontWeight: 700 }}>
            {String(Math.min(counter, filledSlots)).padStart(2, "0")} lessen ingepland
          </span>
        </div>
      </div>
    </AbsoluteFill>
  );
};

function fmt(h: number): string {
  const hh = Math.floor(h);
  const mm = Math.round((h - hh) * 60);
  return `${String(hh).padStart(2, "0")}:${String(mm).padStart(2, "0")}`;
}

const CheckIcon: React.FC = () => (
  <svg width={18} height={18} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12" />
  </svg>
);
