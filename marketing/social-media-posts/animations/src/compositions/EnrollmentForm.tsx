import { useCurrentFrame, useVideoConfig, interpolate, spring, Easing } from 'remotion';
import { BrandFrame } from '../components/BrandFrame';
import { AppWindow } from '../components/AppWindow';
import { SidebarMock } from '../components/SidebarMock';
import { Cursor } from '../components/Cursor';
import {
  SeriesHeader,
  FormBuilderSection,
  EnrollmentsHeader,
  type CustomFieldProps,
  type SaveState,
  type FieldType,
} from '../components/LessonDetailMock';
import { ShareLinkReveal } from '../components/ShareLinkReveal';
import { COLORS } from '../brand';

// "Aanmaken van een inschrijfformulier en de link delen" — 270 frames @ 30fps.
// Carousel slide 3 (02 / 04 in chrome).
//
// Timeline reference (frames):
//   0–15    entry: detail page visible, empty form-builder, cursor enters
//   15–22   cursor presses "Veld toevoegen" (empty state)
//   22–50   field 1 enters, type "Telefoonnummer"
//   50–58   cursor toggles "Verplicht" checkbox
//   58–70   cursor presses "Veld toevoegen" again
//   70–105  field 2 enters, type "Eigen racket meebrengen?"
//   105–130 type dropdown opens, "Ja/Nee" highlighted + selected
//   130–145 cursor presses "Formulier opslaan"
//   145–160 save shows "Opslaan…" → "Opgeslagen"
//   160–180 cursor moves up to "Inschrijflink", presses
//   180–195 button shows "Link gekopieerd"
//   195–225 share-link reveal card springs in over window
//   225–270 card holds (final beat)

const FIELD1_LABEL = 'Telefoonnummer';
const FIELD2_LABEL = 'Eigen racket meebrengen?';
const PUBLIC_URL = 'coach-os.be/enroll/voorjaarsreeks-2026';

type Waypoint = { f: number; x: number; y: number; pressed?: boolean };

