import { COLORS, FONTS } from '../brand';

// Centered share-link card that springs over the app window after the
// trainer presses "Inschrijflink". The URL is the visual hero — the badge
// caption above just confirms what just happened.

interface ShareLinkRevealProps {
  // 0..1 — opacity/scale of the card
  progress: number;
  scale: number;
  url: string;
}

const CheckCircleIcon: React.FC<{ size?: number; color: string }> = ({ size = 14, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
    <polyline points="22 4 12 14.01 9 11.01" />
  </svg>
);

export const ShareLinkReveal: React.FC<ShareLinkRevealProps> = ({ progress, scale, url }) => (
  <div
    style={{
      position: 'absolute',
      inset: 0,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      pointerEvents: 'none',
      zIndex: 50,
      opacity: progress,
    }}
  >
    <div
      style={{
        transform: `scale(${scale})`,
        background: COLORS.tennisLime,
        borderRadius: 22,
        padding: '32px 44px',
        boxShadow:
          '0 30px 60px -10px rgba(208,255,20,0.45), 0 16px 28px rgba(0,0,0,0.35)',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 18,
        maxWidth: 820,
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          fontSize: 16,
          fontWeight: 600,
          color: COLORS.ink,
          fontFamily: FONTS.sans,
        }}
      >
        <CheckCircleIcon size={18} color={COLORS.ink} />
        <span>Inschrijflink gekopieerd</span>
      </div>
      <div
        style={{
          fontFamily: FONTS.mono,
          fontSize: 26,
          fontWeight: 600,
          color: COLORS.ink,
          letterSpacing: -0.4,
          padding: '14px 22px',
          background: 'rgba(0,0,0,0.06)',
          borderRadius: 12,
          whiteSpace: 'nowrap',
        }}
      >
        {url}
      </div>
      <p
        style={{
          fontSize: 13,
          color: 'rgba(22,21,19,0.7)',
          margin: 0,
          fontFamily: FONTS.sans,
        }}
      >
        Deel de link met je leerlingen — geen account nodig.
      </p>
    </div>
  </div>
);
