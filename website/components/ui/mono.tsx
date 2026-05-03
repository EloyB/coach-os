import { cn } from "@/lib/utils";

interface MonoProps {
  children: React.ReactNode;
  className?: string;
  as?: "span" | "p" | "div" | "td";
}

export function Mono({ children, className, as: Tag = "span" }: MonoProps) {
  return (
    <Tag
      className={cn(
        "font-mono tabular-nums",
        className,
      )}
    >
      {children}
    </Tag>
  );
}
