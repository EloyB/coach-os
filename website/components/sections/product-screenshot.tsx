import Image from "next/image";

/** Dashboard-screenshot in een laptopframe, direct onder de hero. */
export function ProductScreenshot() {
  return (
    <section className="overflow-hidden border-b border-rule bg-canvas">
      <div className="mx-auto max-w-5xl px-6 pb-16 pt-12 md:pb-24 md:pt-16">
        <Laptop>
          <Image
            src="/screenshots/dashboard.png"
            alt="Het CoachOS-dashboard met de lessen van vandaag en een maandkalender waarin lessenreeksen en kampen staan."
            width={2400}
            height={1359}
            quality={90}
            sizes="(min-width: 1024px) 960px, 100vw"
            className="block h-auto w-full"
          />
        </Laptop>
      </div>
    </section>
  );
}

function Laptop({ children }: { children: React.ReactNode }) {
  return (
    <div className="mx-auto w-full">
      {/* Scherm met bezel en cameradotje */}
      <div className="relative rounded-t-[1.25rem] border border-ink/80 bg-ink p-[0.6rem] pt-5 shadow-[0_30px_60px_-20px_rgba(22,21,19,0.35)] md:p-3 md:pt-6">
        <span className="absolute left-1/2 top-1.5 h-1 w-1 -translate-x-1/2 rounded-full bg-white/25 md:top-2" />
        <div className="overflow-hidden rounded-md bg-paper">{children}</div>
      </div>
      {/* Onderkant van de laptop, iets breder dan het scherm */}
      <div className="relative mx-auto -mt-px h-3 w-[108%] -translate-x-[3.7%] rounded-b-2xl bg-gradient-to-b from-[#d9d7d2] to-[#b9b6af] md:h-4">
        <span className="absolute left-1/2 top-0 h-1.5 w-1/5 -translate-x-1/2 rounded-b-lg bg-[#a7a49d]" />
      </div>
    </div>
  );
}
