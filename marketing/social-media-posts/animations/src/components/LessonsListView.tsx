import { COLORS, FONTS } from '../brand';

// Visual recreation of /dashboard/lessons in two states:
//  - empty   : the freshly-onboarded "no lesreeksen yet" state
//  - populated : list with one row (the just-created series), with optional
//               glow on that row driven by glowProgress (0..1).

const PlusIcon: React.FC<{ size?: number; color?: string }> = ({ size = 13, color = COLORS.white }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="12" y1="5" x2="12" y2="19" />
    <line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

const TennisBallCircle: React.FC = () => (
  <div
    style={{
      width: 72,
      height: 72,
      borderRadius: '50%',
      background: 'rgba(45,80,22,0.08)',
      display: 'grid',
      placeItems: 'center',
    }}
  >
    <svg width="38" height="38" viewBox="0 0 60 60">
      <defs>
        <radialGradient id="ball-grad" cx="35%" cy="35%" r="65%">
          <stop offset="0%" stopColor="#E5FF60" />
          <stop offset="55%" stopColor="#D0FF14" />
          <stop offset="100%" stopColor="#A8CC0E" />
        </radialGradient>
      </defs>
      <circle cx="30" cy="30" r="24" fill="url(#ball-grad)" />
      <path
        d="M 8 30 C 16 22, 24 22, 30 30 C 36 38, 44 38, 52 30"
        stroke="#ffffff"
        strokeWidth="2"
        fill="none"
        strokeLinecap="round"
        opacity="0.9"
      />
    </svg>
  </div>
);

interface PageHeaderProps {
  activeCount: number;
  draftCount: number;
  newButtonPressed?: boolean;
  newButtonHover?: boolean;
}

const PageHeader: React.FC<PageHeaderProps> = ({ activeCount, draftCount, newButtonPressed, newButtonHover }) => (
  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', marginBottom: 22 }}>
    <div>
      <p
        style={{
          fontSize: 10.5,
          fontFamily: FONTS.mono,
          color: COLORS.ink3,
          textTransform: 'uppercase',
          letterSpacing: '0.1em',
          margin: 0,
        }}
      >
        / {activeCount} actief · {draftCount} concept
      </p>
      <h1
        style={{
          fontSize: 30,
          fontWeight: 700,
          color: COLORS.ink,
          margin: '6px 0 0 0',
          letterSpacing: -0.6,
          fontFamily: FONTS.sans,
        }}
      >
        Lessen
      </h1>
    </div>
    <button
      type="button"
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        padding: '9px 16px',
        background: newButtonHover ? '#264411' : COLORS.tennisGreen,
        color: COLORS.white,
        fontSize: 13,
        fontWeight: 600,
        borderRadius: 8,
        border: 'none',
        transform: newButtonPressed ? 'scale(0.96)' : 'scale(1)',
        fontFamily: FONTS.sans,
        boxShadow: '0 1px 2px rgba(0,0,0,0.08)',
      }}
    >
      <PlusIcon />
      Nieuwe lesreeks
    </button>
  </div>
);

interface ListEmptyProps {
  newButtonPressed?: boolean;
  newButtonHover?: boolean;
}

export const LessonsListEmpty: React.FC<ListEmptyProps> = ({ newButtonPressed, newButtonHover }) => (
  <div
    style={{
      flex: 1,
      padding: '28px 32px',
      background: COLORS.canvas,
      display: 'flex',
      flexDirection: 'column',
      overflow: 'hidden',
    }}
  >
    <PageHeader
      activeCount={0}
      draftCount={0}
      newButtonPressed={newButtonPressed}
      newButtonHover={newButtonHover}
    />
    <div
      style={{
        flex: 1,
        background: COLORS.paper,
        border: `1px solid ${COLORS.rule}`,
        borderRadius: 14,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 14,
        padding: '40px 20px',
      }}
    >
      <TennisBallCircle />
      <div style={{ textAlign: 'center' }}>
        <p
          style={{
            fontSize: 16,
            fontWeight: 600,
            color: COLORS.ink,
            margin: 0,
            fontFamily: FONTS.sans,
          }}
        >
          Nog geen lesreeksen
        </p>
        <p
          style={{
            fontSize: 13,
            color: COLORS.ink3,
            margin: '6px 0 0',
            fontFamily: FONTS.sans,
            maxWidth: 280,
          }}
        >
          Maak je eerste lesreeks aan en begin met plannen.
        </p>
      </div>
    </div>
  </div>
);

