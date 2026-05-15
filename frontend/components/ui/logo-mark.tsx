import { cn } from "@/lib/utils";
import { LogoMarkSvg } from "@/components/ui/logo-mark-svg";

interface LogoMarkProps {
  variant?: "lime" | "green";
  className?: string;
  markPx?: number;
  markStroke?: number;
}

export function LogoMark({
  variant = "lime",
  className,
  markPx = 20,
  markStroke = 10,
}: LogoMarkProps) {
  const isLime = variant === "lime";
  return (
    <span
      className={cn(
        "inline-flex h-7 w-7 items-center justify-center rounded-md",
        isLime ? "bg-tennis-lime" : "bg-tennis-green",
        className,
      )}
      aria-label="CoachOS"
    >
      <LogoMarkSvg
        size={markPx}
        strokeWidth={markStroke}
        strokeColor={isLime ? "#161513" : "#D0FF14"}
      />
    </span>
  );
}
