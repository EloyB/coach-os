import { ArrowRight } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import {
  PILOT_AVAILABLE_SEATS,
  PILOT_BANNER,
} from "@/content/pilot";

export function PilotBanner() {
  if (PILOT_AVAILABLE_SEATS <= 0) return null;

  return (
    <a
      href={PILOT_BANNER.href}
      className="group block bg-tennis-green text-paper transition-colors hover:bg-tennis-green/95"
    >
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-center gap-x-3 gap-y-1 px-6 py-2 text-center text-[12px]">
        <Mono className="inline-flex items-center gap-1.5 text-tennis-lime">
          <span className="inline-block h-1.5 w-1.5 rounded-full bg-tennis-lime" />
          {PILOT_BANNER.prefix.toUpperCase()}
        </Mono>
        <span className="font-semibold">
          {PILOT_BANNER.body(PILOT_AVAILABLE_SEATS)}
        </span>
        <span className="hidden text-paper/70 sm:inline">·</span>
        <span className="text-paper/85">{PILOT_BANNER.benefit}</span>
        <span className="inline-flex items-center gap-1 font-semibold text-tennis-lime">
          {PILOT_BANNER.ctaLabel}
          <ArrowRight className="h-3 w-3 transition-transform group-hover:translate-x-0.5" />
        </span>
      </div>
    </a>
  );
}
