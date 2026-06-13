import { Series } from 'remotion';
import { PROMO_DURATIONS, type PromoFormat } from './layout';
import { HookScene } from './scenes/HookScene';
import { SeriesScene } from './scenes/SeriesScene';
import { EnrollScene } from './scenes/EnrollScene';
import { PlannerScene } from './scenes/PlannerScene';
import { PaymentsScene } from './scenes/PaymentsScene';
import { OutroScene } from './scenes/OutroScene';

// ~35s promo. Harde cuts; elke scène regelt z'n eigen entry/exit-beweging.

export const Promo: React.FC<{ format: PromoFormat }> = ({ format }) => (
  <Series>
    <Series.Sequence durationInFrames={PROMO_DURATIONS.hook}>
      <HookScene format={format} />
    </Series.Sequence>
    <Series.Sequence durationInFrames={PROMO_DURATIONS.series}>
      <SeriesScene format={format} />
    </Series.Sequence>
    <Series.Sequence durationInFrames={PROMO_DURATIONS.enroll}>
      <EnrollScene format={format} />
    </Series.Sequence>
    <Series.Sequence durationInFrames={PROMO_DURATIONS.planner}>
      <PlannerScene format={format} />
    </Series.Sequence>
    <Series.Sequence durationInFrames={PROMO_DURATIONS.payments}>
      <PaymentsScene format={format} />
    </Series.Sequence>
    <Series.Sequence durationInFrames={PROMO_DURATIONS.outro}>
      <OutroScene format={format} />
    </Series.Sequence>
  </Series>
);
