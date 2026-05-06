import { ImageResponse } from "next/og";

export const size = { width: 180, height: 180 };
export const contentType = "image/png";

export default function AppleIcon() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#2D5016",
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            width: 132,
            height: 132,
            background: "#D0FF14",
            borderRadius: 28,
            color: "#161513",
            fontSize: 88,
            fontWeight: 800,
            fontFamily: "ui-monospace, SFMono-Regular, monospace",
            letterSpacing: -4,
          }}
        >
          c/
        </div>
      </div>
    ),
    { ...size },
  );
}
