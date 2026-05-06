import {
  AbsoluteFill,
  interpolate,
  random,
  useCurrentFrame,
} from "remotion";
import { colors, fontStack, monoStack } from "../ui/tokens";

const DAYS = ["MA", "DI", "WO", "DO", "VR", "ZA"];
const HOURS = ["16:00", "17:00", "18:00", "19:00", "20:00", "21:00"];

type Cell = {
  row: number;
  col: number;
  text: string;
  variant: "normal" | "highlight" | "conflict";
  enterFrame: number;
};

const CELLS: Cell[] = [
  { row: 0, col: 0, text: "T17 C1 EB",  variant: "normal", enterFrame: 30 },
  { row: 0, col: 2, text: "T17 C1 JL",  variant: "normal", enterFrame: 34 },
  { row: 0, col: 4, text: "MINI C2",     variant: "highlight", enterFrame: 38 },
  { row: 1, col: 1, text: "P18 P1",      variant: "normal", enterFrame: 42 },
  { row: 1, col: 3, text: "T18 C3 EB",  variant: "normal", enterFrame: 46 },
  { row: 1, col: 4, text: "T18 C3 ??",  variant: "conflict", enterFrame: 96 },
  { row: 2, col: 0, text: "T19 C2 EB",  variant: "normal", enterFrame: 50 },
  { row: 2, col: 1, text: "T19 C3 JL",  variant: "normal", enterFrame: 54 },
  { row: 2, col: 2, text: "P19 P3 MK",  variant: "normal", enterFrame: 58 },
  { row: 2, col: 4, text: "T19 C2 EB",  variant: "conflict", enterFrame: 102 },
  { row: 3, col: 0, text: "COMP C4 EB", variant: "highlight", enterFrame: 62 },
  { row: 3, col: 2, text: "T20 C1 JL",  variant: "normal", enterFrame: 66 },
  { row: 3, col: 3, text: "P20 P2 MK",  variant: "normal", enterFrame: 70 },
  { row: 3, col: 5, text: "JEUGD C2",   variant: "highlight", enterFrame: 74 },
  { row: 4, col: 1, text: "T20 C3 EB",  variant: "normal", enterFrame: 78 },
  { row: 4, col: 3, text: "P20 P1 ??",  variant: "conflict", enterFrame: 108 },
  { row: 4, col: 4, text: "T21 C1 JL",  variant: "normal", enterFrame: 82 },
  { row: 5, col: 0, text: "T21 C2 EB",  variant: "normal", enterFrame: 86 },
  { row: 5, col: 2, text: "P21 P3",     variant: "normal", enterFrame: 90 },
];

const STICKIES: { x: number; y: number; rot: number; text: string; color: string; enterFrame: number }[] = [
  { x: 70,   y: 380, rot: -6, text: "Wanneer is\nmijn les?",        color: "#FEF08A", enterFrame: 65 },
  { x: 1480, y: 240, rot: 4,  text: "Heb je het\nrooster nog?",      color: "#FECACA", enterFrame: 78 },
  { x: 1520, y: 720, rot: -3, text: "Conflict op\nwoensdag!",        color: "#FECACA", enterFrame: 92 },
  { x: 100,  y: 760, rot: 5,  text: "Padel\nof tennis?",             color: "#FEF08A", enterFrame: 105 },
  { x: 1620, y: 480, rot: -8, text: "??",                            color: "#FECACA", enterFrame: 118 },
];

