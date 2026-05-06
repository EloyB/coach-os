"use client";

import { useState } from "react";
import { Mono } from "@/components/ui/mono";
import { ContactForm } from "@/components/sections/contact-form";
import { WaitlistForm } from "@/components/sections/waitlist-form";
import { CTA_SECTION } from "@/content/cta";
import { PILOT, PILOT_AVAILABLE_SEATS } from "@/content/pilot";
import { cn } from "@/lib/utils";

type Tab = "waitlist" | "contact";

export function FinalCta() {
  const [tab, setTab] = useState<Tab>("waitlist");

  const active = tab === "waitlist" ? CTA_SECTION.waitlist : CTA_SECTION.contact;

  return (
    <section id="contact" className="border-b border-rule">
      <div className="mx-auto max-w-5xl px-6 py-20 md:py-28">
        <div className="max-w-2xl">
          <Mono className="text-[11px] tracking-[0.18em] text-ink-3">
            START
          </Mono>
          <h2 className="mt-3 text-3xl font-bold tracking-tight md:text-4xl">
            {CTA_SECTION.heading}
          </h2>
          <p className="mt-3 text-base text-ink-2">{CTA_SECTION.sub}</p>
          {PILOT_AVAILABLE_SEATS > 0 ? (
            <p className="mt-4 inline-flex items-center gap-2 rounded-full border border-tennis-green/20 bg-tennis-green/5 px-3 py-1.5 text-xs font-semibold text-tennis-green">
              <span className="inline-block h-1.5 w-1.5 rounded-full bg-tennis-green" />
              Pilot-fase · nog {PILOT_AVAILABLE_SEATS} van {PILOT.totalSeats}{" "}
              plekken — gratis tijdens pilot, lifetime korting
            </p>
          ) : null}
        </div>

        <div className="mt-10 overflow-hidden rounded-2xl border border-rule bg-paper">
          <div
            role="tablist"
            aria-label="Inschrijven of contact"
            className="flex border-b border-rule bg-canvas/50"
          >
            <TabButton
              label={CTA_SECTION.tabs.waitlist}
              active={tab === "waitlist"}
              onClick={() => setTab("waitlist")}
            />
            <TabButton
              label={CTA_SECTION.tabs.contact}
              active={tab === "contact"}
              onClick={() => setTab("contact")}
            />
          </div>

          <div className="grid gap-10 p-8 md:grid-cols-[1fr_1.2fr] md:p-12">
            <div>
              <h3 className="text-xl font-bold tracking-tight">
                {active.title}
              </h3>
              <p className="mt-3 text-sm leading-relaxed text-ink-2">
                {active.body}
              </p>
            </div>
            <div>
              {tab === "waitlist" ? <WaitlistForm /> : <ContactForm />}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

interface TabButtonProps {
  label: string;
  active: boolean;
  onClick: () => void;
}

function TabButton({ label, active, onClick }: TabButtonProps) {
  return (
    <button
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={cn(
        "flex-1 px-6 py-4 text-sm font-semibold transition-colors",
        active
          ? "bg-paper text-ink"
          : "text-ink-3 hover:text-ink-2",
      )}
    >
      {label}
    </button>
  );
}
