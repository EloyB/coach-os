import { test, expect } from "bun:test";

const requests = [];
let backendStatus = 200;
const backend = Bun.serve({ hostname: "127.0.0.1", port: 0, async fetch(req) {
  requests.push({ url: req.url, body: await req.json(), ip: req.headers.get("x-forwarded-for"), authorization: req.headers.get("authorization") });
  // A compromised/unexpected upstream response must not leak a capability.
  return Response.json({ ok: true, unsubscribeUrl: "/uitschrijven#" + "a".repeat(64) }, { status: backendStatus });
} });
process.env.WEBSITE_API_URL = `http://127.0.0.1:${backend.port}/api`;
const { POST } = await import("../app/api/waitlist/route.ts");
const request = (data) => new Request("http://localhost/api/waitlist", { method: "POST", headers: { "content-type": "application/json", "x-forwarded-for": "192.0.2.1", authorization: "Bearer never-forward" }, body: JSON.stringify(data) });

test("website sends only validated waitlist fields to fixed backend and never returns a token", async () => {
  const response = await POST(request({ email: "proxy@example.test", role: "club", url: "https://attacker.invalid", subject: "arbitrary", website: "" }));
  expect(response.status).toBe(200);
  expect(requests.at(-1)?.body).toEqual({ email: "proxy@example.test", role: "club" });
  expect(requests.at(-1)?.url).toBe(process.env.WEBSITE_API_URL + "/website/waitlist");
  expect(requests.at(-1)?.ip).toBeNull();
  expect(requests.at(-1)?.authorization).toBeNull();
  expect(await response.json()).toEqual({ ok: true });
  expect(response.headers.get("cache-control")).toBe("no-store");
});

test("invalid inputs and honeypot do not enqueue mail", async () => {
  const count = requests.length;
  expect((await POST(request({ email: "bad" }))).status).toBe(400);
  expect((await POST(request({ email: "bot@example.test", website: "spam" }))).status).toBe(200);
  expect(requests.length).toBe(count);
});

test("upstream failure/rate limit/closure are not reported as success", async () => {
  for (const [upstream, expected] of [[500, 503], [429, 429], [410, 410]]) {
    backendStatus = upstream;
    expect((await POST(request({ email: "retry@example.test" }))).status).toBe(expected);
  }
  backendStatus = 200;
});

test("confirmed unsubscribe reaches backend, GET is absent and backend failure is visible", async () => {
  const token = "b".repeat(64);
  backendStatus = 500;
  // Legacy store tests intentionally chdir before their first import. Keep this
  // route import in its own process so its module-level DATA_DIR cannot pollute them.
  const child = Bun.spawn([process.execPath, "-e", `
    const route = await import('./app/api/waitlist/unsubscribe/route.ts');
    if (route.GET !== undefined) throw new Error('GET must be absent');
    const response = await route.POST(new Request('http://localhost/api/waitlist/unsubscribe', {
      method: 'POST', headers: {'Content-Type':'application/json'}, body: JSON.stringify({token:${JSON.stringify(token)}})
    }));
    if (response.status !== 503) throw new Error('Expected503, got'+response.status);
  `], { cwd: import.meta.dir + "/..", env: { PATH: process.env.PATH, WEBSITE_API_URL: process.env.WEBSITE_API_URL }, stdout: "pipe", stderr: "pipe" });
  const error = await new Response(child.stderr).text();
  expect(await child.exited, error).toBe(0);
  expect(requests.at(-1)?.body).toEqual({ token });
  expect(requests.at(-1)?.url).toEndWith("/website/waitlist/unsubscribe");
  backendStatus = 200;
});

// Bun exits when tests complete; server unref avoids leaking a long-lived handle.
backend.unref();
