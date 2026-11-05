import { NextResponse } from "next/server";
import { z } from "zod";
import { unsubscribeWaitlist } from "@/lib/submissions";

export const runtime = "nodejs";
const schema = z.object({ token: z.string().regex(/^[a-f0-9]{64}$/) });

// No GET handler: link scanners cannot remove a subscription.
export async function POST(req: Request) {
  const body: unknown = await req.json().catch(() => null);
  const parsed = schema.safeParse(body);
  if (!parsed.success) return NextResponse.json({ error: "invalid" }, { status: 400 });
  await unsubscribeWaitlist(parsed.data.token);
  return NextResponse.json({ ok: true }, { headers: { "Cache-Control": "no-store" } });
}
