import { ImageResponse } from "next/og";
import { LogoMarkSvg } from "@/components/site/logo-mark-svg";

export const contentType = "image/png";

// High-resolution PNG of the CoachOS brand mark for use in external surfaces
// (email signatures, third-party embeds) where the 32×32 favicon at /icon is
// too low-res. Scales down crisply to typical signature display sizes (40–64px).
export function GET() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#D0FF14",
          borderRadius: 48,
        }}
      >
        <LogoMarkSvg size={182} strokeWidth={7} />
      </div>
    ),
    { width: 256, height: 256 },
  );
}
