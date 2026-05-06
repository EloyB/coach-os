/**
 * Tennis-ball monogram, extracted from the business card logo
 * (`marketing/business-card/card-front.svg`).
 *
 * Pure JSX — safe to import into both browser-rendered components
 * and Next.js `ImageResponse` (Satori) routes (icon, apple-icon,
 * opengraph-image).
 */

interface LogoMarkSvgProps {
  /** Rendered pixel size (square). */
  size?: number;
  /** Stroke colour for the ball + seam. Defaults to ink. */
  strokeColor?: string;
  /**
   * Stroke width in viewBox units (the source uses 7 in a 108×108 box).
   * Bump this for very small renders (e.g. 32×32 favicon) so the line
   * stays crisp after sub-pixel scaling.
   */
  strokeWidth?: number;
}

export function LogoMarkSvg({
  size = 108,
  strokeColor = "#161513",
  strokeWidth = 7,
}: LogoMarkSvgProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 108 108"
      xmlns="http://www.w3.org/2000/svg"
    >
      <g
        stroke={strokeColor}
        strokeWidth={strokeWidth}
        fill="none"
        strokeLinecap="round"
      >
        <circle cx="54" cy="54" r="28" />
        <path d="M 28 54 C 36 42, 44 42, 54 54 C 64 66, 72 66, 80 54" />
      </g>
    </svg>
  );
}
