import { COLORS, FONTS } from '../brand';

// Inner phone screens for the student-enrollment animation.
// Each screen is sized to fit within the PhoneFrame's screen area
// (360w, ~732h after the status bar).

const WHATSAPP_GREEN = '#075E54';
const WHATSAPP_TEAL = '#25D366';
const WHATSAPP_BG = '#ECE5DD';

const BackIcon: React.FC<{ color: string; size?: number }> = ({ color, size = 18 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="15 18 9 12 15 6" />
  </svg>
);

const LockIcon: React.FC<{ color: string; size?: number }> = ({ color, size = 11 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill={color}>
    <path d="M12 2 a5 5 0 0 0 -5 5 v3 H6 a1 1 0 0 0 -1 1 v10 a1 1 0 0 0 1 1 h12 a1 1 0 0 0 1 -1 V11 a1 1 0 0 0 -1 -1 h-1 V7 a5 5 0 0 0 -5 -5 zm-3 8 V7 a3 3 0 0 1 6 0 v3 z" />
  </svg>
);

const CheckIcon: React.FC<{ color: string; size?: number }> = ({ color, size = 14 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12" />
  </svg>
);

const CalendarIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="4" width="18" height="18" rx="2" />
    <line x1="16" y1="2" x2="16" y2="6" />
    <line x1="8" y1="2" x2="8" y2="6" />
    <line x1="3" y1="10" x2="21" y2="10" />
  </svg>
);

const MapPinIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z" />
    <circle cx="12" cy="10" r="3" />
  </svg>
);

const EuroIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M4 10h12M4 14h12M19 6.2A7.5 7.5 0 0 0 14 4a8 8 0 0 0 0 16 7.5 7.5 0 0 0 5-2.2" />
  </svg>
);