const WAYPOINTS: Waypoint[] = [
  { f: 0,   x: 1140, y: 1140 },
  // Empty state: Veld toevoegen at top-left of save row.
  { f: 12,  x: 420,  y: 515 },
  { f: 18,  x: 420,  y: 515, pressed: true },
  { f: 22,  x: 420,  y: 515 },
  // Move to field 1 label input
  { f: 28,  x: 650,  y: 490 },
  { f: 50,  x: 650,  y: 490 },
  // Required checkbox on field 1
  { f: 56,  x: 498,  y: 530 },
  { f: 57,  x: 498,  y: 530, pressed: true },
  { f: 60,  x: 498,  y: 530 },
  // Veld toevoegen — now field 1 has pushed the save row down
  { f: 66,  x: 420,  y: 585 },
  { f: 68,  x: 420,  y: 585, pressed: true },
  { f: 72,  x: 420,  y: 585 },
  // Field 2 label input
  { f: 78,  x: 650,  y: 595 },
  { f: 105, x: 650,  y: 595 },
  // Type dropdown trigger on field 2
  { f: 112, x: 420,  y: 632 },
  { f: 114, x: 420,  y: 632, pressed: true },
  { f: 117, x: 420,  y: 632 },
  // Drift down through dropdown
  { f: 122, x: 420,  y: 680 },
  // Hover and click Ja/Nee (bottom option)
  { f: 126, x: 420,  y: 715 },
  { f: 128, x: 420,  y: 715, pressed: true },
  { f: 132, x: 420,  y: 715 },
  // Formulier opslaan (with 2 fields, save row at ~y=690)
  { f: 142, x: 880,  y: 690 },
  { f: 144, x: 880,  y: 690, pressed: true },
  { f: 148, x: 880,  y: 690 },
  // Hold during saving → saved
  { f: 160, x: 880,  y: 690 },
  // Up to Inschrijflink button in EnrollmentsHeader (below form-builder card)
  { f: 175, x: 895,  y: 760 },
  { f: 178, x: 895,  y: 760, pressed: true },
  { f: 182, x: 895,  y: 760 },
  // Drift off during share reveal
  { f: 210, x: 1050, y: 1000 },
  { f: 270, x: 1050, y: 1000 },
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

export const EnrollmentForm: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const cur = cursorAt(frame, WAYPOINTS);
  const cursorBlink = Math.floor(frame / 15) % 2 === 0;

  // Field entry progress
  const field1EntryProgress = interpolate(frame, [22, 32], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });
  const field2EntryProgress = interpolate(frame, [70, 80], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });

  // Field 1 — typing 28..50
  const f1Chars = Math.max(0, Math.floor(interpolate(frame, [28, 50], [0, FIELD1_LABEL.length], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })));
  const field1Label = FIELD1_LABEL.slice(0, f1Chars);
  const field1ActiveInput = frame >= 28 && frame < 54;
  const field1Required = frame >= 57;

  // Field 2 — typing 78..105
  const f2Chars = Math.max(0, Math.floor(interpolate(frame, [78, 105], [0, FIELD2_LABEL.length], { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' })));
  const field2Label = FIELD2_LABEL.slice(0, f2Chars);
  const field2ActiveInput = frame >= 78 && frame < 110;

  // Type dropdown on field 2
  const typeDropdownOpen = frame >= 114 && frame < 132;
  // Highlight progression: starts at top (Vrije tekst), moves down to Ja/Nee
  let highlightedType: FieldType = 1;
  if (typeDropdownOpen) {
    if (frame < 120) highlightedType = 1;
    else if (frame < 124) highlightedType = 2;
    else highlightedType = 3;
  }
  const field2Type: FieldType = frame >= 132 ? 3 : 1;

  const customFields: CustomFieldProps[] = [];
  if (frame >= 22) {
    customFields.push({
      label: field1Label,
      cursorOnLabel: field1ActiveInput,
      cursorBlink,
      type: 1,
      required: field1Required,
    });
  }
  if (frame >= 70) {
    customFields.push({
      label: field2Label,
      cursorOnLabel: field2ActiveInput,
      cursorBlink,
      type: field2Type,
      required: false,
      typeDropdownOpen,
      highlightedDropdownOption: typeDropdownOpen ? highlightedType : undefined,
    });
  }

  // Add Field hover/press
  const addFieldPressed =
    (frame >= 18 && frame < 22) || (frame >= 68 && frame < 72);
  const addFieldHover =
    (frame >= 10 && frame < 24) || (frame >= 64 && frame < 74);

  // Save button
  const saveButtonHover = frame >= 138 && frame < 162;
  const saveButtonPressed = frame >= 144 && frame < 148;
  let saveState: SaveState = 'idle';
  if (frame >= 148 && frame < 156) saveState = 'saving';
  else if (frame >= 156) saveState = 'saved';

  // Share button
  const shareButtonHover = frame >= 170 && frame < 196;
  const shareButtonPressed = frame >= 178 && frame < 182;
  const shareCopied = frame >= 182;

  // Share reveal — springs in at f=195
  const shareScale = spring({
    frame: frame - 195,
    fps,
    config: { damping: 13, mass: 0.7, stiffness: 130 },
    durationInFrames: 28,
  });
  const shareProgress = interpolate(frame, [195, 210], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  // App window fades out as share reveal comes in
  const appOpacity = interpolate(frame, [195, 210], [1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <BrandFrame step="03 / 05" title="Inschrijfformulier delen">
      {/* App window fades out as the share reveal takes over */}
      <div style={{ position: 'absolute', inset: 0, opacity: appOpacity }}>
        <AppWindow entryFrame={0}>
          <SidebarMock />
          <div
            style={{
              position: 'relative',
              flex: 1,
              background: COLORS.canvas,
              padding: '28px 32px',
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
            }}
          >
            <SeriesHeader />
            <FormBuilderSection
              customFields={customFields}
              fieldEntryProgress={[field1EntryProgress, field2EntryProgress]}
              addFieldHover={addFieldHover}
              addFieldPressed={addFieldPressed}
              saveButtonHover={saveButtonHover}
              saveButtonPressed={saveButtonPressed}
              saveState={saveState}
            />
            <EnrollmentsHeader
              copied={shareCopied}
              shareButtonHover={shareButtonHover}
              shareButtonPressed={shareButtonPressed}
            />
          </div>
        </AppWindow>
      </div>

      {/* Share reveal sits over the (fading) app window */}
      {shareProgress > 0 && (
        <ShareLinkReveal progress={shareProgress} scale={shareScale} url={PUBLIC_URL} />
      )}

      <Cursor x={cur.x} y={cur.y} pressed={cur.pressed} />
    </BrandFrame>
  );
};
