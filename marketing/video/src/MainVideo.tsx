import { Easing } from "remotion";
import { linearTiming, TransitionSeries } from "@remotion/transitions";
import { fade } from "@remotion/transitions/fade";
import { Problem } from "./scenes/Problem";
import { Transition } from "./scenes/Transition";
import { Planning } from "./scenes/Planning";
import { Enrollments } from "./scenes/Enrollments";
import { EnrollmentForm } from "./scenes/EnrollmentForm";
import { Payments } from "./scenes/Payments";
import { Closing } from "./scenes/Closing";

// Each scene gets ~0.4s of extra hold time to compensate for transition overlap,
// so the final composited video hits ~30s exactly.
export const SCENE_DURATIONS = {
  problem: 162,        // 5s + buffer
  transition: 72,      // 2s + buffer
  planning: 162,       // 5s + buffer
  enrollments: 162,    // 5s + buffer
  enrollmentForm: 162, // 5s + buffer
  payments: 162,       // 5s + buffer
  closing: 102,        // 3s + buffer
} as const;

const TRANSITIONS = {
  problemToTransition: 12,
  transitionToPlanning: 14,
  planningToEnrollments: 14,
  enrollmentsToForm: 14,
  formToPayments: 14,
  paymentsToClosing: 16,
} as const;

export const TOTAL_DURATION =
  Object.values(SCENE_DURATIONS).reduce((a, b) => a + b, 0) -
  Object.values(TRANSITIONS).reduce((a, b) => a + b, 0);

const smooth = (frames: number) =>
  linearTiming({
    durationInFrames: frames,
    easing: Easing.bezier(0.22, 1, 0.36, 1),
  });

export const MainVideo: React.FC = () => {
  return (
    <TransitionSeries>
      <TransitionSeries.Sequence durationInFrames={SCENE_DURATIONS.problem}>
        <Problem />
      </TransitionSeries.Sequence>

      <TransitionSeries.Transition presentation={fade()} timing={smooth(TRANSITIONS.problemToTransition)} />

      <TransitionSeries.Sequence durationInFrames={SCENE_DURATIONS.transition}>
        <Transition />
      </TransitionSeries.Sequence>

      <TransitionSeries.Transition presentation={fade()} timing={smooth(TRANSITIONS.transitionToPlanning)} />

      <TransitionSeries.Sequence durationInFrames={SCENE_DURATIONS.planning}>
        <Planning />
      </TransitionSeries.Sequence>

      <TransitionSeries.Transition presentation={fade()} timing={smooth(TRANSITIONS.planningToEnrollments)} />

      <TransitionSeries.Sequence durationInFrames={SCENE_DURATIONS.enrollments}>
        <Enrollments />
      </TransitionSeries.Sequence>

      <TransitionSeries.Transition presentation={fade()} timing={smooth(TRANSITIONS.enrollmentsToForm)} />

      <TransitionSeries.Sequence durationInFrames={SCENE_DURATIONS.enrollmentForm}>
        <EnrollmentForm />
      </TransitionSeries.Sequence>

      <TransitionSeries.Transition presentation={fade()} timing={smooth(TRANSITIONS.formToPayments)} />

      <TransitionSeries.Sequence durationInFrames={SCENE_DURATIONS.payments}>
        <Payments />
      </TransitionSeries.Sequence>

      <TransitionSeries.Transition presentation={fade()} timing={smooth(TRANSITIONS.paymentsToClosing)} />

      <TransitionSeries.Sequence durationInFrames={SCENE_DURATIONS.closing}>
        <Closing />
      </TransitionSeries.Sequence>
    </TransitionSeries>
  );
};
