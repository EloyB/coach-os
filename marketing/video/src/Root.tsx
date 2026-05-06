import { Composition } from "remotion";
import { Problem } from "./scenes/Problem";
import { Transition } from "./scenes/Transition";
import { Planning } from "./scenes/Planning";
import { Enrollments } from "./scenes/Enrollments";
import { EnrollmentForm } from "./scenes/EnrollmentForm";
import { Payments } from "./scenes/Payments";
import { Closing } from "./scenes/Closing";
import { MainVideo, SCENE_DURATIONS, TOTAL_DURATION } from "./MainVideo";

const FPS = 30;
const W = 1920;
const H = 1080;

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="MainVideo"
        component={MainVideo}
        durationInFrames={TOTAL_DURATION}
        fps={FPS}
        width={W}
        height={H}
      />
      <Composition
        id="Problem"
        component={Problem}
        durationInFrames={SCENE_DURATIONS.problem}
        fps={FPS}
        width={W}
        height={H}
      />
      <Composition
        id="Transition"
        component={Transition}
        durationInFrames={SCENE_DURATIONS.transition}
        fps={FPS}
        width={W}
        height={H}
      />
      <Composition
        id="Planning"
        component={Planning}
        durationInFrames={SCENE_DURATIONS.planning}
        fps={FPS}
        width={W}
        height={H}
      />
      <Composition
        id="Enrollments"
        component={Enrollments}
        durationInFrames={SCENE_DURATIONS.enrollments}
        fps={FPS}
        width={W}
        height={H}
      />
      <Composition
        id="EnrollmentForm"
        component={EnrollmentForm}
        durationInFrames={SCENE_DURATIONS.enrollmentForm}
        fps={FPS}
        width={W}
        height={H}
      />
      <Composition
        id="Payments"
        component={Payments}
        durationInFrames={SCENE_DURATIONS.payments}
        fps={FPS}
        width={W}
        height={H}
      />
      <Composition
        id="Closing"
        component={Closing}
        durationInFrames={SCENE_DURATIONS.closing}
        fps={FPS}
        width={W}
        height={H}
      />
    </>
  );
};
