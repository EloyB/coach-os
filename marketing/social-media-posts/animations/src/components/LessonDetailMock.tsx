import { COLORS, FONTS } from '../brand';

// Visual recreation of the /dashboard/lessons/[id] detail page,
// focused on the FormBuilder + Enrollments-header sections.
// Composition drives animated state via props.

const ClipboardIcon: React.FC<{ size?: number; color: string }> = ({ size = 13, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2" />
    <rect x="9" y="3" width="6" height="4" rx="1" />
  </svg>
);

const PlusIcon: React.FC<{ size?: number; color: string }> = ({ size = 12, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="12" y1="5" x2="12" y2="19" />
    <line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

const TrashIcon: React.FC<{ size?: number; color: string }> = ({ size = 13, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
    <path d="M10 11v6M14 11v6" />
  </svg>
);

const ChevronUpIcon: React.FC<{ size?: number; color: string }> = ({ size = 13, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="18 15 12 9 6 15" />
  </svg>
);

const ChevronDownIcon: React.FC<{ size?: number; color: string }> = ({ size = 13, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="6 9 12 15 18 9" />
  </svg>
);

const CopyIcon: React.FC<{ size?: number; color: string }> = ({ size = 12, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="9" y="9" width="13" height="13" rx="2" />
    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
  </svg>
);

const CheckCircleIcon: React.FC<{ size?: number; color: string }> = ({ size = 12, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
    <polyline points="22 4 12 14.01 9 11.01" />
  </svg>
);

const CheckIcon: React.FC<{ size?: number; color: string }> = ({ size = 12, color }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="20 6 9 17 4 12" />
  </svg>
);

const BackArrowIcon: React.FC<{ color: string }> = ({ color }) => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <line x1="19" y1="12" x2="5" y2="12" />
    <polyline points="12 19 5 12 12 5" />
  </svg>
);

// ────────────────────────────────────────────────────────────────────────
//  Series header (compact)
// ────────────────────────────────────────────────────────────────────────

export const SeriesHeader: React.FC = () => (
  <>
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        fontSize: 12,
        color: COLORS.ink3,
        marginBottom: 8,
        fontFamily: FONTS.sans,
      }}
    >
      <BackArrowIcon color={COLORS.ink3} />
      <span>Lessen</span>
    </div>
    <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 6 }}>
      <h1
        style={{
          fontSize: 22,
          fontWeight: 700,
          color: COLORS.ink,
          margin: 0,
          letterSpacing: -0.4,
          fontFamily: FONTS.sans,
        }}
      >
        Voorjaarsreeks 2026
      </h1>
      <span
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 5,
          fontSize: 11,
          fontWeight: 500,
          color: COLORS.tennisGreen,
          background: 'rgba(45,80,22,0.1)',
          padding: '3px 9px',
          borderRadius: 999,
          fontFamily: FONTS.sans,
        }}
      >
        <span style={{ width: 6, height: 6, background: COLORS.tennisGreen, borderRadius: '50%' }} />
        actief
      </span>
    </div>
    <p
      style={{
        fontSize: 12,
        color: COLORS.ink3,
        margin: '0 0 18px',
        fontFamily: FONTS.mono,
        letterSpacing: '0.03em',
      }}
    >
      01 mrt → 30 mei · TC De Linde · €49
    </p>
  </>
);

// ────────────────────────────────────────────────────────────────────────
//  Form builder section
// ────────────────────────────────────────────────────────────────────────

export type FieldType = 1 | 2 | 3; // 1=Vrije tekst, 2=Meerkeuze, 3=Ja/Nee

const TYPE_LABELS: Record<FieldType, string> = {
  1: 'Vrije tekst',
  2: 'Meerkeuze',
  3: 'Ja/Nee',
};

export interface CustomFieldProps {
  label: string;
  cursorOnLabel?: boolean;
  cursorBlink?: boolean;
  type: FieldType;
  required: boolean;
  typeDropdownOpen?: boolean;
  highlightedDropdownOption?: FieldType;
}

const PREDEFINED = ['Voornaam', 'Achternaam', 'E-mailadres', 'Inschrijvingstype', 'Beschikbaarheid'];

const Badge: React.FC<{ children: React.ReactNode; entered?: boolean }> = ({ children, entered = true }) => (
  <span
    style={{
      display: 'inline-flex',
      alignItems: 'center',
      padding: '4px 10px',
      borderRadius: 999,
      fontSize: 11.5,
      background: 'rgba(45,80,22,0.1)',
      color: COLORS.tennisGreen,
      fontWeight: 500,
      fontFamily: FONTS.sans,
      opacity: entered ? 1 : 0,
      transform: entered ? 'scale(1)' : 'scale(0.9)',
    }}
  >
    {children}
  </span>
);

const FieldRow: React.FC<{
  field: CustomFieldProps;
  isFirst?: boolean;
  isLast?: boolean;
}> = ({ field, isFirst, isLast }) => {
  const labelEmpty = field.label === '';
  return (
    <div
      style={{
        border: `1px solid ${COLORS.rule}`,
        borderRadius: 12,
        padding: 12,
        background: '#FAFAF8',
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
        position: 'relative',
        zIndex: field.typeDropdownOpen ? 40 : 'auto',
      }}
    >
      {/* Row 1: label input */}
      <div
        style={{
          position: 'relative',
          height: 32,
          border: `1px solid ${field.cursorOnLabel ? COLORS.tennisGreen : '#e5e7eb'}`,
          boxShadow: field.cursorOnLabel ? `0 0 0 3px rgba(45,80,22,0.18)` : 'none',
          borderRadius: 8,
          background: COLORS.white,
          display: 'flex',
          alignItems: 'center',
          padding: '0 10px',
          fontSize: 12.5,
          color: labelEmpty ? '#9ca3af' : COLORS.ink,
          fontFamily: FONTS.sans,
        }}
      >
        <span>{labelEmpty ? "Veldlabel, bijv. 'Telefoonnummer'" : field.label}</span>
        {field.cursorOnLabel && field.cursorBlink && (
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

      {/* Row 2: type dropdown + required + reorder/delete */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, position: 'relative' }}>
        {/* Type dropdown */}
        <div
          style={{
            position: 'relative',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 6,
            height: 28,
            padding: '0 8px',
            border: `1px solid ${field.typeDropdownOpen ? COLORS.tennisGreen : '#e5e7eb'}`,
            boxShadow: field.typeDropdownOpen ? `0 0 0 3px rgba(45,80,22,0.18)` : 'none',
            background: COLORS.white,
            borderRadius: 6,
            fontSize: 11.5,
            color: COLORS.ink,
            fontFamily: FONTS.sans,
            minWidth: 110,
          }}
        >
          <span>{TYPE_LABELS[field.type]}</span>
          <ChevronDownIcon size={10} color={COLORS.ink3} />
          {field.typeDropdownOpen && (
            <div
              style={{
                position: 'absolute',
                top: 'calc(100% + 4px)',
                left: 0,
                right: 0,
                background: COLORS.white,
                border: `1px solid ${COLORS.rule}`,
                borderRadius: 6,
                boxShadow: '0 8px 20px rgba(0,0,0,0.12)',
                padding: 4,
                zIndex: 30,
              }}
            >
              {([1, 2, 3] as FieldType[]).map((t) => (
                <div
                  key={t}
                  style={{
                    padding: '5px 8px',
                    fontSize: 11.5,
                    color: COLORS.ink,
                    borderRadius: 4,
                    background: field.highlightedDropdownOption === t ? 'rgba(45,80,22,0.08)' : 'transparent',
                  }}
                >
                  {TYPE_LABELS[t]}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Required checkbox */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 5, fontSize: 11, color: COLORS.ink2, fontFamily: FONTS.sans }}>
          <div
            style={{
              width: 13,
              height: 13,
              borderRadius: 3,
              border: `1.5px solid ${field.required ? COLORS.tennisGreen : '#cbd5e0'}`,
              background: field.required ? COLORS.tennisGreen : COLORS.white,
              display: 'grid',
              placeItems: 'center',
            }}
          >
            {field.required && <CheckIcon size={9} color={COLORS.white} />}
          </div>
          <span>Verplicht</span>
        </div>

        {/* Reorder + delete */}
        <div style={{ marginLeft: 'auto', display: 'flex', gap: 2 }}>
          <button
            type="button"
            disabled={isFirst}
            style={{
              width: 26,
              height: 26,
              display: 'grid',
              placeItems: 'center',
              borderRadius: 6,
              background: 'transparent',
              border: 'none',
              color: '#9ca3af',
              opacity: isFirst ? 0.3 : 1,
            }}
          >
            <ChevronUpIcon size={12} color="#9ca3af" />
          </button>
          <button
            type="button"
            disabled={isLast}
            style={{
              width: 26,
              height: 26,
              display: 'grid',
              placeItems: 'center',
              borderRadius: 6,
              background: 'transparent',
              border: 'none',
              opacity: isLast ? 0.3 : 1,
            }}
          >
            <ChevronDownIcon size={12} color="#9ca3af" />
          </button>
          <button
            type="button"
            style={{
              width: 26,
              height: 26,
              display: 'grid',
              placeItems: 'center',
              borderRadius: 6,
              background: 'transparent',
              border: 'none',
            }}
          >
            <TrashIcon size={12} color="#9ca3af" />
          </button>
        </div>
      </div>
    </div>
  );
};

export type SaveState = 'idle' | 'saving' | 'saved';

interface FormBuilderProps {
  customFields: CustomFieldProps[];
  addFieldHover?: boolean;
  addFieldPressed?: boolean;
  saveButtonHover?: boolean;
  saveButtonPressed?: boolean;
  saveState?: SaveState;
  // Reveal: 0..1 — how many of customFields are currently visible (with slide-in)
  fieldEntryProgress?: number[];
}

export const FormBuilderSection: React.FC<FormBuilderProps> = ({
  customFields,
  addFieldHover,
  addFieldPressed,
  saveButtonHover,
  saveButtonPressed,
  saveState = 'idle',
  fieldEntryProgress = [],
}) => {
  return (
    <div
      style={{
        background: COLORS.white,
        borderRadius: 12,
        boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
        border: `1px solid ${COLORS.rule}`,
        marginBottom: 14,
      }}
    >
      {/* Section header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          padding: '14px 18px',
          borderBottom: `1px solid #f3f4f6`,
        }}
      >
        <div
          style={{
            width: 24,
            height: 24,
            borderRadius: 6,
            background: 'rgba(45,80,22,0.1)',
            display: 'grid',
            placeItems: 'center',
          }}
        >
          <ClipboardIcon color={COLORS.tennisGreen} />
        </div>
        <span style={{ fontSize: 13.5, fontWeight: 600, color: COLORS.ink, fontFamily: FONTS.sans }}>
          Inschrijfformulier
        </span>
      </div>

      <div style={{ padding: '14px 18px' }}>
        {/* Predefined fields */}
        <p style={{ fontSize: 11, color: COLORS.ink3, margin: '0 0 8px', fontFamily: FONTS.sans }}>
          Vaste velden (altijd zichtbaar)
        </p>
        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginBottom: 14 }}>
          {PREDEFINED.map((p) => (
            <Badge key={p}>{p}</Badge>
          ))}
        </div>

        {/* Custom fields */}
        <p style={{ fontSize: 11, color: COLORS.ink3, margin: '0 0 8px', fontFamily: FONTS.sans }}>
          Aangepaste velden
        </p>
        {customFields.length === 0 ? (
          <p
            style={{
              fontSize: 11.5,
              color: COLORS.ink3,
              margin: '0 0 10px',
              padding: '8px 0',
              fontFamily: FONTS.sans,
            }}
          >
            Nog geen aangepaste velden. Klik 'Veld toevoegen' om te beginnen.
          </p>
        ) : (
          <div
            style={{
              display: 'flex',
              flexDirection: 'column',
              gap: 10,
              marginBottom: 12,
              position: 'relative',
              zIndex: 10,
            }}
          >
            {customFields.map((f, i) => {
              const progress = fieldEntryProgress[i] ?? 1;
              return (
                <div
                  key={i}
                  style={{
                    opacity: progress,
                    transform: `translateY(${(1 - progress) * 12}px)`,
                  }}
                >
                  <FieldRow field={f} isFirst={i === 0} isLast={i === customFields.length - 1} />
                </div>
              );
            })}
          </div>
        )}

        {/* Save row */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 8 }}>
          <button
            type="button"
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 5,
              padding: '7px 12px',
              fontSize: 11.5,
              fontWeight: 500,
              color: addFieldHover ? COLORS.ink : COLORS.ink2,
              border: `1px solid ${COLORS.rule}`,
              background: addFieldHover ? '#f9fafb' : COLORS.white,
              borderRadius: 8,
              transform: addFieldPressed ? 'scale(0.96)' : 'scale(1)',
              fontFamily: FONTS.sans,
            }}
          >
            <PlusIcon size={11} color={addFieldHover ? COLORS.ink : COLORS.ink2} />
            Veld toevoegen
          </button>
          <button
            type="button"
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 6,
              padding: '8px 16px',
              fontSize: 12,
              fontWeight: 600,
              color: COLORS.white,
              background: saveButtonHover ? '#264411' : COLORS.tennisGreen,
              border: 'none',
              borderRadius: 8,
              transform: saveButtonPressed ? 'scale(0.96)' : 'scale(1)',
              fontFamily: FONTS.sans,
            }}
          >
            {saveState === 'saved' && <CheckIcon size={11} color={COLORS.white} />}
            {saveState === 'idle' && 'Formulier opslaan'}
            {saveState === 'saving' && 'Opslaan…'}
            {saveState === 'saved' && 'Opgeslagen'}
          </button>
        </div>
      </div>
    </div>
  );
};

