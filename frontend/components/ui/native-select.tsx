import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

interface NativeSelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  children: React.ReactNode;
  /** Compact variant for inline use (calendar header, etc.) */
  variant?: "default" | "compact";
}

export function NativeSelect({
  children,
  className,
  variant = "default",
  ...props
}: NativeSelectProps) {
  return (
    <div className="relative inline-flex">
      <select
        className={cn(
          "appearance-none bg-white border border-gray-200 text-ink cursor-pointer transition-colors",
          "focus:outline-none focus:ring-1 focus:ring-tennis-green focus:border-tennis-green",
          "hover:border-tennis-green/50",
          variant === "compact"
            ? "text-sm font-semibold pl-2 pr-6 py-1 rounded-md border-transparent hover:bg-canvas"
            : "text-[12.5px] pl-3 pr-8 py-2 rounded-lg w-full",
          className,
        )}
        {...props}
      >
        {children}
      </select>
      <ChevronDown
        className={cn(
          "absolute right-1.5 top-1/2 -translate-y-1/2 pointer-events-none text-ink-3",
          variant === "compact" ? "size-3" : "size-3.5",
        )}
      />
    </div>
  );
}
