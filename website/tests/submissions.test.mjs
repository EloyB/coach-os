import { test, expect, beforeAll, afterAll } from "bun:test";
import { createHash } from "node:crypto";
import { mkdtemp, readFile, rm, writeFile, mkdir } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";

const originalCwd = process.cwd();
let directory;
let contact;
let backend;
let backendStatus = 200;
let received;
let receivedForwardedFor;
const originalApi = process.env.WEBSITE_API_URL;
beforeAll(async () => {
  directory = await mkdtemp(path.join(tmpdir(), "coachos-264-"));
  process.chdir(directory);
  contact = await import("../app/api/contact/route.ts");
  backend = Bun.serve({ hostname: "127.0.0.1", port: 0, async fetch(req) {
    received = await req.json();
    receivedForwardedFor = req.headers.get("x-forwarded-for");
    return Response.json({ ok: true }, { status: backendStatus });
  } });
  process.env.WEBSITE_API_URL = `http://127.0.0.1:${backend.port}/api`;
});
afterAll(async () => {
  backend?.stop(true);
  if (originalApi === undefined) delete process.env.WEBSITE_API_URL;
  else process.env.WEBSITE_API_URL = originalApi;
  process.chdir(originalCwd);
  await rm(directory, { recursive: true, force: true });
});

const file = (type) => path.join(directory, "data", `${type}-submissions.json`);
const seed = async (type, values) => { await mkdir(path.dirname(file(type)), { recursive: true }); await writeFile(file(type), JSON.stringify(values)); };
const records = async (type) => JSON.parse(await readFile(file(type), "utf8"));
const sha256 = (value) => createHash("sha256").update(value).digest("hex");

test("contact API proxies only the message fields and never writes a JSON file", async () => {
  const response = await contact.POST(new Request("http://localhost/api/contact", {
    method: "POST", headers: { "content-type": "application/json", "x-forwarded-for": "192.0.2.10" },
    body: JSON.stringify({ name: "Local Test", email: "local@example.test", message: "A local test message", website: "" }),
  }));
  expect(response.status).toBe(200);
  expect(received).toEqual({ name: "Local Test", email: "local@example.test", message: "A local test message" });
  expect(receivedForwardedFor).toBeNull();
  await expect(readFile(file("contact"))).rejects.toThrow();
});

test("contact API forwards only the nginx-set X-Real-IP for per-visitor rate limiting", async () => {
  for (const [realIp, expected] of [["203.0.113.7", "203.0.113.7"], ["not-an-ip, 1.2.3.4", null]]) {
    await contact.POST(new Request("http://localhost/api/contact", {
      method: "POST", headers: { "content-type": "application/json", "x-real-ip": realIp, "x-forwarded-for": "192.0.2.10" },
      body: JSON.stringify({ name: "Local Test", email: "local@example.test", message: "A local test message" }),
    }));
    expect(receivedForwardedFor).toBe(expected);
  }
});

test("waitlist registration uses the backend and creates no JSON records", async () => {
  const waitlist = await import("../app/api/waitlist/route.ts");
  await seed("waitlist", []);
  const response = await waitlist.POST(new Request("http://localhost/api/waitlist", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ email: "owner@example.test", role: "club" }) }));
  expect(response.status).toBe(200);
  expect(await response.json()).toEqual({ ok: true });
  expect(received).toEqual({ email: "owner@example.test", role: "club" });
  expect(await records("waitlist")).toHaveLength(0);
});

test("legacy unsubscribe link removes only its own JSON record, idempotently", async () => {
  const route = await import("../app/api/waitlist/unsubscribe/route.ts");
  expect(route.GET).toBeUndefined();
  const token = "c".repeat(64);
  await seed("waitlist", [
    { email: "owner@example.test", submittedAt: "2026-11-01T00:00:00.000Z", unsubscribeTokenHash: sha256(token) },
    { email: "other@example.test", submittedAt: "2026-11-01T00:00:00.000Z", unsubscribeTokenHash: sha256("d".repeat(64)) },
  ]);
  const request = (value) => new Request("http://localhost/api/waitlist/unsubscribe", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ token: value }) });
  expect((await route.POST(request("guess"))).status).toBe(400);
  expect((await route.POST(request("a".repeat(64)))).status).toBe(200);
  expect(await records("waitlist")).toHaveLength(2);
  expect((await route.POST(request(token))).status).toBe(200);
  expect(received).toEqual({ token });
  expect((await records("waitlist")).map((entry) => entry.email)).toEqual(["other@example.test"]);
  expect((await route.POST(request(token))).status).toBe(200);
});

test("malformed legacy file is never overwritten", async () => {
  const route = await import("../app/api/waitlist/unsubscribe/route.ts");
  await mkdir(path.dirname(file("waitlist")), { recursive: true });
  for (const raw of ["{broken", "{}", "[null]"]) {
    await writeFile(file("waitlist"), raw);
    const response = await route.POST(new Request("http://localhost/api/waitlist/unsubscribe", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ token: "e".repeat(64) }) }));
    expect(response.status).toBe(503);
    expect(await readFile(file("waitlist"), "utf8")).toBe(raw);
  }
});

test("waitlist API reports closure instead of acknowledging a discarded registration", async () => {
  const waitlist = await import("../app/api/waitlist/route.ts");
  backendStatus = 410;
  try {
    const response = await waitlist.POST(new Request("http://localhost/api/waitlist", {
      method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ email: "late@example.test" }),
    }));
    expect(response.status).toBe(410);
  } finally { backendStatus = 200; }
});
