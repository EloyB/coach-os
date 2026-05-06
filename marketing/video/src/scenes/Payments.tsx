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

const PAYMENTS = [
  { name: "Sofie Janssens",  amount: 120, enterAt: 60 },
  { name: "Tom Verstraeten", amount: 120, enterAt: 70 },
  { name: "Marlies De Smet", amount: 120, enterAt: 80 },
  { name: "Pieter Claes",    amount: 120, enterAt: 92 },
  { name: "Anna Peeters",    amount: 120, enterAt: 105 },
];

const FINAL_TOTAL_EUR = 1680;

export const Payments: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const headingOpacity = interpolate(frame, [0, 18], [0, 1], { extrapolateRight: "clamp" });
  const headingY = interpolate(frame, [0, 18], [20, 0], { extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) });

  const phoneSpring = spring({ frame: frame - 8, fps, config: { damping: 16, mass: 0.7, stiffness: 110 } });
  const phoneY = interpolate(phoneSpring, [0, 1], [40, 0]);
  const phoneOpacity = interpolate(frame, [8, 26], [0, 1], { extrapolateRight: "clamp" });

  const cardSpring = spring({ frame: frame - 12, fps, config: { damping: 16, mass: 0.7, stiffness: 110 } });
  const cardY = interpolate(cardSpring, [0, 1], [40, 0]);
  const cardOpacity = interpolate(frame, [12, 30], [0, 1], { extrapolateRight: "clamp" });

  // iDEAL highlight at frame 35-45
  const idealHover = frame >= 35 && frame < 45;
  const idealPress = frame >= 45 && frame < 55;
  const idealSelected = frame >= 45;

  // Pay button press
  const payStart = 60;
  const payHover = frame >= payStart && frame < payStart + 6;
  const payPress = frame >= payStart + 6 && frame < payStart + 14;
  const paid = frame >= payStart + 14;
  const payScale = payPress ? 0.96 : payHover ? 1.03 : 1;

  // Success spring
  const successSpring = spring({
    frame: frame - (payStart + 14),
    fps,
    config: { damping: 14, mass: 0.6, stiffness: 160 },
  });

  const visiblePayments = PAYMENTS.filter((p) => frame >= p.enterAt).length;
  const counter = Math.round(
    interpolate(frame, [56, 120], [0, FINAL_TOTAL_EUR], {
      extrapolateLeft: "clamp",
      extrapolateRight: "clamp",
      easing: Easing.out(Easing.quad),
    }),
  );

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
          04 / BETALINGEN
        </div>
        <h1 style={{ fontSize: 52, fontWeight: 700, color: "white", letterSpacing: "-0.02em", margin: 0, marginTop: 6, lineHeight: 1.05 }}>
          Betaal in <span style={{ color: colors.lime }}>één tap</span>.
        </h1>
      </div>

      {/* Layout */}
      <div style={{ flex: 1, display: "grid", gridTemplateColumns: "minmax(320px, 0.7fr) 1.3fr", gap: 50, alignItems: "center" }}>
        {/* Phone with Mollie pay */}
        <div style={{ display: "flex", justifyContent: "center", opacity: phoneOpacity, transform: `translateY(${phoneY}px)` }}>
          <PhoneFrame width={320} height={620}>
            {/* Mollie-ish header */}
            <div style={{ padding: "8px 18px 12px", borderBottom: "1px solid #ECE9E2" }}>
              <div style={{ fontFamily: monoStack, fontSize: 10, fontWeight: 700, color: "#6B7280", letterSpacing: "0.1em" }}>
                MOLLIE · TC WILRIJK
              </div>
              <div style={{ fontSize: 14, fontWeight: 700, color: colors.ink, marginTop: 2 }}>
                Voorjaar 2026 — Gevorderd
              </div>
            </div>

            {/* Order summary */}
            <div style={{ padding: "14px 18px", background: "#FAFAF8", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div style={{ fontSize: 12, color: colors.inkDim }}>Te betalen</div>
              <div style={{ fontFamily: monoStack, fontSize: 20, fontWeight: 800, color: colors.ink }}>
                € 120,00
              </div>
            </div>

            {/* Payment methods */}
            <div style={{ padding: "14px 18px", display: "flex", flexDirection: "column", gap: 8 }}>
              <div style={{ fontFamily: monoStack, fontSize: 10, fontWeight: 700, color: colors.inkDim, letterSpacing: "0.08em", textTransform: "uppercase", marginBottom: 4 }}>
                Kies betaalmethode
              </div>

              <PayMethod
                label="iDEAL"
                tag="Populair"
                tagColor="#D946EF"
                logo={<IdealLogo />}
                selected={idealSelected}
                hover={idealHover}
                pressed={idealPress}
              />
              <PayMethod
                label="Bancontact"
                logo={<BancontactLogo />}
                selected={false}
                hover={false}
                pressed={false}
                muted
              />
              <PayMethod
                label="Apple Pay"
                logo={<AppleLogo />}
                selected={false}
                hover={false}
                pressed={false}
                muted
              />
            </div>

            <div style={{ flex: 1 }} />

            {/* Pay button */}
            <div style={{ padding: "0 18px 18px" }}>
              <div
                style={{
                  height: 46,
                  background: paid ? "#10B981" : colors.green,
                  color: paid ? "white" : colors.lime,
                  borderRadius: 10,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  fontSize: 14,
                  fontWeight: 700,
                  transform: `scale(${payScale})`,
                  gap: 8,
                  boxShadow: payHover ? "0 8px 20px rgba(45,80,22,0.4)" : "0 2px 6px rgba(0,0,0,0.1)",
                }}
              >
                {paid ? (
                  <>
                    <span style={{ transform: `scale(${successSpring})` }}>
                      <CheckIcon />
                    </span>
                    Betaald
                  </>
                ) : (
                  "Betaal € 120,00"
                )}
              </div>
            </div>
          </PhoneFrame>
        </div>

        {/* Side card: live payments list */}
        <div
          style={{
            background: "white",
            borderRadius: 14,
            boxShadow: "0 30px 80px rgba(0,0,0,0.45)",
            padding: 22,
            opacity: cardOpacity,
            transform: `translateY(${cardY}px)`,
            display: "flex",
            flexDirection: "column",
            gap: 14,
            maxHeight: 580,
            overflow: "hidden",
          }}
        >
          {/* Total */}
          <div>
            <div style={{ fontFamily: monoStack, fontSize: 11, fontWeight: 600, color: colors.inkDim, letterSpacing: "0.08em", textTransform: "uppercase" }}>
              Ontvangen vandaag
            </div>
            <div style={{ fontFamily: monoStack, fontSize: 44, fontWeight: 800, color: colors.green, letterSpacing: "-0.02em", marginTop: 4, lineHeight: 1 }}>
              € {counter.toLocaleString("nl-BE")}
            </div>
            <div style={{ fontSize: 12, color: colors.inkDim, marginTop: 4 }}>
              {visiblePayments} van 14 spelers betaald
            </div>
          </div>

          <div style={{ height: 1, background: colors.border }} />

          {/* Payments list */}
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {PAYMENTS.slice(0, visiblePayments).map((p, i) => {
              const slotSpring = spring({
                frame: frame - p.enterAt,
                fps,
                config: { damping: 14, mass: 0.5, stiffness: 180 },
              });
              const isLatest = i === visiblePayments - 1 && frame < p.enterAt + 18;
              return (
                <div
                  key={p.name}
                  style={{
                    background: isLatest ? "rgba(208,255,20,0.10)" : "white",
                    border: `1px solid ${isLatest ? colors.lime : colors.border}`,
                    borderRadius: 8,
                    padding: "9px 12px",
                    display: "flex",
                    alignItems: "center",
                    gap: 10,
                    opacity: slotSpring,
                    transform: `translateY(${interpolate(slotSpring, [0, 1], [-6, 0])}px)`,
                  }}
                >
                  <div
                    style={{
                      width: 26,
                      height: 26,
                      borderRadius: 999,
                      background: "#D1FAE5",
                      color: "#065F46",
                      display: "grid",
                      placeItems: "center",
                    }}
                  >
                    <CheckIcon size={13} />
                  </div>
                  <div style={{ flex: 1, fontSize: 12, fontWeight: 600, color: colors.ink }}>
                    {p.name}
                  </div>
                  <div style={{ fontFamily: monoStack, fontSize: 12, fontWeight: 700, color: colors.ink }}>
                    € {p.amount}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </AbsoluteFill>
  );
};

// ─── Payment method row ─────────────────────────────────────────────────────

const PayMethod: React.FC<{
  label: string;
  tag?: string;
  tagColor?: string;
  logo: React.ReactNode;
  selected: boolean;
  hover: boolean;
  pressed: boolean;
  muted?: boolean;
}> = ({ label, tag, tagColor, logo, selected, hover, pressed, muted }) => (
  <div
    style={{
      padding: "10px 12px",
      background: selected ? "rgba(45,80,22,0.04)" : "white",
      border: `1.5px solid ${selected ? colors.green : colors.border}`,
      borderRadius: 10,
      display: "flex",
      alignItems: "center",
      gap: 10,
      transform: `scale(${pressed ? 0.98 : hover ? 1.01 : 1})`,
      opacity: muted ? 0.5 : 1,
      boxShadow: selected ? "0 0 0 3px rgba(45,80,22,0.12)" : "none",
    }}
  >
    <div style={{ width: 32, height: 22, display: "grid", placeItems: "center" }}>{logo}</div>
    <div style={{ flex: 1 }}>
      <div style={{ fontSize: 13, fontWeight: 700, color: colors.ink }}>{label}</div>
    </div>
    {tag && (
      <div
        style={{
          fontFamily: monoStack,
          fontSize: 9,
          fontWeight: 700,
          padding: "2px 7px",
          borderRadius: 999,
          background: `${tagColor}1A`,
          color: tagColor,
          letterSpacing: "0.05em",
          textTransform: "uppercase",
        }}
      >
        {tag}
      </div>
    )}
    <div
      style={{
        width: 16,
        height: 16,
        borderRadius: 999,
        border: `1.5px solid ${selected ? colors.green : "#D1D5DB"}`,
        background: selected ? colors.green : "white",
        display: "grid",
        placeItems: "center",
      }}
    >
      {selected && <span style={{ width: 6, height: 6, borderRadius: 999, background: "white" }} />}
    </div>
  </div>
);

// ─── Logos ──────────────────────────────────────────────────────────────────

const IdealLogo: React.FC = () => (
  <div
    style={{
      fontFamily: '"SF Pro Display", system-ui, sans-serif',
      fontSize: 11,
      fontWeight: 800,
      color: "#CC0066",
      lineHeight: 1,
    }}
  >
    iDEAL
  </div>
);

const BancontactLogo: React.FC = () => (
  <div style={{ display: "flex", alignItems: "center", gap: 1 }}>
    <span style={{ fontSize: 8, fontWeight: 800, color: "#005498", lineHeight: 1 }}>BC</span>
    <span style={{ fontSize: 8, fontWeight: 800, color: "#FFC832", lineHeight: 1 }}>·</span>
  </div>
);

const AppleLogo: React.FC = () => (
  <svg width={18} height={20} viewBox="0 0 24 24" fill="#0F1513">
    <path d="M17.05 20.28c-.98.95-2.05.8-3.08.35-1.09-.46-2.09-.48-3.24 0-1.44.62-2.2.44-3.06-.35C2.79 15.25 3.51 7.59 9.05 7.31c1.35.07 2.29.74 3.08.8 1.18-.24 2.31-.93 3.57-.84 1.51.12 2.65.72 3.4 1.8-3.12 1.87-2.38 5.98.48 7.13-.57 1.5-1.31 2.99-2.54 4.08zM12.03 7.25c-.15-2.23 1.66-4.07 3.74-4.25.29 2.58-2.34 4.5-3.74 4.25z" />
  </svg>
);

const CheckIcon: React.FC<{ size?: number }> = ({ size = 16 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12" />
  </svg>
);
