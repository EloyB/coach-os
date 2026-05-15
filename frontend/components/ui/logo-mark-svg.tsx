interface LogoMarkSvgProps {
  size?: number;
  strokeColor?: string;
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
