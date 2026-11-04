import { CourtLines } from "@/components/ui/court-lines";
import { Mono } from "@/components/ui/mono";

/** Koptegel voor binnenpagina's: een grote donkergroene kaart met baanlijnen. */
export function BentoPageHero({
  kicker,
  title,
  lead,
  children,
  narrow = false,
  titleClassName = "text-4xl md:text-5xl",
}: {
  kicker: string;
  title: string;
  lead?: string;
  children?: React.ReactNode;
  /** Smallere kaart, voor leesbare pagina's zoals een artikel. */
  narrow?: boolean;
  titleClassName?: string;
}) {
  return (
    <section>
      <div
        className={`mx-auto px-6 pb-4 pt-12 md:pt-16 ${narrow ? "max-w-4xl" : "max-w-6xl"}`}
      >
        <div className="relative overflow-hidden rounded-3xl bg-[#1f3d0e] p-8 text-white md:p-12">
          <CourtLines opacity={0.07} />
          <div className="relative">
            <Mono className="text-[11px] tracking-[0.18em] text-tennis-lime">
              {kicker}
            </Mono>
            <h1
              className={`mt-5 max-w-3xl font-bold leading-[1.05] tracking-tight ${titleClassName}`}
            >
              {title}
            </h1>
            {lead ? (
              <p className="mt-5 max-w-3xl text-lg leading-relaxed text-white/75">
                {lead}
              </p>
            ) : null}
            {children}
          </div>
        </div>
      </div>
    </section>
  );
}
