import { ImageResponse } from "next/og";

export const alt = "CoachOS — Lesplanning voor tennis- en padelclubs";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function OpengraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          justifyContent: "space-between",
          padding: 80,
          background: "#fdfcf9",
          fontFamily: "ui-sans-serif, system-ui, sans-serif",
          color: "#161513",
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 20 }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              width: 64,
              height: 64,
              background: "#D0FF14",
              borderRadius: 12,
              color: "#161513",
              fontSize: 36,
              fontWeight: 800,
              fontFamily: "ui-monospace, SFMono-Regular, monospace",
              letterSpacing: -2,
            }}
          >
            c/
          </div>
          <div
            style={{
              fontSize: 36,
              fontWeight: 700,
              letterSpacing: -1,
            }}
          >
            CoachOS
          </div>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 24 }}>
          <div
            style={{
              fontSize: 18,
              fontWeight: 600,
              textTransform: "uppercase",
              letterSpacing: 4,
              color: "#4a4741",
              fontFamily: "ui-monospace, SFMono-Regular, monospace",
            }}
          >
            LESPLANNING / BENELUX
          </div>
          <div
            style={{
              fontSize: 76,
              fontWeight: 700,
              letterSpacing: -2,
              lineHeight: 1.05,
              maxWidth: 1000,
            }}
          >
            Lesreeksen, inschrijvingen en betalingen voor tennis- en padelclubs.
          </div>
        </div>

        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            paddingTop: 40,
            borderTop: "2px solid #161513",
            fontSize: 24,
            color: "#4a4741",
          }}
        >
          <div>Een planning die zichzelf bevestigt.</div>
          <div style={{ fontWeight: 600, color: "#161513" }}>coach-os.be</div>
        </div>
      </div>
    ),
    { ...size },
  );
}
