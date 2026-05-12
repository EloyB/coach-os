import { useCurrentFrame, useVideoConfig, interpolate, spring, Easing } from 'remotion';
import { BrandFrame } from '../components/BrandFrame';
import { PhoneFrame } from '../components/PhoneFrame';
import { Cursor } from '../components/Cursor';
import {
  WhatsAppScreen,
  EnrollmentScreen,
  SuccessScreen,
  type EnrollmentValues,
  type ActiveField,
} from '../components/StudentScreens';

// "Inschrijven voor een lessenreeks" — 300 frames @ 30fps = 10 seconds.
// Carousel slide 4 (03 / 04 in chrome).
//
// Timeline reference:
//   0–18    entry: phone scales in (WhatsApp screen), cursor enters
//   18–55   cursor approaches link preview card
//   55–65   tap link → card highlight + transition begins
//   65–80   cross-fade WhatsApp → enrollment page
//   80–95   cursor lands on Voornaam input
//   95–115  type "Sven"
//   115–125 cursor moves to Achternaam
//   125–148 type "Janssens"
//   148–158 cursor moves to Email
//   158–195 type "sven.j@gmail.com"
//   195–205 cursor taps Maandag Voorkeur
//   205–215 cursor taps Woensdag Beschikbaar
//   215–225 cursor taps Vrijdag Beschikbaar
//   225–238 cursor moves to Inschrijven button + press
//   238–252 submitting (spinner)
//   252–300 success screen revealed (long hold so viewers can read it)

const FIRST_NAME = 'Sven';
const LAST_NAME = 'Janssens';
const EMAIL = 'sven.j@gmail.com';

type Waypoint = { f: number; x: number; y: number; pressed?: boolean };

const WAYPOINTS: Waypoint[] = [
  { f: 0,   x: 1140, y: 1140 },
  // Approach WhatsApp link card
  { f: 22,  x: 534,  y: 420 },
  { f: 58,  x: 534,  y: 420, pressed: true },
  { f: 64,  x: 534,  y: 420 },
  // Hold while transition plays
  { f: 80,  x: 534,  y: 420 },
  // Move to Voornaam input
  { f: 92,  x: 461,  y: 487 },
  { f: 115, x: 461,  y: 487 },
  // Achternaam
  { f: 124, x: 619,  y: 487 },
  { f: 148, x: 619,  y: 487 },
  // Email
  { f: 156, x: 540,  y: 549 },
  { f: 195, x: 540,  y: 549 },
  // Maandag Voorkeur bubble
  { f: 202, x: 611,  y: 630 },
  { f: 203, x: 611,  y: 630, pressed: true },
  { f: 205, x: 611,  y: 630 },
  // Woensdag Beschikbaar bubble
  { f: 211, x: 645,  y: 668 },
  { f: 212, x: 645,  y: 668, pressed: true },
  { f: 215, x: 645,  y: 668 },
  // Vrijdag Beschikbaar bubble
  { f: 221, x: 645,  y: 705 },
  { f: 222, x: 645,  y: 705, pressed: true },
  { f: 225, x: 645,  y: 705 },
  // Submit button (Inschrijven)
  { f: 234, x: 540,  y: 750 },
  { f: 236, x: 540,  y: 750, pressed: true },
  { f: 240, x: 540,  y: 750 },
  // Drift off during submit/success
  { f: 260, x: 540,  y: 940 },
  { f: 300, x: 540,  y: 940 },
];

function cursorAt(frame: number, waypoints: Waypoint[]): { x: number; y: number; pressed: boolean } {
  if (frame <= waypoints[0].f) return { x: waypoints[0].x, y: waypoints[0].y, pressed: !!waypoints[0].pressed };
  for (let i = 1; i < waypoints.length; i++) {
    const prev = waypoints[i - 1];
    const curr = waypoints[i];
    if (frame <= curr.f) {
      const span = curr.f - prev.f;
      const t = span === 0 ? 1 : (frame - prev.f) / span;
      const eased = Easing.bezier(0.25, 0.1, 0.25, 1)(t);
      return {
        x: prev.x + (curr.x - prev.x) * eased,
        y: prev.y + (curr.y - prev.y) * eased,
        pressed: !!curr.pressed,
      };
    }
  }
  const last = waypoints[waypoints.length - 1];
  return { x: last.x, y: last.y, pressed: !!last.pressed };
}

function getActiveField(frame: number): ActiveField {
  if (frame >= 88 && frame < 120) return 'first';
  if (frame >= 120 && frame < 150) return 'last';
  if (frame >= 150 && frame < 200) return 'email';
  return null;
}

export const StudentEnrollment: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const cur = cursorAt(frame, WAYPOINTS);
  const cursorBlink = Math.floor(frame / 15) % 2 === 0;

  // ─── WhatsApp → Enrollment screen cross-fade
  const whatsAppOpacity = interpolate(frame, [65, 78], [1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const enrollmentOpacity =
    interpolate(frame, [65, 78], [0, 1], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' }) *
    interpolate(frame, [248, 256], [1, 0], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' });
  const successOpacity = interpolate(frame, [248, 256], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const successProgress = interpolate(frame, [252, 270], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });

  // Link card highlight when pressed
  const linkTapHighlight = frame >= 56 && frame < 64;

  // Typed values
  const f1 = Math.max(0, Math.floor(interpolate(frame, [95, 113], [0, FIRST_NAME.length], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })));
  const firstName = FIRST_NAME.slice(0, f1);
  const l1 = Math.max(0, Math.floor(interpolate(frame, [126, 146], [0, LAST_NAME.length], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })));
  const lastName = LAST_NAME.slice(0, l1);
  const e1 = Math.max(0, Math.floor(interpolate(frame, [160, 193], [0, EMAIL.length], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })));
  const email = EMAIL.slice(0, e1);

  const availability: EnrollmentValues['availability'] = {
    ma: frame >= 203 ? 'preferred' : null,
    wo: frame >= 212 ? 'available' : null,
    vr: frame >= 222 ? 'available' : null,
  };

  const submitButtonHover = frame >= 230 && frame < 244;
  const submitButtonPressed = frame >= 236 && frame < 240;
  const submitting = frame >= 240 && frame < 254;

  const values: EnrollmentValues = { firstName, lastName, email, availability };
  const activeField = getActiveField(frame);

  return (
    <BrandFrame step="03 / 04" title="Speler inschrijven">
      <PhoneFrame entryFrame={0}>
        {/* Stack screens at the same position; toggle via opacity. */}
        <div style={{ position: 'relative', flex: 1, display: 'flex', overflow: 'hidden' }}>
          {whatsAppOpacity > 0 && (
            <div style={{ position: 'absolute', inset: 0, opacity: whatsAppOpacity, display: 'flex' }}>
              <WhatsAppScreen tapHighlight={linkTapHighlight} />
            </div>
          )}
          {enrollmentOpacity > 0 && (
            <div style={{ position: 'absolute', inset: 0, opacity: enrollmentOpacity, display: 'flex' }}>
              <EnrollmentScreen
                values={values}
                activeField={activeField}
                cursorBlink={cursorBlink}
                submitButtonHover={submitButtonHover}
                submitButtonPressed={submitButtonPressed}
                submitting={submitting}
              />
            </div>
          )}
          {successOpacity > 0 && (
            <div style={{ position: 'absolute', inset: 0, opacity: successOpacity, display: 'flex' }}>
              <SuccessScreen progress={successProgress} />
            </div>
          )}
        </div>
      </PhoneFrame>

      <Cursor x={cur.x} y={cur.y} pressed={cur.pressed} />
    </BrandFrame>
  );
};
