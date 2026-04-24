import { cn } from "@/lib/utils";

interface SlashLabelProps {
  children: React.ReactNode;
  className?: string;
}

export function SlashLabel({ children, className }: SlashLabelProps) {
  return (
    <p
      className={cn(
        "font-mono text-[10.5px] text-ink-3 uppercase tracking-[0.1em] m-0",
        className,
      )}
    >
      {children}
    </p>
  );
}