export const Problem: React.FC = () => {
  const frame = useCurrentFrame();

  const bgOpacity = interpolate(frame, [0, 14], [0, 1], { extrapolateRight: "clamp" });
  const titleOpacity = interpolate(frame, [4, 22], [0, 1], { extrapolateRight: "clamp" });
  const gridOpacity = interpolate(frame, [14, 30], [0, 1], { extrapolateRight: "clamp" });

  // Late-stage shake to convey chaos
  const shakeStart = 110;
  const shakeAmp = interpolate(frame, [shakeStart, 145], [0, 4], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const shakeX = Math.sin(frame * 1.7) * shakeAmp;
  const shakeY = Math.cos(frame * 2.1) * shakeAmp * 0.6;

  return (
    <AbsoluteFill style={{ background: "#F1EFEA", fontFamily: fontStack, opacity: bgOpacity }}>
      {/* Subtle paper texture via radial gradient */}
      <AbsoluteFill
        style={{
          background:
            "radial-gradient(ellipse at center, rgba(255,255,255,0.5) 0%, rgba(0,0,0,0.04) 100%)",
          pointerEvents: "none",
        }}
      />

      {/* Title strip */}
      <div
        style={{
          position: "absolute",
          top: 60,
          left: 80,
          opacity: titleOpacity,
          fontFamily: monoStack,
          fontSize: 13,
          fontWeight: 700,
          color: "#6B6660",
          letterSpacing: "0.18em",
          textTransform: "uppercase",
        }}
      >
        Lessenrooster_Q2-2026.xlsx · Niet opgeslagen
      </div>

      {/* Spreadsheet container */}
      <div
        style={{
          position: "absolute",
          top: 110,
          left: 80,
          right: 80,
          bottom: 100,
          background: "white",
          border: "1px solid #D8D4CC",
          borderRadius: 4,
          boxShadow: "0 2px 8px rgba(0,0,0,0.06)",
          opacity: gridOpacity,
          transform: `translate(${shakeX}px, ${shakeY}px) rotate(${shakeAmp * 0.05}deg)`,
          overflow: "hidden",
          display: "flex",
          flexDirection: "column",
        }}
      >
        {/* Excel-style toolbar bar */}
        <div
          style={{
            height: 36,
            background: "#F5F4F1",
            borderBottom: "1px solid #D8D4CC",
            display: "flex",
            alignItems: "center",
            padding: "0 16px",
            gap: 14,
            fontFamily: monoStack,
            fontSize: 11,
            color: "#6B6660",
          }}
        >
          <span style={{ fontWeight: 700 }}>Bestand</span>
          <span>Bewerken</span>
          <span>Beeld</span>
          <span>Invoegen</span>
          <span>Opmaak</span>
          <span>Hulp</span>
          <span style={{ marginLeft: "auto", color: "#999" }}>A1: Lessenrooster Q2</span>
        </div>

        {/* Header row */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: `64px repeat(${DAYS.length}, 1fr)`,
            background: "#F5F4F1",
            borderBottom: "1px solid #D8D4CC",
          }}
        >
          <div style={{ borderRight: "1px solid #D8D4CC" }} />
          {DAYS.map((d) => (
            <div
              key={d}
              style={{
                padding: "10px 0",
                textAlign: "center",
                fontFamily: monoStack,
                fontSize: 12,
                fontWeight: 700,
                color: "#3F3D38",
                borderRight: "1px solid #D8D4CC",
              }}
            >
              {d}
            </div>
          ))}
        </div>

        {/* Body */}
        <div
          style={{
            flex: 1,
            display: "grid",
            gridTemplateColumns: `64px repeat(${DAYS.length}, 1fr)`,
            gridTemplateRows: `repeat(${HOURS.length}, 1fr)`,
          }}
        >
          {HOURS.map((h, ri) => (
            <>
              <div
                key={`h-${ri}`}
                style={{
                  borderRight: "1px solid #D8D4CC",
                  borderBottom: ri < HOURS.length - 1 ? "1px solid #ECE9E2" : "none",
                  background: "#F9F7F2",
                  padding: "0 10px",
                  display: "flex",
                  alignItems: "center",
                  fontFamily: monoStack,
                  fontSize: 11,
                  color: "#6B6660",
                }}
              >
                {h}
              </div>
              {DAYS.map((_, ci) => {
                const cell = CELLS.find((c) => c.row === ri && c.col === ci);
                const visible = cell && frame >= cell.enterFrame;
                const cellOpacity = cell
                  ? interpolate(frame, [cell.enterFrame, cell.enterFrame + 6], [0, 1], {
                      extrapolateLeft: "clamp",
                      extrapolateRight: "clamp",
                    })
                  : 0;
                const cellY = cell
                  ? interpolate(frame, [cell.enterFrame, cell.enterFrame + 6], [-4, 0], {
                      extrapolateLeft: "clamp",
                      extrapolateRight: "clamp",
                    })
                  : 0;

                const bg =
                  cell?.variant === "highlight" ? "#FEF3C7"
                  : cell?.variant === "conflict" ? "#FEE2E2"
                  : "white";
                const border =
                  cell?.variant === "conflict" ? "1.5px solid #DC2626" : "1px solid #ECE9E2";

                return (
                  <div
                    key={`c-${ri}-${ci}`}
                    style={{
                      borderRight: ci < DAYS.length - 1 ? "1px solid #ECE9E2" : "none",
                      borderBottom: ri < HOURS.length - 1 ? "1px solid #ECE9E2" : "none",
                      padding: 6,
                      position: "relative",
                    }}
                  >
                    {visible && cell && (
                      <div
                        style={{
                          width: "100%",
                          height: "100%",
                          background: bg,
                          border,
                          borderRadius: 2,
                          padding: "4px 8px",
                          fontFamily: monoStack,
                          fontSize: 11,
                          fontWeight: 600,
                          color: cell.variant === "conflict" ? "#991B1B" : "#3F3D38",
                          opacity: cellOpacity,
                          transform: `translateY(${cellY}px)`,
                          display: "flex",
                          alignItems: "center",
                        }}
                      >
                        {cell.text}
                      </div>
                    )}
                  </div>
                );
              })}
            </>
          ))}
        </div>
      </div>

      {/* Sticky notes flying in */}
      {STICKIES.map((s, i) => {
        const visible = frame >= s.enterFrame;
        if (!visible) return null;
        const t = (frame - s.enterFrame) / 18;
        const t1 = Math.max(0, Math.min(1, t));
        const opacity = t1;
        const scale = 0.6 + t1 * 0.4 + (1 - t1) * 0.05 * Math.sin(frame * 0.4);
        const wobble = Math.sin(frame * 0.15 + i) * 1.5;
        return (
          <div
            key={i}
            style={{
              position: "absolute",
              left: s.x,
              top: s.y,
              width: 160,
              minHeight: 110,
              background: s.color,
              padding: "16px 14px",
              transform: `rotate(${s.rot + wobble * 0.3}deg) scale(${scale})`,
              boxShadow: "0 12px 30px rgba(0,0,0,0.18), 0 2px 4px rgba(0,0,0,0.06)",
              fontFamily: '"Caveat", "Comic Sans MS", cursive',
              fontSize: 22,
              fontWeight: 700,
              color: "#3F3D38",
              lineHeight: 1.15,
              whiteSpace: "pre-line",
              opacity,
            }}
          >
            {s.text}
          </div>
        );
      })}
    </AbsoluteFill>
  );
};

// silence unused random import (kept for future variants)
void random;
