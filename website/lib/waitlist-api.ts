import { NextResponse } from "next/server";
import { forwardedClientHeaders } from "./client-ip";

// Server-only adapter naar api:8080 (zelfde Compose-netwerk); WEBSITE_API_URL is een optionele
// override voor dev/tests. Nooit een mail-URL/host afleiden uit een request.
export async function postWaitlist(req: Request, path: "" | "/unsubscribe", payload: object) {
  try {
    const base = process.env.WEBSITE_API_URL || "http://api:8080/api";
    const response = await fetch(`${base.replace(/\/$/, "")}/website/waitlist${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...forwardedClientHeaders(req) },
      body: JSON.stringify(payload),
      cache: "no-store",
      signal: AbortSignal.timeout(10_000),
    });
    // Nooit een upstream-body/token of Authorization-header doorgeven.
    const status = response.ok ? 200 : [400, 410, 429].includes(response.status) ? response.status : 503;
    return NextResponse.json(status === 200 ? { ok: true } : { error: "waitlist_unavailable" }, {
      status, headers: { "Cache-Control": "no-store" },
    });
  } catch {
    return NextResponse.json({ error: "waitlist_unavailable" }, { status: 503, headers: { "Cache-Control": "no-store" } });
  }
}
