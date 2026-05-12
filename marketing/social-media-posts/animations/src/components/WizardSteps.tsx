import { COLORS, FONTS } from '../brand';

// Visual recreation of the /dashboard/lessons/new wizard.
// Components are pure presentational — caller passes animated values + focus
// state, and the composition drives them via interpolate() per frame.

export type ActiveField =
  | 'name'
  | 'price'
  | 'max'
  | 'club'
  | 'start'
  | 'end'
  | 'deadline'
  | null;

interface StepIndicatorProps {
  currentStep: 1 | 2 | 3;
  // 0..1 progress of the line filling from currentStep towards currentStep+1
  // (used during step-transition cross-fades)
  lineProgress?: number;
}

const CheckIcon: React.FC<{ size?: number; color?: string }> = ({ size = 14, color = COLORS.white }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12" />
  </svg>
);

const ChevronRight: React.FC<{ size?: number; color?: string }> = ({ size = 14, color = COLORS.white }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="9 18 15 12 9 6" />
  </svg>
);

const CalendarIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="4" width="18" height="18" rx="2" />
    <line x1="16" y1="2" x2="16" y2="6" />
    <line x1="8" y1="2" x2="8" y2="6" />
    <line x1="3" y1="10" x2="21" y2="10" />
  </svg>
);

const BackArrow: React.FC<{ color: string }> = ({ color }) => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <line x1="19" y1="12" x2="5" y2="12" />
    <polyline points="12 19 5 12 12 5" />
  </svg>
);

