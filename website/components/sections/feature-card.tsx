import type { LucideIcon } from "lucide-react";

interface FeatureCardProps {
  icon: LucideIcon;
  title: string;
  body: string;
}

export function FeatureCard({ icon: Icon, title, body }: FeatureCardProps) {
  return (
    <div className="rounded-xl border border-rule bg-paper p-6 transition-colors hover:border-ink/20">
      <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-tennis-green text-tennis-lime">
        <Icon className="h-5 w-5" strokeWidth={2.2} />
      </div>
      <h3 className="mt-5 text-base font-bold tracking-tight">{title}</h3>
      <p className="mt-2 text-sm leading-relaxed text-ink-2">{body}</p>
    </div>
  );
}
