import { Composition } from 'remotion';
import { AutoPlanner } from './compositions/AutoPlanner';
import { CreateLesreeks } from './compositions/CreateLesreeks';
import { EnrollmentForm } from './compositions/EnrollmentForm';
import { StudentEnrollment } from './compositions/StudentEnrollment';

// Each composition is one animated clip.
// Add new compositions here as we build them.
//
// Frame rate is 30fps everywhere — durationInFrames = seconds × 30.
// All clips are 1080x1080 to match the Instagram carousel format.

export const Root: React.FC = () => {
  return (
    <>
      <Composition
        id="AutoPlanner"
        component={AutoPlanner}
        durationInFrames={300}
        fps={30}
        width={1080}
        height={1080}
      />
      <Composition
        id="CreateLesreeks"
        component={CreateLesreeks}
        durationInFrames={270}
        fps={30}
        width={1080}
        height={1080}
      />
      <Composition
        id="EnrollmentForm"
        component={EnrollmentForm}
        durationInFrames={270}
        fps={30}
        width={1080}
        height={1080}
      />
      <Composition
        id="StudentEnrollment"
        component={StudentEnrollment}
        durationInFrames={300}
        fps={30}
        width={1080}
        height={1080}
      />
    </>
  );
};