export const StepIndicator: React.FC<StepIndicatorProps> = ({ currentStep, lineProgress = 0 }) => {
  const steps = [
    { number: 1 as const, label: 'Basisinfo' },
    { number: 2 as const, label: 'Planning' },
    { number: 3 as const, label: 'Validatie' },
  ];
  return (
    <div style={{ display: 'flex', alignItems: 'center', marginBottom: 28 }}>
      {steps.map((step, i) => {
        const isCompleted = step.number < currentStep;
        const isCurrent = step.number === currentStep;
        const bg = isCompleted || isCurrent ? COLORS.tennisGreen : '#f3f4f6';
        const fg = isCompleted || isCurrent ? COLORS.white : '#9ca3af';
        const labelColor = isCompleted || isCurrent ? '#111827' : '#9ca3af';
        return (
          <div key={step.number} style={{ display: 'flex', alignItems: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <div
                style={{
                  width: 28,
                  height: 28,
                  borderRadius: '50%',
                  background: bg,
                  display: 'grid',
                  placeItems: 'center',
                  fontSize: 12,
                  fontWeight: 700,
                  color: fg,
                  flexShrink: 0,
                  boxShadow: isCurrent ? `0 0 0 4px rgba(45,80,22,0.2)` : 'none',
                  fontFamily: FONTS.sans,
                }}
              >
                {isCompleted ? <CheckIcon size={12} /> : step.number}
              </div>
              <span
                style={{
                  fontSize: 12.5,
                  fontWeight: 500,
                  color: labelColor,
                  whiteSpace: 'nowrap',
                  fontFamily: FONTS.sans,
                }}
              >
                {step.label}
              </span>
            </div>
            {i < steps.length - 1 && (
              <div
                style={{
                  position: 'relative',
                  height: 1,
                  width: 36,
                  margin: '0 14px',
                  background: '#e5e7eb',
                  flexShrink: 0,
                }}
              >
                <div
                  style={{
                    position: 'absolute',
                    inset: 0,
                    background: COLORS.tennisGreen,
                    width: `${
                      isCompleted ? 100 : isCurrent && step.number < 3 ? lineProgress * 100 : 0
                    }%`,
                  }}
                />
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
};

const FormLabel: React.FC<{ children: React.ReactNode; required?: boolean }> = ({ children, required }) => (
  <label
    style={{
      display: 'block',
      fontSize: 12.5,
      fontWeight: 500,
      color: '#374151',
      marginBottom: 6,
      fontFamily: FONTS.sans,
    }}
  >
    {children}
    {required && <span style={{ color: COLORS.red400, marginLeft: 2 }}>*</span>}
  </label>
);

interface InputProps {
  value: string;
  placeholder?: string;
  active?: boolean;
  showCursor?: boolean;
  prefix?: string;
  rightIcon?: React.ReactNode;
  alignment?: 'left' | 'right';
}

const TextInput: React.FC<InputProps> = ({
  value,
  placeholder,
  active,
  showCursor,
  prefix,
  rightIcon,
  alignment = 'left',
}) => {
  const showPlaceholder = value === '' && !active && placeholder;
  return (
    <div
      style={{
        position: 'relative',
        height: 36,
        border: `1px solid ${active ? COLORS.tennisGreen : '#e5e7eb'}`,
        boxShadow: active ? `0 0 0 3px rgba(45,80,22,0.18)` : 'none',
        borderRadius: 8,
        background: COLORS.white,
        display: 'flex',
        alignItems: 'center',
        padding: prefix ? '0 12px 0 26px' : '0 12px',
        fontSize: 13,
        fontFamily: FONTS.sans,
        color: showPlaceholder ? '#9ca3af' : COLORS.ink,
      }}
    >
      {prefix && (
        <span
          style={{
            position: 'absolute',
            left: 12,
            top: '50%',
            transform: 'translateY(-50%)',
            color: '#9ca3af',
            fontSize: 13,
            pointerEvents: 'none',
          }}
        >
          {prefix}
        </span>
      )}
      <span style={{ flex: 1, textAlign: alignment }}>{showPlaceholder ? placeholder : value}</span>
      {showCursor && (
        <span
          style={{
            display: 'inline-block',
            width: 1.5,
            height: 16,
            background: COLORS.ink,
            marginLeft: 1,
          }}
        />
      )}
      {rightIcon && <span style={{ marginLeft: 8, display: 'flex' }}>{rightIcon}</span>}
    </div>
  );
};

interface Step1Values {
  name: string;
  price: number | null;
  maxRegistrations: number | null;
  tennisClub: string;
  startDate: string;
  endDate: string;
  deadline: string;
}

interface StepBasisinfoProps {
  values: Step1Values;
  activeField: ActiveField;
  cursorBlink: boolean;
  showClubDropdown?: boolean;
  buttonPressed?: boolean;
  buttonHover?: boolean;
}

export const StepBasisinfo: React.FC<StepBasisinfoProps> = ({
  values,
  activeField,
  cursorBlink,
  showClubDropdown,
  buttonPressed,
  buttonHover,
}) => {
  const fmt = (n: number | null) => (n === null ? '' : String(n));
  return (
    <>
      <div
        style={{
          background: COLORS.white,
          borderRadius: 12,
          boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
          padding: 20,
          display: 'flex',
          flexDirection: 'column',
          gap: 14,
        }}
      >
        <div>
          <FormLabel required>Naam</FormLabel>
          <TextInput
            value={values.name}
            placeholder="Voorjaarslessen 2026"
            active={activeField === 'name'}
            showCursor={activeField === 'name' && cursorBlink}
          />
        </div>

        <div>
          <FormLabel required>Prijs</FormLabel>
          <TextInput
            value={fmt(values.price)}
            prefix="€"
            active={activeField === 'price'}
            showCursor={activeField === 'price' && cursorBlink}
          />
        </div>

        <div>
          <FormLabel required>Max. inschrijvingen</FormLabel>
          <TextInput
            value={fmt(values.maxRegistrations)}
            active={activeField === 'max'}
            showCursor={activeField === 'max' && cursorBlink}
          />
        </div>

        <div style={{ position: 'relative' }}>
          <FormLabel required>Tennisclub</FormLabel>
          <TextInput
            value={values.tennisClub}
            placeholder="Kies een tennisclub"
            active={activeField === 'club'}
            rightIcon={
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="#9ca3af" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="6 9 12 15 18 9" />
              </svg>
            }
          />
          {showClubDropdown && (
            <div
              style={{
                position: 'absolute',
                top: '100%',
                left: 0,
                right: 0,
                marginTop: 4,
                background: COLORS.white,
                border: `1px solid ${COLORS.rule}`,
                borderRadius: 8,
                boxShadow: '0 8px 20px rgba(0,0,0,0.12)',
                padding: 4,
                zIndex: 10,
              }}
            >
              {['TC De Linde', 'Padel Club Antwerpen', 'KTC De Veldjes'].map((c, i) => (
                <div
                  key={c}
                  style={{
                    padding: '6px 10px',
                    fontSize: 12.5,
                    color: COLORS.ink,
                    borderRadius: 4,
                    background: i === 0 ? 'rgba(45,80,22,0.08)' : 'transparent',
                    fontFamily: FONTS.sans,
                  }}
                >
                  {c}
                </div>
              ))}
            </div>
          )}
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div>
            <FormLabel required>Startdatum</FormLabel>
            <TextInput
              value={values.startDate}
              placeholder="dd-mm-jjjj"
              active={activeField === 'start'}
              rightIcon={<CalendarIcon color="#9ca3af" />}
            />
          </div>
          <div>
            <FormLabel required>Einddatum</FormLabel>
            <TextInput
              value={values.endDate}
              placeholder="dd-mm-jjjj"
              active={activeField === 'end'}
              rightIcon={<CalendarIcon color="#9ca3af" />}
            />
          </div>
        </div>

        <div>
          <FormLabel required>Inschrijfdeadline</FormLabel>
          <TextInput
            value={values.deadline}
            placeholder="dd-mm-jjjj"
            active={activeField === 'deadline'}
            rightIcon={<CalendarIcon color="#9ca3af" />}
          />
        </div>
      </div>

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 18 }}>
        <button
          type="button"
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            padding: '9px 18px',
            background: buttonHover ? '#264411' : COLORS.tennisGreen,
            color: COLORS.white,
            fontSize: 13,
            fontWeight: 600,
            borderRadius: 8,
            border: 'none',
            cursor: 'pointer',
            transform: buttonPressed ? 'scale(0.96)' : 'scale(1)',
            transition: 'none',
            fontFamily: FONTS.sans,
          }}
        >
          Volgende
          <ChevronRight size={13} color={COLORS.white} />
        </button>
      </div>
    </>
  );
};

interface StepPlanningProps {
  slotsRevealed: number; // 0..14
  buttonPressed?: boolean;
}

export const StepPlanning: React.FC<StepPlanningProps> = ({ slotsRevealed, buttonPressed }) => {
  const days = ['Ma', 'Di', 'Wo', 'Do', 'Vr', 'Za', 'Zo'];
  const slots = [
    { day: 0, time: '17:00', label: 'Beginners' },
    { day: 0, time: '18:30', label: 'Gevorderd' },
    { day: 2, time: '16:00', label: 'Jeugd' },
    { day: 2, time: '17:30', label: 'Beginners' },
    { day: 2, time: '19:00', label: 'Gevorderd' },
    { day: 4, time: '17:00', label: 'Jeugd' },
    { day: 4, time: '18:30', label: 'Beginners' },
    { day: 4, time: '20:00', label: 'Gevorderd' },
    { day: 5, time: '10:00', label: 'Jeugd' },
    { day: 5, time: '11:30', label: 'Beginners' },
    { day: 5, time: '14:00', label: 'Gevorderd' },
  ];
  return (
    <>
      <div
        style={{
          background: COLORS.white,
          borderRadius: 12,
          boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
          padding: 18,
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 14 }}>
          <div>
            <p style={{ fontSize: 13, fontWeight: 600, color: COLORS.ink, margin: 0 }}>Standaard weekschema</p>
            <p style={{ fontSize: 11, color: COLORS.ink3, margin: '2px 0 0 0' }}>
              Voeg lesmomenten toe — herhaalt voor elke week.
            </p>
          </div>
          <span
            style={{
              fontSize: 10.5,
              fontFamily: FONTS.mono,
              color: COLORS.ink3,
              letterSpacing: '0.08em',
              textTransform: 'uppercase',
            }}
          >
            week 1 / 12
          </span>
        </div>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(7, 1fr)',
            gap: 6,
            border: `1px solid ${COLORS.rule}`,
            borderRadius: 8,
            padding: 8,
            background: COLORS.canvas,
          }}
        >
          {days.map((d, di) => (
            <div key={d} style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <div
                style={{
                  fontSize: 10.5,
                  fontWeight: 700,
                  color: COLORS.ink3,
                  textTransform: 'uppercase',
                  letterSpacing: '0.08em',
                  textAlign: 'center',
                  padding: '2px 0 4px',
                  borderBottom: `1px solid ${COLORS.rule}`,
                }}
              >
                {d}
              </div>
              {slots
                .filter((s) => s.day === di)
                .map((s, si) => {
                  const slotIndex = slots.findIndex((x) => x === s);
                  const visible = slotIndex < slotsRevealed;
                  return (
                    <div
                      key={si}
                      style={{
                        background: visible ? 'rgba(45,80,22,0.08)' : 'rgba(0,0,0,0.03)',
                        border: visible ? `1px solid ${COLORS.tennisGreen}` : `1px dashed ${COLORS.rule}`,
                        borderRadius: 5,
                        padding: '4px 5px',
                        fontSize: 9.5,
                        color: visible ? COLORS.tennisGreen : 'transparent',
                        fontWeight: 600,
                        opacity: visible ? 1 : 0.5,
                        transform: visible ? 'scale(1)' : 'scale(0.95)',
                        minHeight: 26,
                      }}
                    >
                      {visible && (
                        <>
                          <div>{s.time}</div>
                          <div style={{ fontSize: 8.5, fontWeight: 500, color: COLORS.ink2 }}>{s.label}</div>
                        </>
                      )}
                    </div>
                  );
                })}
            </div>
          ))}
        </div>
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 18 }}>
        <button
          type="button"
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            padding: '9px 14px',
            background: COLORS.white,
            color: COLORS.ink2,
            fontSize: 13,
            fontWeight: 500,
            borderRadius: 8,
            border: `1px solid ${COLORS.rule}`,
            fontFamily: FONTS.sans,
          }}
        >
          <BackArrow color={COLORS.ink2} />
          Terug
        </button>
        <button
          type="button"
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            padding: '9px 18px',
            background: COLORS.tennisGreen,
            color: COLORS.white,
            fontSize: 13,
            fontWeight: 600,
            borderRadius: 8,
            border: 'none',
            transform: buttonPressed ? 'scale(0.96)' : 'scale(1)',
            fontFamily: FONTS.sans,
          }}
        >
          Volgende
          <ChevronRight size={13} color={COLORS.white} />
        </button>
      </div>
    </>
  );
};

