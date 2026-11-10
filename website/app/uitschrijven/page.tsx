import Link from "next/link";
import type { Metadata } from "next";
import { LogoMark } from "@/components/site/logo-mark";
import { WaitlistUnsubscribeForm } from "@/components/sections/waitlist-unsubscribe-form";
import { WAITLIST_PRIVACY as copy } from "@/content/waitlist-privacy";

export const metadata: Metadata = {
  title: "Uitschrijven",
  robots: { index: false, follow: false },
  referrer: "no-referrer",
};

export default function UitschrijvenPage() {
  return (
    <main className="flex min-h-svh items-center justify-center bg-white px-6 py-16">
      <div className="w-full max-w-md rounded-3xl bg-[#eef1e0] p-8 text-center sm:p-10">
        <h1 className="text-2xl font-bold tracking-tight">{copy.title}</h1>
        <WaitlistUnsubscribeForm />
        <Link
          href="/"
          className="mt-8 inline-flex h-10 items-center gap-2 rounded-lg bg-ink px-4 text-sm font-semibold text-white transition-colors hover:bg-ink/90"
        >
          <LogoMark size="sm" />
          {copy.home}
        </Link>
      </div>
    </main>
  );
}
