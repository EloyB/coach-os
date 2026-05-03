import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";

interface StatItem {
  value: string;
  label: string;
  color?: string;
}

interface StatStripProps {
  items: StatItem[];
  className?: string;
}

export function StatStrip({ items, className }: StatStripProps) {
  return (
    <div
      className={cn(
        "bg-ink text-white rounded-xl px-5 py-4 grid gap-[18px]",
        className,
      )}
      style={{ gridTemplateColumns: `repeat(${items.length}, 1fr)` }}
    >
      {items.map((item, i) => (
        <div
          key={i}
          className={cn(
            i > 0 && "border-l border-white/10 pl-4",
          )}
        >
          <Mono
            className={cn(
              "text-[22px] font-extrabold leading-none",
              item.color ?? "text-white",
            )}
          >
            {item.value}
          </Mono>
          <Mono className="text-[10px] text-[#a8a195] mt-1 block tracking-[0.05em]">
            {item.label}
          </Mono>
        </div>
      ))}
    </div>
  );
}
