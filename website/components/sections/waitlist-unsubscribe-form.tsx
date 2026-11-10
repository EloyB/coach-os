"use client";

import { useState } from "react";
import { useUnsubscribeToken } from "@/components/sections/waitlist-unsubscribe-link";
import { WAITLIST_PRIVACY as copy } from "@/content/waitlist-privacy";

export function WaitlistUnsubscribeForm() {
  const token = useUnsubscribeToken();
  const [status, setStatus] = useState<"idle" | "pending" | "success" | "error">("idle");

  async function unsubscribe() {
    if (!token || status === "pending" || status === "success") return;
    setStatus("pending");
    try {
      const response = await fetch("/api/waitlist/unsubscribe", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token }),
      });
      if (!response.ok) throw new Error("unsubscribe failed");
      window.history.replaceState(null, "", window.location.pathname);
      setStatus("success");
    } catch {
      setStatus("error");
    }
  }

  if (status === "success") {
    return <p role="status" className="mt-6 text-sm leading-relaxed text-tennis-green">{copy.success}</p>;
  }
  if (!token) return <p className="mt-6 text-sm leading-relaxed text-ink-2">{copy.invalid}</p>;

  return (
    <div className="mt-6">
      <p className="text-sm leading-relaxed text-ink-2">{copy.explanation}</p>
      {status === "error" && <p role="alert" className="mt-4 text-sm text-urgent">{copy.error}</p>}
      <button
        type="button"
        onClick={unsubscribe}
        disabled={status === "pending"}
        className="mt-6 inline-flex min-h-11 items-center justify-center rounded-md bg-tennis-lime px-5 py-2 text-sm font-semibold text-ink transition-colors hover:bg-tennis-lime/90 disabled:opacity-50"
      >
        {status === "pending" ? copy.pending : copy.confirm}
      </button>
    </div>
  );
}
