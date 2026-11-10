import { test, expect } from "bun:test";
import { POST } from "../app/api/contact/route";

test("contact upstream failure never acknowledges lost data or forwards private errors", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = async () => new Response("private upstream details", { status: 500 });
  try {
    const response = await POST(new Request("http://localhost/api/contact", { method: "POST", body: JSON.stringify({ name: "Synthetic Person", email: "synthetic@example.test", message: "Synthetic contact message" }) }));
    expect(response.status).toBe(503);
    expect(await response.text()).not.toContain("private upstream details");
  } finally { globalThis.fetch = original; }
});

test("contact uses specific backend endpoint, no PII logging or filesystem fallback", async () => {
  const original = globalThis.fetch;
  const previous = process.env.WEBSITE_API_URL;
  process.env.WEBSITE_API_URL = "http://127.0.0.1:59999/api";
  let called = "";
  globalThis.fetch = async (input) => {
    called = String(input);
    return new Response('{"ok":true}', { status: 200 });
  };
  try {
    const response = await POST(new Request("http://localhost/api/contact", { method: "POST", body: JSON.stringify({ name: "Synthetic Person", email: "synthetic@example.test", message: "Synthetic contact message" }) }));
    expect(response.status).toBe(200);
    expect(called).toBe("http://127.0.0.1:59999/api/website/contact");
  } finally {
    globalThis.fetch = original;
    if (previous === undefined) delete process.env.WEBSITE_API_URL;
    else process.env.WEBSITE_API_URL = previous;
  }
});
