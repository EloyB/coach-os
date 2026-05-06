import Link from "next/link";
import { Check } from "lucide-react";
import type { Metadata } from "next";
import { LogoMark } from "@/components/site/logo-mark";
import { Mono } from "@/components/ui/mono";
import { CTA_SECTION } from "@/content/cta";

export const metadata: Metadata = {
  title: "Bedankt",
  robots: { index: false },
};

interface PageProps {
  searchParams: Promise<{ type?: string }>;
}

export default async function BedanktPage({ searchParams }: PageProps) {
  const { type } = await searchParams;
  const isWaitlist = type === "waitlist";
  const heading = isWaitlist
    ? CTA_SECTION.waitlist.success
    : CTA_SECTION.contact.success;
  const body = isWaitlist
    ? "We nemen contact op om een tijdstip te prikken voor je demo — meestal binnen één werkdag."
    : "Een van ons leest je bericht en antwoordt binnen één werkdag.";

  return (
    <main className="flex min-h-svh items-center justify-center bg-canvas px-6 py-16">
      <div className="w-full max-w-md rounded-2xl border border-rule bg-paper p-10 text-center shadow-sm">
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-tennis-lime/30">
          <Check className="h-6 w-6 text-tennis-green" strokeWidth={3} />
        </div>
        <Mono className="mt-6 block text-[11px] tracking-[0.18em] text-ink-3">
          {isWaitlist ? "DEMO AANGEVRAAGD" : "BERICHT ONTVANGEN"}
        </Mono>
        <h1 className="mt-3 text-2xl font-bold tracking-tight">{heading}</h1>
        <p className="mt-3 text-sm leading-relaxed text-ink-2">{body}</p>

        <Link
          href="/"
          className="mt-8 inline-flex h-10 items-center gap-2 rounded-md bg-ink px-4 text-sm font-semibold text-white transition-colors hover:bg-ink/90"
        >
          <LogoMark size="sm" />
          Terug naar home
        </Link>
      </div>
    </main>
  );
}
