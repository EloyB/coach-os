import { NextResponse } from "next/server";
import { contactSchema } from "@/lib/schemas";
import { postContact } from "@/lib/contact-api";

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

  return postContact({
    name: parsed.data.name, email: parsed.data.email,
    message: parsed.data.message, organization: parsed.data.organization,
  });
}
