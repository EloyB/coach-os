import { COLORS, FONTS } from '../brand';

// Static recreation of the real CoachOS dashboard sidebar.
// Matches frontend/components/layouts/dashboard-sidebar.tsx visually, but
// uses inline styles (Remotion convention) and inlined SVG icons (no lucide).

const NAV_ITEMS = [
  { key: 'today', label: 'Vandaag', icon: 'dashboard' as const },
  { key: 'lessons', label: 'Lesreeksen', icon: 'book' as const, active: true },
  { key: 'oneoff', label: 'Losse lessen', icon: 'ticket' as const },
  { key: 'trainers', label: 'Trainers', icon: 'cap' as const },
  { key: 'settings', label: 'Instellingen', icon: 'settings' as const },
];

const Icon: React.FC<{ name: 'dashboard' | 'book' | 'ticket' | 'cap' | 'settings'; color: string }> = ({
  name,
  color,
}) => {
  const common = {
    width: 14.5,
    height: 14.5,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: color,
    strokeWidth: 2,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
  };
  if (name === 'dashboard') {
    return (
      <svg {...common}>
        <rect x="3" y="3" width="7" height="7" rx="1" />
        <rect x="14" y="3" width="7" height="7" rx="1" />
        <rect x="14" y="14" width="7" height="7" rx="1" />
        <rect x="3" y="14" width="7" height="7" rx="1" />
      </svg>
    );
  }
  if (name === 'book') {
    return (
      <svg {...common}>
        <path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z" />
        <path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z" />
      </svg>
    );
  }
  if (name === 'ticket') {
    return (
      <svg {...common}>
        <path d="M2 9V7a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v2a2 2 0 0 0 0 4v2a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-2a2 2 0 0 0 0-4z" />
        <path d="M13 5v2M13 11v2M13 17v2" />
      </svg>
    );
  }
  if (name === 'cap') {
    return (
      <svg {...common}>
        <path d="M22 10v6M2 10l10-5 10 5-10 5z" />
        <path d="M6 12v5c3 3 9 3 12 0v-5" />
      </svg>
    );
  }
  return (
    <svg {...common}>
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
    </svg>
  );
};

const LogOutIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <polyline points="16 17 21 12 16 7" />
    <line x1="21" y1="12" x2="9" y2="12" />
  </svg>
);

const ChevronIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" style={{ transform: 'rotate(90deg)' }}>
    <polyline points="9 18 15 12 9 6" />
  </svg>
);

const CourtLinesBackdrop: React.FC = () => (
  <svg
    style={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}
    viewBox="0 0 224 760"
    width="224"
    height="760"
    preserveAspectRatio="none"
  >
    <g stroke={COLORS.white} fill="none" opacity={0.05}>
      <rect x="14" y="40" width="196" height="680" strokeWidth="1.5" />
      <line x1="112" y1="40" x2="112" y2="720" strokeWidth="1.5" />
      <line x1="14" y1="380" x2="210" y2="380" strokeWidth="1" />
    </g>
  </svg>
);

export const SidebarMock: React.FC = () => {
  return (
    <aside
      style={{
        position: 'relative',
        width: 224,
        flexShrink: 0,
        background: COLORS.tennisGreen,
        display: 'flex',
        flexDirection: 'column',
        fontFamily: FONTS.sans,
        color: COLORS.white,
        overflow: 'hidden',
      }}
    >
      <CourtLinesBackdrop />

      {/* Logo + club switcher */}
      <div style={{ position: 'relative', zIndex: 10, padding: '20px 18px 18px 18px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div
            style={{
              width: 28,
              height: 28,
              borderRadius: 6,
              background: COLORS.tennisLime,
              display: 'grid',
              placeItems: 'center',
            }}
          >
            <span
              style={{
                color: COLORS.tennisGreen,
                fontFamily: FONTS.mono,
                fontWeight: 800,
                fontSize: 13,
                lineHeight: 1,
              }}
            >
              c/
            </span>
          </div>
          <span style={{ fontWeight: 700, fontSize: 15.5, letterSpacing: -0.2 }}>CoachOS</span>
        </div>

        {/* Club switcher tile */}
        <div
          style={{
            marginTop: 16,
            padding: '8px 10px',
            background: 'rgba(0,0,0,0.18)',
            borderRadius: 6,
            display: 'flex',
            alignItems: 'center',
            gap: 8,
          }}
        >
          <div
            style={{
              width: 20,
              height: 20,
              borderRadius: 4,
              background: 'rgba(208,255,20,0.2)',
              display: 'grid',
              placeItems: 'center',
            }}
          >
            <span style={{ color: COLORS.tennisLime, fontFamily: FONTS.mono, fontSize: 10, fontWeight: 800 }}>L</span>
          </div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <p
              style={{
                fontSize: 10,
                color: 'rgba(255,255,255,0.5)',
                textTransform: 'uppercase',
                letterSpacing: '0.08em',
                margin: 0,
                lineHeight: 1.2,
              }}
            >
              Club
            </p>
            <p
              style={{
                fontSize: 11.5,
                fontWeight: 600,
                color: COLORS.white,
                margin: 0,
                whiteSpace: 'nowrap',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                lineHeight: 1.3,
              }}
            >
              TC De Linde
            </p>
          </div>
          <ChevronIcon color="rgba(255,255,255,0.5)" />
        </div>
      </div>

      {/* Nav */}
      <nav style={{ position: 'relative', zIndex: 10, flex: 1, padding: '0 10px' }}>
        <p
          style={{
            margin: '10px 12px 6px 12px',
            fontSize: 9.5,
            color: 'rgba(255,255,255,0.35)',
            fontWeight: 700,
            textTransform: 'uppercase',
            letterSpacing: '0.12em',
          }}
        >
          Werk
        </p>
        {NAV_ITEMS.map((item) => {
          const active = item.active === true;
          return (
            <div
              key={item.key}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 11,
                padding: '8px 12px',
                borderRadius: 6,
                fontSize: 12.5,
                fontWeight: 500,
                background: active ? 'rgba(208,255,20,0.12)' : 'transparent',
                color: active ? COLORS.white : 'rgba(255,255,255,0.7)',
                marginBottom: 1,
              }}
            >
              <Icon name={item.icon} color={active ? COLORS.tennisLime : 'rgba(255,255,255,0.5)'} />
              <span style={{ flex: 1 }}>{item.label}</span>
            </div>
          );
        })}
      </nav>

      {/* Profile */}
      <div
        style={{
          position: 'relative',
          zIndex: 10,
          margin: '0 10px 10px 10px',
          padding: '10px 12px',
          background: 'rgba(0,0,0,0.18)',
          borderRadius: 10,
          display: 'flex',
          alignItems: 'center',
          gap: 10,
        }}
      >
        <div
          style={{
            width: 28,
            height: 28,
            borderRadius: '50%',
            background: COLORS.tennisLime,
            display: 'grid',
            placeItems: 'center',
            flexShrink: 0,
          }}
        >
          <span style={{ color: COLORS.tennisGreen, fontWeight: 800, fontSize: 11 }}>EB</span>
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <p
            style={{
              fontSize: 11.5,
              fontWeight: 600,
              color: COLORS.white,
              margin: 0,
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              lineHeight: 1.3,
            }}
          >
            Eloy Boone
          </p>
          <p style={{ fontSize: 10, color: 'rgba(255,255,255,0.5)', margin: 0 }}>Beheerder</p>
        </div>
        <LogOutIcon color="rgba(255,255,255,0.4)" />
      </div>
    </aside>
  );
};