const UserIcon: React.FC<{ color: string; size?: number }> = ({ color, size = 14 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
    <circle cx="12" cy="7" r="4" />
  </svg>
);

const TennisBallMini: React.FC<{ size?: number }> = ({ size = 24 }) => (
  <svg width={size} height={size} viewBox="0 0 60 60">
    <defs>
      <radialGradient id="ball-mini" cx="35%" cy="35%" r="65%">
        <stop offset="0%" stopColor="#E5FF60" />
        <stop offset="55%" stopColor="#D0FF14" />
        <stop offset="100%" stopColor="#A8CC0E" />
      </radialGradient>
    </defs>
    <circle cx="30" cy="30" r="24" fill="url(#ball-mini)" />
    <path d="M 8 30 C 16 22, 24 22, 30 30 C 36 38, 44 38, 52 30" stroke="#ffffff" strokeWidth="2" fill="none" strokeLinecap="round" opacity="0.9" />
  </svg>
);

// ────────────────────────────────────────────────────────────────────────
//  WhatsApp screen
// ────────────────────────────────────────────────────────────────────────

interface WhatsAppScreenProps {
  tapHighlight?: boolean; // briefly highlight the link card when tapped
}

export const WhatsAppScreen: React.FC<WhatsAppScreenProps> = ({ tapHighlight }) => (
  <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: WHATSAPP_BG }}>
    {/* WhatsApp header */}
    <div
      style={{
        background: WHATSAPP_GREEN,
        color: COLORS.white,
        padding: '8px 14px',
        display: 'flex',
        alignItems: 'center',
        gap: 10,
        flexShrink: 0,
      }}
    >
      <BackIcon color={COLORS.white} size={16} />
      <div
        style={{
          width: 36,
          height: 36,
          borderRadius: '50%',
          background: 'rgba(255,255,255,0.18)',
          display: 'grid',
          placeItems: 'center',
        }}
      >
        <span style={{ fontWeight: 700, fontSize: 13 }}>T</span>
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <p style={{ margin: 0, fontSize: 14, fontWeight: 600, lineHeight: 1.1 }}>Trainer Tim</p>
        <p style={{ margin: '2px 0 0', fontSize: 10.5, color: 'rgba(255,255,255,0.75)' }}>online</p>
      </div>
    </div>

    {/* Chat area */}
    <div
      style={{
        flex: 1,
        padding: '14px 12px',
        display: 'flex',
        flexDirection: 'column',
        gap: 6,
      }}
    >
      {/* Text bubble */}
      <div
        style={{
          alignSelf: 'flex-start',
          background: COLORS.white,
          borderRadius: '8px 12px 12px 12px',
          padding: '8px 12px',
          maxWidth: '80%',
          boxShadow: '0 1px 0.5px rgba(0,0,0,0.13)',
        }}
      >
        <p style={{ margin: 0, fontSize: 13, color: '#262626', lineHeight: 1.35 }}>
          Hey! Hier de inschrijflink voor de voorjaarsreeks 🎾
        </p>
        <p
          style={{
            margin: '4px 0 0',
            fontSize: 10,
            color: '#999',
            textAlign: 'right',
          }}
        >
          14:32
        </p>
      </div>

      {/* Link preview card */}
      <div
        style={{
          alignSelf: 'flex-start',
          background: COLORS.white,
          borderRadius: 12,
          overflow: 'hidden',
          maxWidth: '88%',
          boxShadow: tapHighlight
            ? '0 0 0 3px rgba(45,80,22,0.4)'
            : '0 1px 0.5px rgba(0,0,0,0.13)',
          transform: tapHighlight ? 'scale(0.97)' : 'scale(1)',
        }}
      >
        {/* Image area */}
        <div
          style={{
            height: 96,
            background: `linear-gradient(135deg, ${COLORS.tennisGreen} 0%, #3d6620 60%, #2a4815 100%)`,
            display: 'grid',
            placeItems: 'center',
            position: 'relative',
            overflow: 'hidden',
          }}
        >
          <TennisBallMini size={42} />
          <svg
            style={{ position: 'absolute', inset: 0, opacity: 0.12 }}
            viewBox="0 0 240 96"
            preserveAspectRatio="none"
          >
            <g stroke={COLORS.white} strokeWidth="1" fill="none">
              <rect x="20" y="14" width="200" height="68" />
              <line x1="120" y1="14" x2="120" y2="82" />
              <line x1="40" y1="48" x2="200" y2="48" />
            </g>
          </svg>
        </div>
        {/* Card body */}
        <div style={{ padding: '8px 12px' }}>
          <p
            style={{
              margin: 0,
              fontSize: 12,
              fontWeight: 600,
              color: COLORS.ink,
              lineHeight: 1.25,
            }}
          >
            Voorjaarsreeks 2026 — Inschrijven
          </p>
          <p
            style={{
              margin: '2px 0 0',
              fontSize: 11,
              color: '#888',
              fontFamily: FONTS.sans,
            }}
          >
            coach-os.be
          </p>
        </div>
        <p
          style={{
            margin: '0 12px 6px',
            fontSize: 10,
            color: '#999',
            textAlign: 'right',
          }}
        >
          14:32 ✓✓
        </p>
      </div>
    </div>

    {/* Input bar */}
    <div
      style={{
        background: COLORS.white,
        padding: '8px 10px',
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        flexShrink: 0,
      }}
    >
      <div
        style={{
          flex: 1,
          height: 32,
          background: '#f3f0eb',
          borderRadius: 18,
          padding: '0 12px',
          display: 'flex',
          alignItems: 'center',
          fontSize: 11.5,
          color: '#999',
        }}
      >
        Bericht
      </div>
      <div
        style={{
          width: 32,
          height: 32,
          borderRadius: '50%',
          background: WHATSAPP_TEAL,
          display: 'grid',
          placeItems: 'center',
          color: COLORS.white,
        }}
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill={COLORS.white}>
          <path d="M3 12 l18 -9 -7 18 -3 -7 -8 -2z" />
        </svg>
      </div>
    </div>
  </div>
);

// ────────────────────────────────────────────────────────────────────────
//  Enrollment page (mobile browser)
// ────────────────────────────────────────────────────────────────────────

const BrowserBar: React.FC = () => (
  <div
    style={{
      background: '#f3f3f3',
      padding: '6px 10px',
      display: 'flex',
      alignItems: 'center',
      gap: 6,
      flexShrink: 0,
      borderBottom: '1px solid rgba(0,0,0,0.06)',
    }}
  >
    <LockIcon color="#666" size={11} />
    <span
      style={{
        fontSize: 11,
        color: '#444',
        fontFamily: FONTS.sans,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        flex: 1,
      }}
    >
      coach-os.be/enroll/voorjaarsreeks-2026
    </span>
  </div>
);

