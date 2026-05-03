import type { Metadata } from "next";
import Link from "next/link";
import { SiteNav } from "@/components/site/site-nav";
import { SiteFooter } from "@/components/site/site-footer";
import { Mono } from "@/components/ui/mono";

export const metadata: Metadata = {
  title: "Voorwaarden",
  description: "Algemene voorwaarden van CoachOS.",
};

export default function VoorwaardenPage() {
  return (
    <>
      <SiteNav />
      <main>
        <article className="mx-auto max-w-3xl px-6 py-20 md:py-28">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            JURIDISCH
          </Mono>
          <h1 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            Algemene voorwaarden
          </h1>
          <p className="mt-4 text-sm text-ink-3">
            Laatst bijgewerkt: nog te bepalen.
          </p>

          <div className="mt-10 space-y-6 text-base leading-relaxed text-ink-2">
            <p>
              CoachOS is in pre-launch. De definitieve algemene voorwaarden
              worden gepubliceerd vóór algemene beschikbaarheid.
            </p>
            <p>
              Specifieke vragen over een toekomstige overeenkomst?{" "}
              <Link
                href="/#contact"
                className="font-semibold text-ink underline underline-offset-4"
              >
                Neem contact op
              </Link>
              .
            </p>
          </div>
        </article>
      </main>
      <SiteFooter />
    </>
  );
}
