import Link from "next/link";
import { LogoMark } from "@/components/site/logo-mark";
import { Mono } from "@/components/ui/mono";
import { FOOTER } from "@/content/footer";
import { SITE } from "@/content/meta";

export function SiteFooter() {
  return (
    <footer className="border-t border-rule bg-canvas">
      <div className="mx-auto grid max-w-6xl gap-10 px-6 py-14 md:grid-cols-[1.4fr_1fr_1fr_1fr]">
        <div>
          <div className="flex items-center gap-2.5">
            <LogoMark size="sm" />
            <span className="font-semibold tracking-tight">CoachOS</span>
          </div>
          <p className="mt-3 max-w-xs text-sm text-ink-2">{FOOTER.tagline}</p>
          <Mono className="mt-4 block text-[11px] tracking-[0.08em] text-ink-3">
            {FOOTER.languages.toUpperCase()}
          </Mono>
        </div>

        {FOOTER.columns.map((column) => (
          <div key={column.heading}>
            <Mono className="mb-3 block text-[11px] tracking-[0.08em] text-ink-3">
              {column.heading.toUpperCase()}
            </Mono>
            <ul className="space-y-2 text-sm">
              {column.links.map((link) => (
                <li key={`${column.heading}-${link.label}`}>
                  <Link
                    href={link.href}
                    className="text-ink-2 transition-colors hover:text-ink"
                  >
                    {link.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      <div className="border-t border-rule">
        <div className="mx-auto flex max-w-6xl flex-col gap-2 px-6 py-5 text-xs text-ink-3 sm:flex-row sm:items-center sm:justify-between">
          <Mono>{FOOTER.copyright}</Mono>
          <Mono>{SITE.contactEmail}</Mono>
        </div>
      </div>
    </footer>
  );
}
