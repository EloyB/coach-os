import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";

interface LogoMarkProps {
  className?: string;
  size?: "sm" | "md";
}

export function LogoMark({ className, size = "md" }: LogoMarkProps) {
  const dim = size === "sm" ? "h-7 w-7 text-sm" : "h-9 w-9 text-base";
  return (
    <span
      className={cn(
        "inline-flex items-center justify-center rounded-md bg-tennis-lime",
        dim,
        className,
      )}
      aria-label="CoachOS"
    >
      <Mono className="text-ink font-extrabold">c/</Mono>
    </span>
  );
}
