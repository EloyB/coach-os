export function CourtPattern() {
  return (
    <svg
      className="absolute inset-0 w-full h-full"
      viewBox="0 0 520 760"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
    >
      {/* Outer boundary */}
      <rect x="60" y="60" width="400" height="640" stroke="white" strokeWidth="2.5" strokeOpacity="0.15" />
      {/* Singles sidelines */}
      <line x1="110" y1="60" x2="110" y2="700" stroke="white" strokeWidth="1.5" strokeOpacity="0.1" />
      <line x1="410" y1="60" x2="410" y2="700" stroke="white" strokeWidth="1.5" strokeOpacity="0.1" />
      {/* Net */}
      <line x1="60" y1="380" x2="460" y2="380" stroke="white" strokeWidth="3" strokeOpacity="0.2" />
      {/* Service boxes */}
      <line x1="110" y1="192" x2="410" y2="192" stroke="white" strokeWidth="1.5" strokeOpacity="0.12" />
      <line x1="110" y1="568" x2="410" y2="568" stroke="white" strokeWidth="1.5" strokeOpacity="0.12" />
      {/* Center service line */}
      <line x1="260" y1="192" x2="260" y2="568" stroke="white" strokeWidth="1.5" strokeOpacity="0.12" />
      {/* Center marks */}
      <line x1="255" y1="60" x2="265" y2="60" stroke="white" strokeWidth="2.5" strokeOpacity="0.2" />
      <line x1="255" y1="700" x2="265" y2="700" stroke="white" strokeWidth="2.5" strokeOpacity="0.2" />
      {/* Net posts */}
      <circle cx="60" cy="380" r="5" fill="white" fillOpacity="0.2" />
      <circle cx="460" cy="380" r="5" fill="white" fillOpacity="0.2" />
      {/* Decorative circles */}
      <circle cx="420" cy="120" r="80" stroke="white" strokeWidth="1" strokeOpacity="0.06" />
      <circle cx="420" cy="120" r="50" stroke="white" strokeWidth="1" strokeOpacity="0.06" />
    </svg>
  );
}
