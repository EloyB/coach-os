import { Check } from "lucide-react";
import { Mono } from "@/components/ui/mono";

/** Bento-grid met de belangrijkste features; elke kaart heeft een klein UI-fragment dat deels over de rand valt. */
export function BentoFeatures() {
  return (
    <section id="features" className="scroll-mt-16">
      <div className="mx-auto max-w-6xl px-6 py-20 md:py-24">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            FEATURES
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight text-ink md:text-4xl">
            Alles om je lesseizoen te runnen
          </h2>
          <p className="mt-4 text-base text-ink-2">
            Geen losse tools, geen koppelingen onderhouden, geen Excel meer.
          </p>
        </div>

        <div className="mt-12 grid gap-4 lg:grid-cols-6 lg:grid-rows-[repeat(3,minmax(15rem,auto))]">
          <PlanningCard />
          <CampCard />
          <PaymentCard />
          <SignupCard />
          <TrainerCard />
          <FormCard />
        </div>
      </div>
    </section>
  );
}

const CARD = "relative overflow-hidden rounded-3xl p-7 md:p-8";
const TITLE = "font-bold tracking-tight";

function CardText({
  title,
  body,
  dark,
  className = "",
}: {
  title: string;
  body: string;
  dark?: boolean;
  className?: string;
}) {
  return (
    <div className={`relative z-10 ${className}`}>
      <h3
        className={`${TITLE} text-2xl leading-[1.1] md:text-3xl ${dark ? "text-white" : "text-ink"}`}
      >
        {title}
      </h3>
      <p
        className={`mt-3 text-sm leading-relaxed ${dark ? "text-white/70" : "text-ink-2"}`}
      >
        {body}
      </p>
    </div>
  );
}

