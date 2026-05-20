import { Series, useCurrentFrame, interpolate, Easing } from 'remotion';
import { BrandFrame } from '../components/BrandFrame';
import { CreateLesreeks } from './CreateLesreeks';
import { EnrollmentForm } from './EnrollmentForm';
import { StudentEnrollment } from './StudentEnrollment';
import { AutoPlanner } from './AutoPlanner';
import { COLORS, FONTS } from '../brand';

// Long-form LinkedIn cut — all four feature scenes back-to-back, bookended
// by intro + outro cards. 1290 frames @ 30fps = 43s.
//
// Sequence layout:
//   0    – 75    intro card (2.5s)
//   75   – 345   CreateLesreeks (9s)
//   345  – 615   EnrollmentForm (9s)
//   615  – 915   StudentEnrollment (10s)
//   915  – 1215  AutoPlanner (10s)
//   1215 – 1290  outro card (2.5s)

const FADE_IN = 14;
const FADE_OUT_START = 60;

const CenteredCard: React.FC<{
  eyebrow: string;
  headline: React.ReactNode;
  subline?: React.ReactNode;
}> = ({ eyebrow, headline, subline }) => {
  const frame = useCurrentFrame();
  const fade = interpolate(frame, [0, FADE_IN], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.out(Easing.cubic),
  });
  const lift = interpolate(frame, [0, FADE_IN], [12, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
    easing: Easing.out(Easing.cubic),
  });
  const fadeOut = interpolate(frame, [FADE_OUT_START, 75], [1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const opacity = fade * fadeOut;

  return (
    <BrandFrame>
      <div
        style={{
          position: 'absolute',
          inset: 0,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'flex-start',
          justifyContent: 'center',
          padding: '0 80px',
          opacity,
          transform: `translateY(${lift}px)`,
        }}
      >
        <span
          style={{
            fontFamily: FONTS.mono,
            fontSize: 26,
            fontWeight: 700,
            color: COLORS.lime,
            letterSpacing: 5,
            marginBottom: 32,
          }}
        >
          {eyebrow}
        </span>
        <h1
          style={{
            fontFamily: FONTS.sans,
            fontSize: 140,
            fontWeight: 800,
            color: COLORS.white,
            letterSpacing: -5,
            lineHeight: 1.02,
            margin: 0,
          }}
        >
          {headline}
        </h1>
        {subline && (
          <p
            style={{
              fontFamily: FONTS.sans,
              fontSize: 30,
              fontWeight: 600,
              color: COLORS.lime,
              letterSpacing: -0.3,
              marginTop: 40,
              maxWidth: 820,
              lineHeight: 1.3,
            }}
          >
            {subline}
          </p>
        )}
      </div>
    </BrandFrame>
  );
};

const Intro: React.FC = () => (
  <CenteredCard
    eyebrow="/RONDLEIDING"
    headline={
      <>
        Wat kan
        <br />
        Coach<span style={{ color: COLORS.lime }}>OS</span>?
      </>
    }
    subline="Een lessenreeks in 4 stappen"
  />
);

const Outro: React.FC = () => (
  <CenteredCard
    eyebrow="/MEER INFO"
    headline={
      <>
        Surf naar
        <br />
        <span style={{ color: COLORS.lime }}>coach-os.be</span>
      </>
    }
    subline="Voor meer informatie"
  />
);

export const FeatureCarouselFull: React.FC = () => (
  <Series>
    <Series.Sequence durationInFrames={75}>
      <Intro />
    </Series.Sequence>
    <Series.Sequence durationInFrames={270}>
      <CreateLesreeks />
    </Series.Sequence>
    <Series.Sequence durationInFrames={270}>
      <EnrollmentForm />
    </Series.Sequence>
    <Series.Sequence durationInFrames={300}>
      <StudentEnrollment />
    </Series.Sequence>
    <Series.Sequence durationInFrames={300}>
      <AutoPlanner />
    </Series.Sequence>
    <Series.Sequence durationInFrames={75}>
      <Outro />
    </Series.Sequence>
  </Series>
);
