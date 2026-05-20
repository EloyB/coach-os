import { ArrowRight } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { cn } from "@/lib/utils";
import {
  formatDeadlineShort,
  formatLessonDateRange,
  type PublicLessonSeries,
} from "@/content/lesson-series";

interface LessonSeriesCardProps {
  series: PublicLessonSeries;
}

export function LessonSeriesCard({ series }: LessonSeriesCardProps) {
  const enrolled = series.capacity - series.spotsLeft;
  const fillPercent = Math.min(
    100,
    Math.round((enrolled / Math.max(series.capacity, 1)) * 100),
  );
  const isFull = series.spotsLeft === 0;
  const isAlmostFull = !isFull && series.spotsLeft <= 2;

  return (
    <article className="group flex flex-col rounded-xl border border-rule bg-paper transition-colors hover:border-tennis-green/40">
      <header className="flex items-start justify-between gap-3 px-6 pt-5">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={cn(
              "inline-flex items-center rounded-full px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.12em]",
              series.sport === "tennis"
                ? "bg-tennis-green text-tennis-lime"
                : "bg-tennis-lime/30 text-tennis-green",
            )}
          >
            {series.sport === "tennis" ? "Tennis" : "Padel"}
          </span>
          {series.level ? (
            <span className="inline-flex items-center rounded-full border border-rule bg-canvas px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.12em] text-ink-2">
              {series.level}
            </span>
          ) : null}
        </div>
        <div className="text-right">
          <div className="text-2xl font-bold tabular-nums tracking-tight text-ink">
            €{series.pricePerSeriesEur}
          </div>
          <Mono className="text-[10px] tracking-[0.08em] text-ink-3">
            VOLLEDIGE REEKS
          </Mono>
        </div>
      </header>

      <div className="flex flex-1 flex-col gap-5 px-6 py-5">
        <div>
          <h3 className="text-xl font-bold leading-tight tracking-tight">
            {series.name}
          </h3>
          <p className="mt-1.5 text-sm text-ink-2">
            {series.clubName}
            <span className="text-ink-3"> · {series.clubCity}</span>
          </p>
        </div>

        <div className="flex items-baseline justify-between gap-3 text-sm">
          <span className="font-medium text-ink">
            {series.lessonCount} {series.lessonCount === 1 ? "les" : "lessen"}
          </span>
          <Mono className="text-xs text-ink-2">
            {formatLessonDateRange(series.startDate, series.endDate)}
          </Mono>
        </div>
      </div>

      <footer className="flex flex-col gap-3 border-t border-rule bg-canvas/60 px-6 py-4">
        <div className="flex items-center justify-between gap-3 text-xs">
          <span
            className={cn(
              "font-semibold",
              isFull
                ? "text-urgent"
                : isAlmostFull
                  ? "text-warn"
                  : "text-ink-2",
            )}
          >
            {isFull
              ? "Volzet"
              : `Nog ${series.spotsLeft} van ${series.capacity} plekken`}
          </span>
          <Mono className="tabular-nums text-ink-3">
            Inschrijven tot {formatDeadlineShort(series.registrationDeadline)}
          </Mono>
        </div>

        <div className="h-1 overflow-hidden rounded-full bg-rule">
          <div
            className={cn(
              "h-full rounded-full transition-all",
              isFull
                ? "bg-urgent"
                : isAlmostFull
                  ? "bg-warn"
                  : "bg-tennis-green",
            )}
            style={{ width: `${fillPercent}%` }}
          />
        </div>

        <button
          type="button"
          disabled={isFull}
          className={cn(
            "mt-1 inline-flex h-10 items-center justify-center gap-1.5 rounded-md text-sm font-semibold transition-colors",
            isFull
              ? "cursor-not-allowed bg-rule text-ink-3"
              : "bg-ink text-paper hover:bg-ink/90",
          )}
        >
          {isFull ? "Volzet" : "Bekijk en schrijf in"}
          {!isFull ? <ArrowRight className="h-3.5 w-3.5" /> : null}
        </button>
      </footer>
    </article>
  );
}
