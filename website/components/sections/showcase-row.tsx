import { Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import { ScreenshotFrame } from "@/components/site/screenshot-frame";
import { AnimatedLessonWeekMock } from "@/components/sections/animated-lesson-week-mock";
import { AnimatedFormBuilderMock } from "@/components/sections/animated-form-builder-mock";
import { AnimatedEnrollmentMock } from "@/components/sections/animated-enrollment-mock";
import { AnimatedPlanningMock } from "@/components/sections/animated-planning-mock";
import type { ShowcaseItem } from "@/content/showcase";

/** Per-item animated mocks. When a row has an entry here, it renders inside the
 * frame chrome instead of using the static screenshot/placeholder. */
const ANIMATED_MOCKS: Record<string, React.ReactNode> = {
  lessenreeksen: <AnimatedLessonWeekMock />,
  formulierbouwer: <AnimatedFormBuilderMock />,
  "anonieme-inschrijving": <AnimatedEnrollmentMock />,
  planningsalgoritme: <AnimatedPlanningMock />,
};

interface ShowcaseRowProps {
  item: ShowcaseItem;
  /** Reverse the column order on lg+ screens, pass true for every other row. */
  reverse?: boolean;
}

export function ShowcaseRow({ item, reverse = false }: ShowcaseRowProps) {
  const Icon = item.icon;
  const expectedFilename = `/screenshots/${item.id}.png`;

  return (
    <div
      className={cn(
        "grid items-center gap-12 lg:grid-cols-[0.95fr_1.05fr] lg:gap-16",
        reverse && "lg:[&>*:first-child]:order-2",
      )}
    >
      <div className="max-w-xl">
        <div className="flex items-center gap-2">
          <span className="inline-flex h-7 w-7 items-center justify-center rounded-md bg-tennis-green text-tennis-lime">
            <Icon className="h-3.5 w-3.5" />
          </span>
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            {item.kicker}
          </Mono>
        </div>
        <h3 className="mt-5 text-3xl font-bold leading-[1.1] tracking-tight md:text-4xl">
          {item.heading}
        </h3>
        <p className="mt-4 text-base leading-relaxed text-ink-2">{item.body}</p>
        <ul className="mt-6 space-y-3">
          {item.bullets.map((b) => (
            <li key={b} className="flex gap-3 text-sm text-ink-2">
              <span className="mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-tennis-lime/30 text-tennis-green">
                <Check className="h-3 w-3" strokeWidth={3} />
              </span>
              <span>{b}</span>
            </li>
          ))}
        </ul>
      </div>

      <div className="relative">
        {/* Ambient gradient halo behind the frame */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 -z-10 translate-y-6 rounded-[2rem] bg-gradient-to-br from-tennis-lime/20 via-tennis-beige/30 to-transparent blur-2xl"
        />
        <ScreenshotFrame
          chrome={item.chrome}
          image={item.image}
          expectedFilename={expectedFilename}
        >
          {ANIMATED_MOCKS[item.id]}
        </ScreenshotFrame>
      </div>
    </div>
  );
}
