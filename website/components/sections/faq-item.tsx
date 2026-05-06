import { Plus } from "lucide-react";

interface FaqItemProps {
  q: string;
  a: string;
}

export function FaqItem({ q, a }: FaqItemProps) {
  return (
    <details className="group border-b border-rule last:border-b-0">
      <summary className="flex cursor-pointer list-none items-center justify-between gap-4 py-5 text-left">
        <span className="text-base font-semibold tracking-tight text-ink md:text-lg">
          {q}
        </span>
        <Plus
          className="h-5 w-5 flex-shrink-0 text-ink-3 transition-transform duration-200 group-open:rotate-45"
          strokeWidth={2}
        />
      </summary>
      <div className="pb-6 pr-9 text-sm leading-relaxed text-ink-2 md:text-base">
        {a}
      </div>
    </details>
  );
}