export interface EnrollmentValues {
  firstName: string;
  lastName: string;
  email: string;
  // For each day slot: 'preferred' | 'available' | 'unavailable' | null
  availability: Record<string, 'preferred' | 'available' | 'unavailable' | null>;
}

export type ActiveField = 'first' | 'last' | 'email' | null;

interface EnrollmentScreenProps {
  values: EnrollmentValues;
  activeField: ActiveField;
  cursorBlink: boolean;
  submitButtonHover?: boolean;
  submitButtonPressed?: boolean;
  submitting?: boolean;
}

const SLOTS = [
  { key: 'ma', day: 'Maandag', time: '18:30', court: 'Baan 1' },
  { key: 'wo', day: 'Woensdag', time: '19:00', court: 'Baan 2' },
  { key: 'vr', day: 'Vrijdag', time: '17:30', court: 'Baan 1' },
];

const InputBox: React.FC<{
  value: string;
  placeholder: string;
  active: boolean;
  showCursor: boolean;
}> = ({ value, placeholder, active, showCursor }) => {
  const isEmpty = value === '';
  return (
    <div
      style={{
        position: 'relative',
        height: 36,
        border: `1px solid ${active ? COLORS.tennisGreen : '#d1d5db'}`,
        boxShadow: active ? `0 0 0 3px rgba(45,80,22,0.18)` : 'none',
        borderRadius: 8,
        background: COLORS.white,
        display: 'flex',
        alignItems: 'center',
        padding: '0 10px',
        fontSize: 12.5,
        fontFamily: FONTS.sans,
        color: isEmpty && !active ? '#9ca3af' : COLORS.ink,
      }}
    >
      <span>{isEmpty && !active ? placeholder : value}</span>
      {showCursor && (
        <span
          style={{
            display: 'inline-block',
            width: 1.5,
            height: 14,
            background: COLORS.ink,
            marginLeft: 1,
          }}
        />
      )}
    </div>
  );
};

const FieldLabel: React.FC<{ children: React.ReactNode; required?: boolean }> = ({ children, required }) => (
  <label
    style={{
      display: 'block',
      fontSize: 11,
      fontWeight: 500,
      color: '#4a4741',
      marginBottom: 4,
      fontFamily: FONTS.sans,
    }}
  >
    {children}
    {required && <span style={{ color: '#f87171', marginLeft: 2 }}>*</span>}
  </label>
);

const PrefBubble: React.FC<{
  state: 'preferred' | 'available' | 'unavailable' | null;
  type: 'preferred' | 'available' | 'unavailable';
}> = ({ state, type }) => {
  const selected = state === type;
  const colors = {
    preferred: { bg: COLORS.tennisGreen, border: COLORS.tennisGreen },
    available: { bg: '#3b82f6', border: '#3b82f6' },
    unavailable: { bg: '#9ca3af', border: '#9ca3af' },
  };
  const c = colors[type];
  return (
    <div
      style={{
        width: 22,
        height: 22,
        borderRadius: '50%',
        border: `2px solid ${selected ? c.border : '#e5e7eb'}`,
        background: selected ? c.bg : COLORS.white,
        display: 'grid',
        placeItems: 'center',
      }}
    >
      {selected && type !== 'unavailable' && <CheckIcon color={COLORS.white} size={10} />}
      {selected && type === 'unavailable' && (
        <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke={COLORS.white} strokeWidth="3" strokeLinecap="round">
          <line x1="6" y1="6" x2="18" y2="18" />
          <line x1="18" y1="6" x2="6" y2="18" />
        </svg>
      )}
    </div>
  );
};

