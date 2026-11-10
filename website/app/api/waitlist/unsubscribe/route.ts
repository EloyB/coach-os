import { NextResponse } from "next/server";
import { z } from "zod";
import { unsubscribeWaitlist } from "@/lib/submissions";
import { postWaitlist } from "@/lib/waitlist-api";

export const runtime = "nodejs";
const schema = z.object({ token: z.string().regex(/^[a-f0-9]{64}$/) });

// Geen GET-handler: linkscanners kunnen geen inschrijving verwijderen.
export async function POST(req: Request) {
  const body: unknown = await req.json().catch(() => null);
  const parsed = schema.safeParse(body);
  if (!parsed.success) return NextResponse.json({ error: "invalid" }, { status: 400 });
  const response = await postWaitlist(req, "/unsubscribe", parsed.data);
  if (!response.ok) return response;
  try {
    // Oude JSON-links blijven werken tot de legacybestanden na import verwijderd zijn.
    await unsubscribeWaitlist(parsed.data.token);
  } catch {
    // Nooit tokens of adressen loggen; de API-verwijdering is al gelukt.
    console.error("[uitschrijven] Legacy-wachtlijstbestand kon niet bijgewerkt worden; controleer data/waitlist-submissions.json.");
    return NextResponse.json({ error: "waitlist_unavailable" }, { status: 503, headers: { "Cache-Control": "no-store" } });
  }
  return NextResponse.json({ ok: true }, { headers: { "Cache-Control": "no-store" } });
}
