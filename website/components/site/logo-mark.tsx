import { cn } from "@/lib/utils";
import { LogoMarkSvg } from "@/components/site/logo-mark-svg";

interface LogoMarkProps {
  className?: string;
  size?: "sm" | "md";
}

export function LogoMark({ className, size = "md" }: LogoMarkProps) {
  const tileClass = size === "sm" ? "h-7 w-7" : "h-9 w-9";
  // Bump stroke at small browser sizes so the seam stays crisp on
  // non-retina displays. Source SVG uses 7 in a 108-unit box.
  const markSize = size === "sm" ? 20 : 26;
  const markStroke = size === "sm" ? 10 : 9;

  return (
    <span
      className={cn(
        "inline-flex items-center justify-center rounded-md bg-tennis-lime",
        tileClass,
        className,
      )}
      aria-label="CoachOS"
    >
      <LogoMarkSvg size={markSize} strokeWidth={markStroke} />
    </span>
  );
}
