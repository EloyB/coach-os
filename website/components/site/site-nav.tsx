import Link from "next/link";
import { LogoMark } from "@/components/site/logo-mark";
import { NAV_LINKS, NAV_CTA } from "@/content/nav";
import { SITE } from "@/content/meta";

export function SiteNav() {
  return (
    <header className="sticky top-0 z-40 border-b border-rule bg-paper/85 backdrop-blur supports-[backdrop-filter]:bg-paper/70">
      <div className="mx-auto flex h-16 max-w-6xl items-center gap-8 px-6">
        <Link
          href="/"
          className="flex items-center gap-2.5 text-ink"
          aria-label="CoachOS, naar boven"
        >
          <LogoMark size="sm" />
          <span className="font-semibold tracking-tight">CoachOS</span>
        </Link>

        <nav className="hidden items-center gap-6 text-sm text-ink-2 md:flex">
          {NAV_LINKS.map((link) => (
            <a
              key={link.href}
              href={link.href}
              className="transition-colors hover:text-ink"
            >
              {link.label}
            </a>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-3">
          <Link
            href={`${SITE.appUrl}/login`}
            className="hidden text-sm text-ink-2 transition-colors hover:text-ink sm:inline-flex"
          >
            {NAV_CTA.login.label}
          </Link>
          <a
            href={NAV_CTA.primary.href}
            className="inline-flex h-9 items-center rounded-md bg-tennis-lime px-3.5 text-sm font-semibold text-ink transition-colors hover:bg-tennis-lime/90"
          >
            {NAV_CTA.primary.label}
          </a>
        </div>
      </div>
    </header>
  );
}
