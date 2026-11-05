import { NextResponse } from "next/server";

// Server-only adapter. Same Compose network as api:8080; optional loopback
// override for development/tests. Never derive a mail URL/host from a request.
export async function postWaitlist(path: "" | "/unsubscribe", payload: object) {
  try {
    const base = process.env.WEBSITE_API_URL || "http://api:8080/api";
    const response = await fetch(`${base.replace(/\/$/, "")}/website/waitlist${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
      cache: "no-store",
      signal: AbortSignal.timeout(10_000),
    });
    // Never forward an upstream body/token or user Authorization/IP headers.
    const status = response.ok ? 200 : [400, 410, 429].includes(response.status) ? response.status : 503;
    return NextResponse.json(status === 200 ? { ok: true } : { error: "waitlist_unavailable" }, {
      status, headers: { "Cache-Control": "no-store" },
    });
  } catch {
    return NextResponse.json({ error: "waitlist_unavailable" }, { status: 503, headers: { "Cache-Control": "no-store" } });
  }
}
