import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { LogoMark } from "@/components/site/logo-mark";
import { NAV_LINKS, NAV_CTA } from "@/content/nav";
import { SITE } from "@/content/meta";

/** Navigatie met een zwevende pil in het midden, over de hero heen. */
export function FloatingNav() {
  return (
    <header className="absolute inset-x-0 top-0 z-40">
      <div className="mx-auto flex h-20 max-w-6xl items-center justify-between px-6">
        <Link
          href="/"
          className="flex items-center gap-2.5 text-ink"
          aria-label="CoachOS, naar boven"
        >
          <LogoMark size="sm" />
          <span className="font-semibold tracking-tight">CoachOS</span>
        </Link>

        <nav className="hidden items-center gap-1 rounded-xl border border-white/70 bg-white/60 p-1.5 text-sm text-ink-2 shadow-[0_4px_20px_-8px_rgba(22,21,19,0.15)] backdrop-blur md:flex">
          {NAV_LINKS.map((link) => (
            <a
              key={link.href}
              href={link.href.replace(/^\//, "")}
              className="rounded-lg px-4 py-2 transition-colors hover:bg-white hover:text-ink"
            >
              {link.label}
            </a>
          ))}
        </nav>

        <div className="flex items-center gap-4">
          <Link
            href={`${SITE.appUrl}/login`}
            className="hidden text-sm text-ink-2 transition-colors hover:text-ink sm:inline"
          >
            {NAV_CTA.login.label}
          </Link>
          <a
            href={NAV_CTA.primary.href}
            className="inline-flex h-10 items-center gap-2 rounded-lg bg-ink px-4 text-sm font-medium text-white ring-2 ring-tennis-lime/60 transition-colors hover:bg-ink/90"
          >
            {NAV_CTA.primary.label}
            <ArrowRight className="h-4 w-4" />
          </a>
        </div>
      </div>
    </header>
  );
}
