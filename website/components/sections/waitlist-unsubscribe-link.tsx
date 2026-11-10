"use client";

import { useSyncExternalStore } from "react";
import { WAITLIST_PRIVACY } from "@/content/waitlist-privacy";

function subscribe(onChange: () => void) {
  window.addEventListener("hashchange", onChange);
  return () => window.removeEventListener("hashchange", onChange);
}

export function useUnsubscribeToken() {
  return useSyncExternalStore(
    subscribe,
    () => /^#([a-f0-9]{64})$/.exec(window.location.hash)?.[1] ?? "",
    () => "",
  );
}

export function WaitlistUnsubscribeLink() {
  const token = useUnsubscribeToken();
  if (!token) return null;

  return (
    <div className="mt-6 border-t border-rule pt-5 text-sm text-ink-2">
      <p>{WAITLIST_PRIVACY.saveHint}</p>
      <a
        href={`/uitschrijven#${token}`}
        className="mt-3 inline-block font-semibold text-tennis-green underline underline-offset-4"
      >
        {WAITLIST_PRIVACY.saveLink}
      </a>
    </div>
  );
}
