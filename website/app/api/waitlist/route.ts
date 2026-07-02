import { NextResponse } from "next/server";
import { waitlistSchema } from "@/lib/schemas";
import { appendSubmission } from "@/lib/submissions";

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

  const { website: _hp, ...clean } = parsed.data;
  await appendSubmission("waitlist", {
    ...clean,
    submittedAt: new Date().toISOString(),
    ip: req.headers.get("x-forwarded-for") ?? null,
  });

  // TODO: replace with email service (Resend/Loops/SMTP) before production deploy.
  console.info("[waitlist]", clean.email, clean.role ?? ", ");

  return NextResponse.json({ ok: true });
}