function PlanningCard() {
  const slots = [
    { time: "18:00", name: "Beginners", people: 2, tone: "bg-tennis-lime" },
    { time: "14:00", name: "Mini's", people: 1, tone: "bg-white" },
    { time: "19:00", name: "Intermediate", people: 2, tone: "bg-white" },
    { time: "16:00", name: "Gevorderd", people: 2, tone: "bg-tennis-lime" },
  ];
  return (
    <article
      className={`${CARD} min-h-[28rem] bg-[#1f3d0e] lg:col-span-2 lg:row-span-2`}
    >
      <CardText
        dark
        title="Automatisch plannen"
        body="Eén klik en iedereen heeft een tijdslot, op basis van niveau, voorkeur en groepsverband."
      />
      <div
        aria-hidden
        className="absolute inset-x-6 bottom-6 rounded-2xl bg-[#e9f3d3] p-4 shadow-xl"
      >
        <div className="flex items-center justify-between">
          <Mono className="text-[10px] tracking-[0.14em] text-ink-3">
            PLANNING · WEEK 23
          </Mono>
          <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-700">
            Concept
          </span>
        </div>
        <div className="mt-3 grid grid-cols-2 gap-2">
          {slots.map((slot) => (
            <div key={slot.name} className={`rounded-xl p-3 ${slot.tone}`}>
              <div className="flex justify-between">
                <Mono className="text-[11px] font-semibold text-ink">
                  {slot.time}
                </Mono>
                <Mono className="text-[10px] text-ink-3">{slot.people}/4</Mono>
              </div>
              <p className="mt-0.5 text-xs text-ink-2">{slot.name}</p>
              <div className="mt-2 flex -space-x-1.5">
                {Array.from({ length: slot.people }).map((_, i) => (
                  <span
                    key={i}
                    className={`h-5 w-5 rounded-full border-2 border-white ${["bg-violet-500", "bg-sky-500", "bg-rose-500"][i % 3]}`}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
        <div className="mt-3 flex items-center justify-between rounded-xl bg-ink px-4 py-2.5">
          <span className="text-xs text-white/70">
            Voorkeuren matchen en groepen vormen
          </span>
          <span className="rounded-lg bg-tennis-lime px-3 py-1 text-xs font-semibold text-ink">
            Plannen
          </span>
        </div>
      </div>
    </article>
  );
}

function CampCard() {
  const days = ["MA", "DI", "WO", "DO", "VR"];
  return (
    <article className={`${CARD} min-h-[15rem] bg-[#eef1e0] lg:col-span-4`}>
      <CardText
        title="Kampen en stages"
        body="Hele week, enkele dagen of halve dagen. Met vaste pakketten en een limiet per dagdeel."
        className="max-w-xs"
      />
      <div
        aria-hidden
        className="absolute -bottom-4 right-6 hidden w-[22rem] rounded-2xl bg-white p-4 shadow-xl sm:block md:right-10"
      >
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold">Zomerkamp week 1</span>
          <span className="rounded-full bg-tennis-lime/70 px-2 py-0.5 text-[10px] font-semibold text-tennis-green">
            Hele week
          </span>
        </div>
        <div className="mt-3 grid grid-cols-5 gap-1.5">
          {days.map((day) => (
            <div key={day} className="space-y-1.5">
              <Mono className="block text-center text-[9px] text-ink-3">
                {day}
              </Mono>
              <div className="h-6 rounded-md bg-tennis-green" />
              <div className="h-6 rounded-md bg-tennis-green/60" />
            </div>
          ))}
        </div>
      </div>
    </article>
  );
}

function PaymentCard() {
  return (
    <article className={`${CARD} min-h-[15rem] bg-[#2d5a16] lg:col-span-2`}>
      <CardText
        dark
        title="Betalingen"
        body="Online betalen via Mollie, of cash registreren met één klik."
        className="max-w-[14rem]"
      />
      <div
        aria-hidden
        className="absolute -bottom-3 right-4 w-44 rotate-[-6deg] rounded-2xl bg-white p-4 shadow-xl"
      >
        <Mono className="text-[9px] tracking-[0.14em] text-ink-3">
          INSCHRIJVING
        </Mono>
        <p className="mt-1 text-sm font-semibold">Emma V.</p>
        <p className="text-[11px] text-ink-3">Bancontact</p>
        <span className="mt-2 inline-block rounded-full bg-tennis-lime px-2.5 py-0.5 text-[10px] font-semibold text-tennis-green">
          Betaald
        </span>
      </div>
    </article>
  );
}

function SignupCard() {
  return (
    <article className={`${CARD} min-h-[15rem] bg-[#eef1e0] lg:col-span-2`}>
      <CardText
        title="Inschrijven zonder account"
        body="Eén publieke link per lessenreeks. Geen wachtwoord, geen drempel."
      />
      <div
        aria-hidden
        className="absolute bottom-7 left-7 flex items-center gap-2 rounded-full bg-white px-4 py-2 shadow-md"
      >
        <span className="h-2 w-2 rounded-full bg-tennis-green" />
        <span className="text-xs text-ink-2">Publieke inschrijflink</span>
        <span className="rounded-full bg-tennis-lime px-2 py-0.5 text-[10px] font-semibold text-ink">
          Kopieer
        </span>
      </div>
    </article>
  );
}

function TrainerCard() {
  const trainers = [
    { initials: "JV", color: "bg-violet-500" },
    { initials: "MV", color: "bg-sky-500" },
    { initials: "TJ", color: "bg-rose-500" },
    { initials: "AV", color: "bg-amber-500" },
  ];
  return (
    <article className={`${CARD} min-h-[15rem] bg-[#3b6b1f] lg:col-span-3`}>
      <CardText
        dark
        title="Trainers beheren"
        body="Nodig trainers uit en leg vast wanneer ze beschikbaar zijn, per club en per dag."
        className="max-w-xs"
      />
      <div aria-hidden className="absolute bottom-6 left-7 flex -space-x-2">
        {trainers.map((t) => (
          <span
            key={t.initials}
            className={`flex h-10 w-10 items-center justify-center rounded-full border-2 border-[#3b6b1f] text-xs font-bold text-white ${t.color}`}
          >
            {t.initials}
          </span>
        ))}
      </div>
    </article>
  );
}

function FormCard() {
  return (
    <article className={`${CARD} min-h-[15rem] bg-[#eef1e0] lg:col-span-3`}>
      <CardText
        title="Je eigen inschrijfformulier"
        body="Voeg per lessenreeks vragen toe en verzamel precies wat je nodig hebt."
        className="max-w-[16rem]"
      />
      <div
        aria-hidden
        className="absolute -bottom-4 right-6 hidden w-52 space-y-2 rounded-2xl bg-white p-4 shadow-xl sm:block"
      >
        {["Naam speler", "Speelniveau", "Voorkeurstijd"].map((label) => (
          <div key={label}>
            <p className="text-[10px] text-ink-3">{label}</p>
            <div className="mt-0.5 flex h-6 items-center justify-between rounded-md border border-rule px-2">
              <span className="h-1.5 w-16 rounded bg-ink/10" />
              {label === "Voorkeurstijd" ? (
                <Check className="h-3 w-3 text-tennis-green" strokeWidth={3} />
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </article>
  );
}
