import { Check } from "lucide-react";
import { Mono } from "@/components/ui/mono";
import { STEPS, STEPS_HEADING, STEPS_SUB } from "@/content/steps";

/** "Hoe het werkt" als bento: drie kaarten, elk met een UI-fragment van die stap. */
export function BentoSteps() {
  const fragments = [
    <SeriesFragment key="1" />,
    <SignupFragment key="2" />,
    <ConfirmFragment key="3" />,
  ];
  const tones = [
    {
      card: "bg-[#eef1e0] text-ink",
      body: "text-ink-2",
      num: "bg-tennis-green text-tennis-lime",
    },
    {
      card: "bg-[#2d5a16] text-white",
      body: "text-white/70",
      num: "bg-tennis-lime text-tennis-green",
    },
    {
      card: "bg-[#1f3d0e] text-white",
      body: "text-white/70",
      num: "bg-tennis-lime text-tennis-green",
    },
  ];
  return (
    <section id="hoe-het-werkt" className="scroll-mt-16">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            HOE HET WERKT
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {STEPS_HEADING}
          </h2>
          <p className="mt-3 text-base text-ink-2">{STEPS_SUB}</p>
        </div>

        <ol className="mt-12 grid gap-4 lg:grid-cols-3">
          {STEPS.map((step, i) => (
            <li
              key={step.num}
              className={`relative min-h-[34rem] overflow-hidden rounded-3xl p-7 md:p-8 ${tones[i].card}`}
            >
              <span
                className={`flex h-10 w-10 items-center justify-center rounded-full text-base font-bold ${tones[i].num}`}
              >
                {i + 1}
              </span>
              <h3 className="mt-6 text-2xl font-bold leading-[1.1] tracking-tight md:text-3xl">
                {step.title}
              </h3>
              <p className={`mt-3 text-sm leading-relaxed ${tones[i].body}`}>
                {step.body}
              </p>
              <div aria-hidden className="absolute inset-x-6 -bottom-4">
                {fragments[i]}
              </div>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}

const PANEL = "rounded-2xl bg-white p-4 text-ink shadow-xl";

function SeriesFragment() {
  const rows = [
    ["Naam", "Voorjaarslessen"],
    ["Dag en uur", "Ma 18:00"],
    ["Capaciteit", "4 spelers"],
  ];
  return (
    <div className={PANEL}>
      <Mono className="text-[10px] tracking-[0.14em] text-ink-3">
        NIEUWE LESSENREEKS
      </Mono>
      <div className="mt-3 space-y-2">
        {rows.map(([label, value]) => (
          <div key={label}>
            <p className="text-[10px] text-ink-3">{label}</p>
            <div className="mt-0.5 rounded-md border border-rule px-2 py-1.5 text-xs">
              {value}
            </div>
          </div>
        ))}
      </div>
      <div className="mt-3 h-8" />
    </div>
  );
}

function SignupFragment() {
  const people = [
    { name: "Emma V.", slot: "Ma 18:00", color: "bg-violet-500" },
    { name: "Lucas D.", slot: "Wo 14:00", color: "bg-sky-500" },
    { name: "Noor P.", slot: "Za 10:00", color: "bg-rose-500" },
  ];
  return (
    <div className={PANEL}>
      <div className="flex items-center justify-between">
        <Mono className="text-[10px] tracking-[0.14em] text-ink-3">
          INSCHRIJVINGEN
        </Mono>
        <span className="rounded-full bg-tennis-lime px-2 py-0.5 text-[10px] font-semibold text-tennis-green">
          Nieuw
        </span>
      </div>
      <div className="mt-3 divide-y divide-rule">
        {people.map((p) => (
          <div key={p.name} className="flex items-center gap-2.5 py-2">
            <span className={`h-6 w-6 rounded-full ${p.color}`} />
            <span className="text-xs font-medium">{p.name}</span>
            <span className="ml-auto text-[11px] text-ink-3">
              voorkeur {p.slot}
            </span>
          </div>
        ))}
      </div>
      <div className="mt-1 h-8" />
    </div>
  );
}

function ConfirmFragment() {
  return (
    <div className={PANEL}>
      <div className="flex items-center justify-between">
        <Mono className="text-[10px] tracking-[0.14em] text-ink-3">
          PLANNING · WEEK 23
        </Mono>
        <span className="rounded-full bg-tennis-lime px-2 py-0.5 text-[10px] font-semibold text-tennis-green">
          Bevestigd
        </span>
      </div>
      <div className="mt-3 grid grid-cols-2 gap-2">
        {[
          ["18:00", "Beginners"],
          ["14:00", "Mini's"],
        ].map(([time, name]) => (
          <div key={name} className="rounded-xl bg-[#f3f2ee] p-3">
            <Mono className="text-[11px] font-semibold">{time}</Mono>
            <p className="text-xs text-ink-2">{name}</p>
          </div>
        ))}
      </div>
      <div className="mt-3 flex items-center gap-2 rounded-xl bg-[#eef1e0] px-3 py-2.5 text-xs text-tennis-green">
        <Check className="h-4 w-4" strokeWidth={3} />8 spelers hebben hun
        lesmoment ontvangen
      </div>
      <div className="mt-1 h-6" />
    </div>
  );
}
