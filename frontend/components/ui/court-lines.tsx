export function CourtLines() {
  return (
    <svg
      className="absolute inset-0 w-full h-full opacity-[0.07]"
      viewBox="0 0 240 800"
      fill="none"
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
    >
      <rect x="20" y="60" width="200" height="680" stroke="white" strokeWidth="1.5" />
      <line x1="20" y1="400" x2="220" y2="400" stroke="white" strokeWidth="2" />
      <line x1="20" y1="222" x2="220" y2="222" stroke="white" strokeWidth="1" />
      <line x1="20" y1="578" x2="220" y2="578" stroke="white" strokeWidth="1" />
      <line x1="120" y1="222" x2="120" y2="578" stroke="white" strokeWidth="1" />
      <line x1="115" y1="60" x2="125" y2="60" stroke="white" strokeWidth="2" />
      <line x1="115" y1="740" x2="125" y2="740" stroke="white" strokeWidth="2" />
    </svg>
  );
}