export const EnrollmentScreen: React.FC<EnrollmentScreenProps> = ({
  values,
  activeField,
  cursorBlink,
  submitButtonHover,
  submitButtonPressed,
  submitting,
}) => (
  <div style={{ flex: 1, display: 'flex', flexDirection: 'column', background: '#FAFAF8', overflow: 'hidden' }}>
    <BrowserBar />
    <div style={{ flex: 1, padding: '12px 14px', display: 'flex', flexDirection: 'column', gap: 12, overflow: 'hidden' }}>
      {/* Brand row */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <div
          style={{
            width: 24,
            height: 24,
            borderRadius: '50%',
            background: COLORS.tennisGreen,
            display: 'grid',
            placeItems: 'center',
          }}
        >
          <TennisBallMini size={14} />
        </div>
        <span style={{ fontSize: 14, fontWeight: 600, color: COLORS.tennisGreen, fontFamily: FONTS.sans }}>
          CoachOS
        </span>
      </div>

      {/* Series info card */}
      <div
        style={{
          background: COLORS.white,
          border: `1px solid ${COLORS.rule}`,
          borderRadius: 10,
          padding: 12,
        }}
      >
        <span
          style={{
            display: 'inline-block',
            background: 'rgba(208,255,20,0.25)',
            color: COLORS.tennisGreen,
            padding: '2px 8px',
            borderRadius: 999,
            fontSize: 9.5,
            fontWeight: 700,
            textTransform: 'uppercase',
            letterSpacing: '0.06em',
            marginBottom: 6,
          }}
        >
          Alle niveaus
        </span>
        <p style={{ margin: 0, fontSize: 15, fontWeight: 700, color: COLORS.ink, fontFamily: FONTS.sans }}>
          Voorjaarsreeks 2026
        </p>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '4px 12px', marginTop: 6 }}>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 10.5, color: COLORS.ink2 }}>
            <CalendarIcon color={COLORS.ink3} />
            01 mrt → 30 mei
          </span>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 10.5, color: COLORS.ink2 }}>
            <MapPinIcon color={COLORS.ink3} />
            TC De Linde
          </span>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: 10.5, color: COLORS.ink2 }}>
            <EuroIcon color={COLORS.ink3} />
            49 / reeks
          </span>
        </div>
      </div>

      {/* Form card */}
      <div
        style={{
          background: COLORS.white,
          border: `1px solid ${COLORS.rule}`,
          borderRadius: 10,
          padding: 12,
          display: 'flex',
          flexDirection: 'column',
          gap: 10,
        }}
      >
        <p style={{ margin: 0, fontSize: 13, fontWeight: 600, color: COLORS.ink, fontFamily: FONTS.sans }}>
          Inschrijving
        </p>

        {/* Two-col name */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
          <div>
            <FieldLabel required>Voornaam</FieldLabel>
            <InputBox
              value={values.firstName}
              placeholder="Sven"
              active={activeField === 'first'}
              showCursor={activeField === 'first' && cursorBlink}
            />
          </div>
          <div>
            <FieldLabel required>Achternaam</FieldLabel>
            <InputBox
              value={values.lastName}
              placeholder="Janssens"
              active={activeField === 'last'}
              showCursor={activeField === 'last' && cursorBlink}
            />
          </div>
        </div>

        {/* Email */}
        <div>
          <FieldLabel required>E-mailadres</FieldLabel>
          <InputBox
            value={values.email}
            placeholder="sven@gmail.com"
            active={activeField === 'email'}
            showCursor={activeField === 'email' && cursorBlink}
          />
        </div>

        {/* Availability */}
        <div>
          <FieldLabel required>Beschikbaarheid</FieldLabel>
          <div
            style={{
              border: `1px solid ${COLORS.rule}`,
              borderRadius: 8,
              overflow: 'hidden',
            }}
          >
            {/* Header */}
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: '1fr 30px 30px 30px',
                gap: 4,
                padding: '4px 8px',
                background: '#f9fafb',
                fontSize: 8.5,
                fontWeight: 600,
                color: COLORS.ink3,
                textTransform: 'uppercase',
                letterSpacing: '0.05em',
                borderBottom: `1px solid ${COLORS.rule}`,
              }}
            >
              <span>Tijdslot</span>
              <span style={{ textAlign: 'center', color: COLORS.tennisGreen }}>V</span>
              <span style={{ textAlign: 'center', color: '#3b82f6' }}>B</span>
              <span style={{ textAlign: 'center', color: '#9ca3af' }}>N</span>
            </div>
            {SLOTS.map((s, i) => (
              <div
                key={s.key}
                style={{
                  display: 'grid',
                  gridTemplateColumns: '1fr 30px 30px 30px',
                  gap: 4,
                  padding: '6px 8px',
                  alignItems: 'center',
                  borderBottom: i < SLOTS.length - 1 ? `1px solid ${COLORS.rule}` : 'none',
                }}
              >
                <div>
                  <p style={{ margin: 0, fontSize: 10.5, fontWeight: 600, color: COLORS.ink }}>{s.day}</p>
                  <p style={{ margin: '1px 0 0', fontSize: 9, color: COLORS.ink3, fontFamily: FONTS.mono }}>
                    {s.time} · {s.court}
                  </p>
                </div>
                <div style={{ display: 'grid', placeItems: 'center' }}>
                  <PrefBubble state={values.availability[s.key]} type="preferred" />
                </div>
                <div style={{ display: 'grid', placeItems: 'center' }}>
                  <PrefBubble state={values.availability[s.key]} type="available" />
                </div>
                <div style={{ display: 'grid', placeItems: 'center' }}>
                  <PrefBubble state={values.availability[s.key]} type="unavailable" />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Submit */}
        <button
          type="button"
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 8,
            width: '100%',
            padding: '10px 0',
            background: submitButtonHover ? '#264411' : COLORS.tennisGreen,
            color: COLORS.white,
            fontSize: 13,
            fontWeight: 600,
            border: 'none',
            borderRadius: 8,
            transform: submitButtonPressed ? 'scale(0.97)' : 'scale(1)',
            fontFamily: FONTS.sans,
          }}
        >
          {submitting ? (
            <>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="9" stroke={COLORS.white} strokeOpacity="0.3" strokeWidth="2.5" />
                <path d="M21 12 a9 9 0 0 0 -9 -9" stroke={COLORS.white} strokeWidth="2.5" strokeLinecap="round" />
              </svg>
              Inschrijven…
            </>
          ) : (
            'Inschrijven'
          )}
        </button>

        <p style={{ margin: 0, fontSize: 9.5, color: COLORS.ink3, textAlign: 'center', fontFamily: FONTS.sans }}>
          Je ontvangt een bevestiging per e-mail
        </p>
      </div>
    </div>
  </div>
);

