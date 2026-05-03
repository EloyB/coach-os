import { Mono } from "@/components/ui/mono";

interface StepCardProps {
  num: string;
  title: string;
  body: string;
}

export function StepCard({ num, title, body }: StepCardProps) {
  return (
    <div className="relative rounded-xl border border-rule bg-paper p-7">
      <Mono className="text-sm font-bold tracking-[0.05em] text-tennis-green">
        {num}
      </Mono>
      <h3 className="mt-4 text-lg font-bold tracking-tight">{title}</h3>
      <p className="mt-2 text-sm leading-relaxed text-ink-2">{body}</p>
    </div>
  );
}