interface ListPopulatedProps {
  glowProgress: number; // 0..1
  scaleProgress?: number; // 0..1 — row scale-in
}

export const LessonsListPopulated: React.FC<ListPopulatedProps> = ({ glowProgress, scaleProgress = 1 }) => {
  const glowAlpha = glowProgress * 0.7;
  const scale = 0.96 + scaleProgress * 0.04;
  return (
    <div
      style={{
        flex: 1,
        padding: '28px 32px',
        background: COLORS.canvas,
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
      }}
    >
      <PageHeader activeCount={1} draftCount={0} />

      <div
        style={{
          background: COLORS.paper,
          border: `1px solid ${COLORS.rule}`,
          borderRadius: 14,
          overflow: 'hidden',
        }}
      >
        {/* Column headers */}
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 130px 100px 70px 90px',
            gap: 12,
            padding: '10px 18px',
            fontSize: 10.5,
            fontFamily: FONTS.mono,
            color: COLORS.ink3,
            textTransform: 'uppercase',
            letterSpacing: '0.1em',
            borderBottom: `1px solid ${COLORS.rule}`,
            background: 'rgba(0,0,0,0.015)',
          }}
        >
          <span>Reeks</span>
          <span>Periode</span>
          <span>Bezetting</span>
          <span style={{ textAlign: 'right' }}>Prijs</span>
          <span>Status</span>
        </div>

        {/* New series row */}
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 130px 100px 70px 90px',
            gap: 12,
            padding: '16px 18px',
            alignItems: 'center',
            transform: `scale(${scale})`,
            transformOrigin: 'left center',
            background: `rgba(208,255,20,${glowAlpha * 0.18})`,
            boxShadow: glowAlpha > 0 ? `inset 3px 0 0 0 ${COLORS.tennisLime}` : 'none',
            transition: 'none',
          }}
        >
          <div>
            <p
              style={{
                fontSize: 14,
                fontWeight: 600,
                color: COLORS.ink,
                margin: 0,
                fontFamily: FONTS.sans,
              }}
            >
              Voorjaarsreeks 2026
            </p>
            <p
              style={{
                fontSize: 11,
                color: COLORS.ink3,
                fontFamily: FONTS.mono,
                margin: '3px 0 0',
                letterSpacing: '0.04em',
              }}
            >
              24 lessen · TC De Linde
            </p>
          </div>
          <span
            style={{
              fontSize: 12,
              fontFamily: FONTS.mono,
              color: COLORS.ink2,
              letterSpacing: '0.04em',
            }}
          >
            01 mrt → 30 mei
          </span>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <span style={{ fontSize: 11, fontFamily: FONTS.mono, color: COLORS.ink3 }}>0 / 15</span>
            <div
              style={{
                height: 4,
                background: 'rgba(0,0,0,0.05)',
                borderRadius: 2,
                overflow: 'hidden',
              }}
            >
              <div style={{ width: '0%', height: '100%', background: COLORS.tennisGreen }} />
            </div>
          </div>
          <span
            style={{
              fontSize: 13.5,
              fontWeight: 700,
              color: COLORS.ink,
              textAlign: 'right',
              fontFamily: FONTS.sans,
            }}
          >
            €49
          </span>
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 5,
              fontSize: 11.5,
              fontWeight: 500,
              color: COLORS.tennisGreen,
              background: 'rgba(45,80,22,0.1)',
              padding: '3px 9px',
              borderRadius: 999,
              width: 'fit-content',
              fontFamily: FONTS.sans,
            }}
          >
            <span
              style={{
                width: 6,
                height: 6,
                background: COLORS.tennisGreen,
                borderRadius: '50%',
              }}
            />
            actief
          </span>
        </div>
      </div>
    </div>
  );
};