// ────────────────────────────────────────────────────────────────────────
//  Success screen
// ────────────────────────────────────────────────────────────────────────

interface SuccessScreenProps {
  // 0..1 — for entry scale-in
  progress: number;
}

export const SuccessScreen: React.FC<SuccessScreenProps> = ({ progress }) => {
  const checkScale = 0.6 + progress * 0.4;
  return (
    <div
      style={{
        flex: 1,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        background: '#FAFAF8',
        padding: '24px',
        textAlign: 'center',
        gap: 20,
      }}
    >
      <div
        style={{
          width: 88,
          height: 88,
          borderRadius: '50%',
          background: 'rgba(45,80,22,0.1)',
          display: 'grid',
          placeItems: 'center',
          transform: `scale(${checkScale})`,
        }}
      >
        <div
          style={{
            width: 60,
            height: 60,
            borderRadius: '50%',
            background: COLORS.tennisGreen,
            display: 'grid',
            placeItems: 'center',
          }}
        >
          <CheckIcon color={COLORS.white} size={32} />
        </div>
      </div>
      <div>
        <p style={{ margin: 0, fontSize: 20, fontWeight: 700, color: COLORS.ink, fontFamily: FONTS.sans }}>
          Ingeschreven!
        </p>
        <p
          style={{
            margin: '8px 0 0',
            fontSize: 13,
            color: COLORS.ink2,
            fontFamily: FONTS.sans,
            lineHeight: 1.4,
            maxWidth: 280,
          }}
        >
          We sturen je een mail zodra de planning klaar is.
        </p>
      </div>
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 6,
          padding: '6px 12px',
          background: 'rgba(208,255,20,0.18)',
          border: `1px solid rgba(45,80,22,0.18)`,
          borderRadius: 999,
          color: COLORS.tennisGreen,
          fontSize: 11.5,
          fontWeight: 600,
          fontFamily: FONTS.sans,
        }}
      >
        <UserIcon color={COLORS.tennisGreen} size={12} />
        Geen account aangemaakt
      </div>
    </div>
  );
};
