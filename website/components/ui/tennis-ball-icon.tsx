interface TennisBallIconProps {
  className?: string;
  strokeColor?: string;
  strokeWidth?: number;
}

/**
 * Tennis ball logo icon (24x24 viewBox). Used in sidebar and auth pages.
 */
export function TennisBallIcon({
  className = "w-4.5 h-4.5",
  strokeColor = "#2D5016",
  strokeWidth = 2.5,
}: TennisBallIconProps) {
  return (
    <svg viewBox="0 0 24 24" fill="none" className={className} aria-hidden="true">
      <circle cx="12" cy="12" r="9" stroke={strokeColor} strokeWidth={strokeWidth} />
      <path d="M3.5 8.5Q12 6 20.5 8.5" stroke={strokeColor} strokeWidth="1.5" strokeLinecap="round" />
      <path d="M3.5 15.5Q12 18 20.5 15.5" stroke={strokeColor} strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  );
}

/**
 * Larger tennis ball icon for empty states (48x48 viewBox, subtle opacity).
 */
export function TennisBallEmptyIcon({ className = "w-8 h-8" }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" fill="none" className={className} aria-hidden="true">
      <circle cx="24" cy="24" r="18" stroke="#2D5016" strokeWidth="2.5" strokeOpacity="0.4" />
      <path d="M7 18Q24 13 41 18" stroke="#2D5016" strokeWidth="2" strokeLinecap="round" strokeOpacity="0.4" />
      <path d="M7 30Q24 35 41 30" stroke="#2D5016" strokeWidth="2" strokeLinecap="round" strokeOpacity="0.4" />
      <line x1="24" y1="6" x2="24" y2="42" stroke="#2D5016" strokeWidth="2" strokeOpacity="0.4" />
    </svg>
  );
}
