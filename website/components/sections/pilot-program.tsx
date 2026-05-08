import { ArrowRight, Check, Gift, MessageSquare, Tag } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { cn } from "@/lib/utils";
import {
  PILOT,
  PILOT_AVAILABLE_SEATS,
  PILOT_BENEFITS,
  PILOT_CTA,
  PILOT_HEADING,
  PILOT_KICKER,
  PILOT_SUB,
} from "@/content/pilot";

const BENEFIT_ICONS: LucideIcon[] = [Gift, Tag, MessageSquare];

export function PilotProgram() {
  if (PILOT_AVAILABLE_SEATS <= 0) return null;

  return (
    <section id="pilot" className="border-b border-rule bg-canvas">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-28">
        <div className="grid gap-12 lg:grid-cols-[1fr_1.05fr] lg:items-start lg:gap-16">
          <div>
            <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
              {PILOT_KICKER}
            </Mono>
            <h2 className="mt-3 text-3xl font-bold leading-[1.05] tracking-tight md:text-4xl">
              {PILOT_HEADING}
            </h2>
            <p className="mt-5 text-base leading-relaxed text-ink-2">
              {PILOT_SUB}
            </p>

            <SeatCounter
              total={PILOT.totalSeats}
              taken={PILOT.takenSeats}
              className="mt-10"
            />

            <div className="mt-8 flex flex-wrap items-center gap-4">
              <a
                href={PILOT_CTA.href}
                className="inline-flex h-11 items-center gap-2 rounded-md bg-tennis-green px-5 text-sm font-semibold text-paper transition-colors hover:bg-tennis-green/90"
              >
                {PILOT_CTA.label}
                <ArrowRight className="h-4 w-4" />
              </a>
              <span className="text-sm text-ink-3">
                Geen creditcard nodig · plek geldt tot eind seizoen
              </span>
            </div>
          </div>

          <div className="grid gap-3">
            {PILOT_BENEFITS.map((benefit, i) => {
              const Icon = BENEFIT_ICONS[i] ?? Gift;
              return (
                <div
                  key={benefit.title}
                  className="flex items-start gap-4 rounded-xl border border-rule bg-paper p-5 transition-colors hover:border-ink/20"
                >
                  <span className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-tennis-green text-tennis-lime">
                    <Icon className="h-4 w-4" strokeWidth={2.2} />
                  </span>
                  <div>
                    <h3 className="text-base font-bold tracking-tight">
                      {benefit.title}
                    </h3>
                    <p className="mt-1 text-sm leading-relaxed text-ink-2">
                      {benefit.body}
                    </p>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}

function SeatCounter({
  total,
  taken,
  className,
}: {
  total: number;
  taken: number;
  className?: string;
}) {
  const seats = Array.from({ length: total }, (_, i) => i < taken);
  const available = total - taken;

  return (
    <div className={cn(className)}>
      <div className="grid grid-cols-5 gap-2 sm:gap-3">
        {seats.map((isTaken, i) => (
          <div
            key={i}
            className={cn(
              "flex aspect-square flex-col items-center justify-center rounded-lg border-2 px-2",
              isTaken
                ? "border-tennis-green bg-tennis-green text-tennis-lime"
                : "border-dashed border-ink-3/30 bg-paper text-ink-3",
            )}
          >
            {isTaken ? (
              <Check className="h-5 w-5" strokeWidth={3} />
            ) : (
              <Mono className="text-[10px] font-bold tracking-tight text-ink-3/70">
                #{i + 1}
              </Mono>
            )}
            <Mono
              className={cn(
                "mt-1 text-[9px] font-bold tracking-[0.12em]",
                isTaken ? "text-tennis-lime" : "text-ink-3",
              )}
            >
              {isTaken ? "BEZET" : "VRIJ"}
            </Mono>
          </div>
        ))}
      </div>
      <p className="mt-3 text-sm text-ink-2">
        <span className="font-semibold text-ink">
          {taken} van {total} plekken bezet
        </span>{" "}
        · nog {available} {available === 1 ? "plek" : "plekken"} beschikbaar.
      </p>
    </div>
  );
}
