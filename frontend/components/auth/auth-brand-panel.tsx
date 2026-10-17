import { CourtLines } from "@/components/ui/court-lines";
import { LogoMark } from "@/components/ui/logo-mark";
import { Mono } from "@/components/ui/mono";
import { SlashLabel } from "@/components/ui/slash-label";

interface AuthBrandPanelProps {
  version: string;
  eyebrow: string;
  headline: [string, string, string];
  tagline: string;
  /** Onderste blok van het paneel (bv. een lijst met voordelen). */
  children?: React.ReactNode;
}

/** Linker brandingpaneel van de auth-pagina's — zelfde opbouw als de login-pagina. */
export function AuthBrandPanel({
  version,
  eyebrow,
  headline,
  tagline,
  children,
}: AuthBrandPanelProps) {
  return (
    <div className="hidden lg:flex lg:w-[44%] bg-ink relative overflow-hidden flex-col justify-between p-10">
      <CourtLines opacity={0.05} />

      {/* Subtle green glow */}
      <div
        className="absolute -right-[120px] -top-[80px] w-[320px] h-[320px] rounded-full blur-[10px]"
        style={{ background: "radial-gradient(closest-side, rgba(45,80,22,.55), transparent)" }}
      />

      <div className="relative z-10 flex items-center gap-2.5">
        <LogoMark className="h-[30px] w-[30px]" markPx={22} />
        <span className="text-white font-bold text-base">CoachOS</span>
        <Mono className="ml-auto text-[10.5px] text-[#8a8377]">{version}</Mono>
      </div>

      <div className="relative z-10">
        <SlashLabel className="text-tennis-lime mb-2.5">{eyebrow}</SlashLabel>
        <h1 className="text-[34px] font-extrabold text-white leading-[1.05] tracking-tight mb-3">
          {headline[0]}
          <br />
          {headline[1]}
          <br />
          <span className="text-tennis-lime">{headline[2]}</span>
        </h1>
        <p className="text-[13px] text-[#a8a195] max-w-[300px] leading-relaxed">{tagline}</p>
      </div>

      <div className="relative z-10">{children}</div>
    </div>
  );
}
