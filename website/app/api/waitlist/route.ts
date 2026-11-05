import { NextResponse } from "next/server";
import { waitlistSchema } from "@/lib/schemas";
import { appendSubmission, isWaitlistClosed } from "@/lib/submissions";

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

  const clean = { ...parsed.data };
  delete clean.website;
  const unsubscribeUrl = await appendSubmission("waitlist", {
    ...clean,
    submittedAt: new Date().toISOString(),
  });

  if (isWaitlistClosed()) {
    return NextResponse.json({ error: "waitlist_closed" }, { status: 410, headers: { "Cache-Control": "no-store" } });
  }

  // Only the initial registration receives its new capability; duplicates do
  // not reveal or rotate an existing subscriber's token.
  return NextResponse.json({ ok: true, ...(unsubscribeUrl ? { unsubscribeUrl } : {}) }, {
    headers: { "Cache-Control": "no-store" },
  });
}
