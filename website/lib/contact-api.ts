import { NextResponse } from "next/server";

// Fixed server-only endpoint. Never forward auth, client IP, or upstream bodies.
export async function postContact(payload: object) {
  try {
    const base = process.env.WEBSITE_API_URL || "http://api:8080/api";
    const response = await fetch(`${base.replace(/\/$/, "")}/website/contact`, {
      method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload),
      cache: "no-store", signal: AbortSignal.timeout(10_000),
    });
    const status = response.ok ? 200 : [400, 429].includes(response.status) ? response.status : 503;
    return NextResponse.json(status === 200 ? { ok: true } : { error: "contact_unavailable" }, {
      status, headers: { "Cache-Control": "no-store" },
    });
  } catch {
    return NextResponse.json({ error: "contact_unavailable" }, { status: 503, headers: { "Cache-Control": "no-store" } });
  }
}
