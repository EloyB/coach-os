import { NextResponse } from "next/server";
import { contactSchema } from "@/lib/schemas";
import { appendSubmission } from "@/lib/submissions";

export const runtime = "nodejs";

export async function POST(req: Request) {
  let body: unknown;
  try {
    body = await req.json();
  } catch {
    return NextResponse.json({ error: "invalid json" }, { status: 400 });
  }

  const parsed = contactSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json({ error: "invalid" }, { status: 400 });
  }

  if (parsed.data.website) {
    return NextResponse.json({ ok: true });
  }

  const clean = { ...parsed.data };
  delete clean.website;
  await appendSubmission("contact", {
    ...clean,
    submittedAt: new Date().toISOString(),
  });

  // TODO: replace with email service (Resend/Loops/SMTP) before production deploy.
  console.info("[contact]", clean.email, clean.organization ?? "—");

  return NextResponse.json({ ok: true });
}