// ────────────────────────────────────────────────────────────────────────
//  Enrollments section header (with Inschrijflink button)
// ────────────────────────────────────────────────────────────────────────

interface EnrollmentsHeaderProps {
  copied?: boolean;
  shareButtonHover?: boolean;
  shareButtonPressed?: boolean;
}

export const EnrollmentsHeader: React.FC<EnrollmentsHeaderProps> = ({
  copied,
  shareButtonHover,
  shareButtonPressed,
}) => (
  <div
    style={{
      background: COLORS.white,
      borderRadius: 12,
      boxShadow: '0 1px 2px rgba(0,0,0,0.04)',
      border: `1px solid ${COLORS.rule}`,
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center',
      padding: '14px 18px',
    }}
  >
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      <span style={{ fontSize: 13.5, fontWeight: 600, color: COLORS.ink, fontFamily: FONTS.sans }}>
        Inschrijvingen
      </span>
      <span
        style={{
          fontSize: 11,
          fontFamily: FONTS.mono,
          color: COLORS.ink3,
          letterSpacing: '0.05em',
        }}
      >
        0
      </span>
    </div>
    <button
      type="button"
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        padding: '6px 12px',
        fontSize: 11.5,
        fontWeight: 500,
        color: copied ? '#16a34a' : COLORS.ink2,
        background: shareButtonHover ? '#f9fafb' : COLORS.white,
        border: `1px solid ${copied ? '#86efac' : COLORS.rule}`,
        borderRadius: 8,
        transform: shareButtonPressed ? 'scale(0.96)' : 'scale(1)',
        fontFamily: FONTS.sans,
      }}
    >
      {copied ? <CheckCircleIcon size={11} color="#16a34a" /> : <CopyIcon size={11} color={COLORS.ink2} />}
      {copied ? 'Link gekopieerd' : 'Inschrijflink'}
    </button>
  </div>
);