interface StepValidatieProps {
  buttonPressed?: boolean;
}

export const StepValidatie: React.FC<StepValidatieProps> = ({ buttonPressed }) => {
  const weeks = [
    { week: 1, range: '1 — 7 mrt', lessons: 2 },
    { week: 2, range: '8 — 14 mrt', lessons: 2 },
    { week: 3, range: '15 — 21 mrt', lessons: 2 },
    { week: 4, range: '22 — 28 mrt', lessons: 2 },
    { week: 5, range: '29 mrt — 4 apr', lessons: 2 },
  ];
  return (
    <>
      <div
        style={{
          background: COLORS.white,
          borderRadius: 12,
          boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
          padding: 20,
        }}
      >
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            padding: '6px 12px',
            background: 'rgba(45,80,22,0.06)',
            border: `1px solid rgba(45,80,22,0.18)`,
            borderRadius: 8,
            marginBottom: 14,
          }}
        >
          <span style={{ fontSize: 12.5, fontWeight: 600, color: COLORS.tennisGreen, fontFamily: FONTS.sans }}>
            Voorjaarsreeks 2026
          </span>
          <span
            style={{
              fontSize: 11.5,
              fontFamily: FONTS.mono,
              color: COLORS.tennisGreen,
              letterSpacing: '0.04em',
            }}
          >
            12 weken · 24 lessen
          </span>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {weeks.map((w) => (
            <div
              key={w.week}
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                padding: '8px 12px',
                background: COLORS.canvas,
                border: `1px solid ${COLORS.rule}`,
                borderRadius: 6,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <span
                  style={{
                    fontFamily: FONTS.mono,
                    fontSize: 11,
                    color: COLORS.ink3,
                    letterSpacing: '0.06em',
                  }}
                >
                  W{String(w.week).padStart(2, '0')}
                </span>
                <span style={{ fontSize: 12, color: COLORS.ink2, fontFamily: FONTS.sans }}>{w.range}</span>
              </div>
              <span style={{ fontSize: 11, fontFamily: FONTS.mono, color: COLORS.ink3 }}>
                {w.lessons} lessen
              </span>
            </div>
          ))}
          <div
            style={{
              padding: '6px 12px',
              fontSize: 10.5,
              color: COLORS.ink3,
              fontFamily: FONTS.mono,
              letterSpacing: '0.06em',
              textTransform: 'uppercase',
            }}
          >
            + 7 weken meer
          </div>
        </div>
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 18 }}>
        <button
          type="button"
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 6,
            padding: '9px 14px',
            background: COLORS.white,
            color: COLORS.ink2,
            fontSize: 13,
            fontWeight: 500,
            borderRadius: 8,
            border: `1px solid ${COLORS.rule}`,
            fontFamily: FONTS.sans,
          }}
        >
          <BackArrow color={COLORS.ink2} />
          Terug
        </button>
        <button
          type="button"
          style={{
            padding: '9px 18px',
            background: COLORS.tennisGreen,
            color: COLORS.white,
            fontSize: 13,
            fontWeight: 600,
            borderRadius: 8,
            border: 'none',
            transform: buttonPressed ? 'scale(0.96)' : 'scale(1)',
            fontFamily: FONTS.sans,
          }}
        >
          Lesreeks aanmaken
        </button>
      </div>
    </>
  );
};

// Shared header rendered above the wizard steps.
export const WizardHeader: React.FC = () => (
  <>
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        fontSize: 12,
        color: COLORS.ink3,
        marginBottom: 10,
        fontFamily: FONTS.sans,
      }}
    >
      <BackArrow color={COLORS.ink3} />
      <span>Terug naar lessen</span>
    </div>
    <h1
      style={{
        fontSize: 26,
        fontWeight: 700,
        color: COLORS.ink,
        margin: 0,
        letterSpacing: -0.5,
        fontFamily: FONTS.sans,
      }}
    >
      Nieuwe lesreeks
    </h1>
    <p
      style={{
        fontSize: 13,
        color: COLORS.ink3,
        margin: '4px 0 22px',
        fontFamily: FONTS.sans,
      }}
    >
      Stel een nieuwe lesreeks in via de wizard.
    </p>
  </>
);
