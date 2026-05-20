"use client";

import { useMemo, useState } from "react";
import { ChevronDown, SearchX, X } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { cn } from "@/lib/utils";
import { LessonSeriesCard } from "@/components/sections/lesson-series-card";
import type {
  LessonLevel,
  PublicLessonSeries,
  Sport,
} from "@/content/lesson-series";

interface LessonSeriesGridProps {
  series: PublicLessonSeries[];
}

const LEVEL_ORDER: LessonLevel[] = ["Beginner", "Intermediate", "Gevorderd"];

type SportFilter = Sport | "all";

const SPORT_FILTERS: { value: SportFilter; label: string }[] = [
  { value: "all", label: "Alles" },
  { value: "tennis", label: "Tennis" },
  { value: "padel", label: "Padel" },
];

export function LessonSeriesGrid({ series }: LessonSeriesGridProps) {
  const [sport, setSport] = useState<SportFilter>("all");
  const [level, setLevel] = useState<string>("all");
  const [city, setCity] = useState<string>("all");

  const levels = useMemo(() => {
    const set = new Set(
      series
        .map((s) => s.level)
        .filter((l): l is LessonLevel => l !== null),
    );
    return LEVEL_ORDER.filter((l) => set.has(l));
  }, [series]);

  const cities = useMemo(
    () =>
      Array.from(new Set(series.map((s) => s.clubCity))).sort((a, b) =>
        a.localeCompare(b, "nl-BE"),
      ),
    [series],
  );

  const filtered = useMemo(
    () =>
      series.filter((s) => {
        if (sport !== "all" && s.sport !== sport) return false;
        if (level !== "all" && s.level !== level) return false;
        if (city !== "all" && s.clubCity !== city) return false;
        return true;
      }),
    [series, sport, level, city],
  );

  const isFiltered = sport !== "all" || level !== "all" || city !== "all";

  function resetFilters() {
    setSport("all");
    setLevel("all");
    setCity("all");
  }

  return (
    <>
      <div className="rounded-xl border border-rule bg-paper p-4 md:p-5">
        <div className="flex flex-col gap-4 lg:flex-row lg:flex-wrap lg:items-end lg:justify-between">
          <div className="flex flex-col gap-2">
            <Mono className="text-[10px] tracking-[0.18em] text-ink-3">
              SPORT
            </Mono>
            <div
              role="radiogroup"
              aria-label="Filter op sport"
              className="inline-flex rounded-md border border-rule bg-canvas p-1"
            >
              {SPORT_FILTERS.map((option) => {
                const active = option.value === sport;
                return (
                  <button
                    key={option.value}
                    type="button"
                    role="radio"
                    aria-checked={active}
                    onClick={() => setSport(option.value)}
                    className={cn(
                      "inline-flex h-8 items-center rounded px-3 text-sm font-medium transition-colors",
                      active
                        ? "bg-ink text-paper"
                        : "text-ink-2 hover:text-ink",
                    )}
                  >
                    {option.label}
                  </button>
                );
              })}
            </div>
          </div>

          <FilterSelect
            label="NIVEAU"
            value={level}
            onChange={setLevel}
            options={[
              { value: "all", label: "Alle niveaus" },
              ...levels.map((l) => ({ value: l, label: l })),
            ]}
          />

          <FilterSelect
            label="LOCATIE"
            value={city}
            onChange={setCity}
            options={[
              { value: "all", label: "Alle steden" },
              ...cities.map((c) => ({ value: c, label: c })),
            ]}
          />

          <div className="ml-auto flex items-center gap-3">
            {isFiltered ? (
              <button
                type="button"
                onClick={resetFilters}
                className="inline-flex h-8 items-center gap-1.5 rounded-md border border-rule bg-paper px-2.5 text-xs font-medium text-ink-2 transition-colors hover:border-ink/30 hover:text-ink"
              >
                <X className="h-3.5 w-3.5" />
                Wis filters
              </button>
            ) : null}
            <Mono className="text-[11px] tabular-nums text-ink-3">
              {filtered.length} van {series.length} lessen
            </Mono>
          </div>
        </div>
      </div>

      {filtered.length > 0 ? (
        <div className="mt-8 grid gap-5 md:grid-cols-2 lg:grid-cols-3">
          {filtered.map((s) => (
            <LessonSeriesCard key={s.id} series={s} />
          ))}
        </div>
      ) : (
        <div className="mt-8 flex flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-rule bg-paper px-6 py-16 text-center">
          <SearchX className="h-6 w-6 text-ink-3" />
          <p className="text-base font-semibold text-ink">
            Geen lessen gevonden
          </p>
          <p className="max-w-sm text-sm text-ink-2">
            Probeer een andere combinatie van sport, niveau of stad — of wis je
            filters om alle lessen te zien.
          </p>
          <button
            type="button"
            onClick={resetFilters}
            className="mt-1 inline-flex h-9 items-center rounded-md bg-ink px-4 text-sm font-semibold text-paper transition-colors hover:bg-ink/90"
          >
            Wis filters
          </button>
        </div>
      )}
    </>
  );
}

interface FilterSelectProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
}

function FilterSelect({ label, value, onChange, options }: FilterSelectProps) {
  return (
    <label className="flex flex-col gap-2">
      <Mono className="text-[10px] tracking-[0.18em] text-ink-3">{label}</Mono>
      <div className="relative">
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="h-9 w-full appearance-none rounded-md border border-rule bg-canvas pr-8 pl-3 text-sm font-medium text-ink transition-colors hover:border-ink/30 focus:border-ink/40 focus:outline-none lg:min-w-44"
        >
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
        <ChevronDown className="pointer-events-none absolute top-1/2 right-2.5 h-3.5 w-3.5 -translate-y-1/2 text-ink-3" />
      </div>
    </label>
  );
}
