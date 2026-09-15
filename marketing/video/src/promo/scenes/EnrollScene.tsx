import { interpolate, spring, useCurrentFrame, useVideoConfig } from 'remotion';
import { COLORS, FONTS, SPRING_POP, SPRING_SOFT } from '../brand';
import { PhoneFrame } from '../../ui/PhoneFrame';
import { SceneShell } from '../SceneShell';
import type { PromoFormat } from '../layout';

// 10–16s. WhatsApp-link → formulier vult zich → succes. Geen account.

const FORM_FIELDS: { label: string; value: string; at: number }[] = [
  { label: 'Voornaam', value: 'Sven', at: 64 },
  { label: 'Achternaam', value: 'Janssens', at: 80 },
  { label: 'E-mail', value: 'sven@mail.be', at: 96 },
];

export const EnrollScene: React.FC<{ format: PromoFormat }> = ({ format }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const phone = spring({ frame: frame - 4, fps, config: SPRING_SOFT });
  const phoneOpacity = interpolate(frame, [4, 18], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  // WhatsApp → formulier wissel op frame 48.
  const switchAt = 48;
  const formT = interpolate(frame, [switchAt, switchAt + 10], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const submitAt = 124;
  const submitted = frame >= submitAt;
  const successAt = 136;
  const success = spring({ frame: frame - successAt, fps, config: SPRING_POP });

  // Exit (kaart schuift omhoog weg, zoals FloatingCard doet)
  const exitT = interpolate(frame, [166, 179], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const breathe = Math.sin((frame / fps) * Math.PI * 2 * 0.2) * 4;

  return (
    <SceneShell
      format={format}
      tag="03 · INSCHRIJVEN"
      headline={['Spelers schrijven', 'zich in. Zonder account.']}
    >
      <div
        style={{
          opacity: phoneOpacity * (1 - exitT),
          transform: `translateY(${interpolate(phone, [0, 1], [90, 0]) + breathe + exitT * -70}px) rotate(${interpolate(phone, [0, 1], [3, 0])}deg)`,
        }}
      >
        <PhoneFrame width={330} height={640}>
          {frame < switchAt && (
            /* WhatsApp-weergave */
            <div
              style={{
                flex: 1,
                background: '#E7E0D5',
                padding: '14px 12px',
                opacity: 1 - formT,
                display: 'flex',
                flexDirection: 'column',
                gap: 8,
              }}
            >
              <div
                style={{
                  alignSelf: 'center',
                  fontFamily: FONTS.sans,
                  fontSize: 10.5,
                  color: '#7B7268',
                  background: 'rgba(255,255,255,0.7)',
                  borderRadius: 8,
                  padding: '3px 10px',
                }}
              >
                Tennisclub De Linde
              </div>
              <div
                style={{
                  alignSelf: 'flex-start',
                  maxWidth: 240,
                  background: COLORS.white,
                  borderRadius: '2px 12px 12px 12px',
                  padding: '9px 11px',
                  fontFamily: FONTS.sans,
                  fontSize: 12.5,
                  color: COLORS.ink,
                  boxShadow: '0 1px 1px rgba(0,0,0,0.08)',
                  opacity: interpolate(frame, [14, 24], [0, 1], {
                    extrapolateLeft: 'clamp',
                    extrapolateRight: 'clamp',
                  }),
                  transform: `translateY(${interpolate(frame, [14, 24], [10, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })}px)`,
                }}
              >
                Inschrijven voor de voorjaarsreeks kan hier 👇
                <div
                  style={{
                    marginTop: 7,
                    padding: '8px 10px',
                    background: '#F4F8EE',
                    borderLeft: `3px solid ${COLORS.tennisGreen}`,
                    borderRadius: 6,
                    fontFamily: FONTS.mono,
                    fontSize: 11,
                    color: COLORS.tennisGreen,
                    fontWeight: 600,
                  }}
                >
                  coach-os.be/r/voorjaar-2026
                </div>
              </div>
            </div>
          )}

          {frame >= switchAt && (
            /* Inschrijfformulier */
            <div
              style={{
                flex: 1,
                background: COLORS.canvas,
                padding: '16px 16px',
                display: 'flex',
                flexDirection: 'column',
                gap: 10,
                opacity: formT,
              }}
            >
              <div style={{ fontFamily: FONTS.sans, fontSize: 16, fontWeight: 700, color: COLORS.ink }}>
                Voorjaarsreeks 2026
              </div>
              <div
                style={{
                  fontFamily: FONTS.mono,
                  fontSize: 10,
                  color: COLORS.ink3,
                  letterSpacing: '0.05em',
                  marginBottom: 2,
                }}
              >
                TC DE LINDE · €49 · GEEN ACCOUNT NODIG
              </div>
              {FORM_FIELDS.map((f) => {
                const chars = Math.round(
                  interpolate(frame, [f.at, f.at + 12], [0, f.value.length], {
                    extrapolateLeft: 'clamp',
                    extrapolateRight: 'clamp',
                  }),
                );
                return (
                  <div key={f.label}>
                    <div
                      style={{
                        fontFamily: FONTS.sans,
                        fontSize: 10.5,
                        fontWeight: 600,
                        color: COLORS.ink2,
                        marginBottom: 4,
                      }}
                    >
                      {f.label}
                    </div>
                    <div
                      style={{
                        background: COLORS.white,
                        border: `1px solid ${frame >= f.at && chars < f.value.length ? COLORS.tennisGreen : COLORS.rule}`,
                        borderRadius: 8,
                        padding: '9px 11px',
                        fontFamily: FONTS.sans,
                        fontSize: 13,
                        color: COLORS.ink,
                        minHeight: 36,
                      }}
                    >
                      {f.value.slice(0, chars)}
                      {chars > 0 && chars < f.value.length && (
                        <span style={{ color: COLORS.tennisGreen }}>|</span>
                      )}
                    </div>
                  </div>
                );
              })}
              {/* Voorkeur-chips vullen de ruimte en tonen het tijdslot-kiezen */}
              <div>
                <div
                  style={{
                    fontFamily: FONTS.sans,
                    fontSize: 10.5,
                    fontWeight: 600,
                    color: COLORS.ink2,
                    marginBottom: 4,
                  }}
                >
                  Voorkeur tijdslot
                </div>
                <div style={{ display: 'flex', gap: 6 }}>
                  {['Ma 17:00', 'Wo 17:00'].map((slot, i) => {
                    const at = 108 + i * 8;
                    const on = frame >= at;
                    return (
                      <div
                        key={slot}
                        style={{
                          padding: '7px 11px',
                          borderRadius: 999,
                          fontFamily: FONTS.mono,
                          fontSize: 11,
                          fontWeight: 600,
                          background: on ? 'rgba(208,255,20,0.22)' : COLORS.white,
                          border: `1px solid ${on ? COLORS.tennisGreen : COLORS.rule}`,
                          color: on ? COLORS.tennisGreen : COLORS.ink3,
                        }}
                      >
                        {slot}
                        {on ? ' ✓' : ''}
                      </div>
                    );
                  })}
                </div>
              </div>
              <div style={{ flex: 1 }} />
              {!submitted ? (
                <div
                  style={{
                    height: 44,
                    borderRadius: 10,
                    background: COLORS.tennisGreen,
                    color: COLORS.white,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontFamily: FONTS.sans,
                    fontSize: 14,
                    fontWeight: 700,
                    transform: frame >= submitAt - 5 ? 'scale(0.96)' : 'scale(1)',
                  }}
                >
                  Schrijf me in
                </div>
              ) : (
                <div
                  style={{
                    height: 44,
                    borderRadius: 10,
                    background: 'rgba(208,255,20,0.25)',
                    border: `1px solid ${COLORS.tennisGreen}`,
                    color: COLORS.tennisGreen,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    gap: 8,
                    fontFamily: FONTS.sans,
                    fontSize: 14,
                    fontWeight: 700,
                    transform: `scale(${interpolate(success, [0, 1], [0.8, 1])})`,
                  }}
                >
                  <svg
                    width={16}
                    height={16}
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth={3}
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    style={{ transform: `scale(${success})` }}
                  >
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                  Ingeschreven!
                </div>
              )}
            </div>
          )}
        </PhoneFrame>
      </div>
    </SceneShell>
  );
};
