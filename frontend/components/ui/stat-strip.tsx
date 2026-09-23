import { type CSSProperties } from "react";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";

interface StatItem {
  value: string;
  label: string;
  color?: string;
  /** Verberg deze stat onder lg (mobiel + klein scherm), toon enkel in de volle rij. */
  mobileHidden?: boolean;
}

interface StatStripProps {
  items: StatItem[];
  className?: string;
}

export function StatStrip({ items, className }: StatStripProps) {
  return (
    <div
      className={cn(
        "bg-ink text-white rounded-xl px-5 py-4 grid gap-x-4 gap-y-4 lg:gap-[18px]",
        // Responsive: 2 kolommen op mobiel, 3 vanaf sm, en pas op lg alle stats in
        // één rij (aantal via CSS-var). Voorkomt dat de strip op mobiel buiten beeld valt.
        "[grid-template-columns:repeat(2,minmax(0,1fr))]",
        "sm:[grid-template-columns:repeat(3,minmax(0,1fr))]",
        "lg:[grid-template-columns:var(--stat-cols)]",
        className,
      )}
      style={{ "--stat-cols": `repeat(${items.length}, minmax(0, 1fr))` } as CSSProperties}
    >
      {items.map((item, i) => (
        <div
          key={i}
          // Verticale scheidingslijn enkel op lg (één rij); bij wrap op mobiel/tablet
          // zorgt de grid-gap voor de ruimte.
          className={cn(
            item.mobileHidden && "hidden lg:block",
            i > 0 && "lg:border-l lg:border-white/10 lg:pl-4",
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
