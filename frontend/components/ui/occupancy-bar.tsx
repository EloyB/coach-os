import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";

interface OccupancyBarProps {
  filled: number;
  capacity: number;
  showLabel?: boolean;
  className?: string;
}

export function OccupancyBar({
  filled,
  capacity,
  showLabel = true,
  className,
}: OccupancyBarProps) {
  const pct = capacity > 0 ? Math.round((filled / capacity) * 100) : 0;
  const isLow = pct < 60;

  return (
    <div className={cn("space-y-1", className)}>
      {showLabel && (
        <div className="flex items-baseline gap-1.5">
          <Mono className="font-bold text-ink">
            {filled}
            <span className="text-ink-3 font-normal">/{capacity}</span>
          </Mono>
          <span
            className={cn(
              "text-[10.5px]",
              isLow ? "text-warn" : "text-ink-3",
            )}
          >
            {pct}%
          </span>
        </div>
      )}
      <div className="h-1 rounded-full bg-rule overflow-hidden">
        <div
          className={cn(
            "h-full rounded-full transition-all",
            isLow ? "bg-warn" : "bg-tennis-green",
          )}
          style={{ width: `${Math.min(pct, 100)}%` }}
        />
      </div>
    </div>
  );
}
