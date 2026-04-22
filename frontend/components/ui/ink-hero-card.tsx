import { cn } from "@/lib/utils";
import { CourtLines } from "@/components/ui/court-lines";

interface InkHeroCardProps {
  children: React.ReactNode;
  className?: string;
  showCourtLines?: boolean;
  courtLinesOpacity?: number;
}

export function InkHeroCard({
  children,
  className,
  showCourtLines = false,
  courtLinesOpacity = 0.08,
}: InkHeroCardProps) {
  return (
    <div
      className={cn(
        "bg-ink text-white rounded-xl relative overflow-hidden",
        className,
      )}
    >
      {showCourtLines && <CourtLines opacity={courtLinesOpacity} />}
      <div className="relative">{children}</div>
    </div>
  );
}
