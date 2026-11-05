import { NextResponse } from "next/server";
import { waitlistSchema } from "@/lib/schemas";
import { postWaitlist } from "@/lib/waitlist-api";

export const runtime = "nodejs";

export async function POST(req: Request) {
  let body: unknown;
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: "invalid json" }, { status: 400 });
  }

  const parsed = waitlistSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json({ error: "invalid" }, { status: 400 });
  }

  if (parsed.data.website) {
    return NextResponse.json({ ok: true });
  }

  return postWaitlist("", { email: parsed.data.email, ...(parsed.data.role ? { role: parsed.data.role } : {}) });
}
