import { test, expect, beforeAll, afterAll, setSystemTime } from "bun:test";
import { mkdtemp, readFile, rm, writeFile, mkdir } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";

const originalCwd = process.cwd();
let directory;
let contact;
beforeAll(async () => {
  directory = await mkdtemp(path.join(tmpdir(), "coachos-264-"));
  process.chdir(directory);
  contact = await import("../app/api/contact/route.ts");
});
afterAll(async () => {
  process.chdir(originalCwd);
  await rm(directory, { recursive: true, force: true });
});
const records = async (type) => JSON.parse(await readFile(path.join(directory, "data", `${type}-submissions.json`), "utf8"));

test("contact API persists the message without forwarded IP or honeypot", async () => {
  const response = await contact.POST(new Request("http://localhost/api/contact", {
    method: "POST", headers: { "content-type": "application/json", "x-forwarded-for": "192.0.2.10" },
    body: JSON.stringify({ name: "Local Test", email: "local@example.test", message: "A local test message", website: "" }),
  }));
  expect(response.status).toBe(200);
  const [saved] = await records("contact");
  expect(saved.email).toBe("local@example.test");
  expect(saved).not.toHaveProperty("ip");
  expect(saved).not.toHaveProperty("website");
});

test("malformed file fails closed without replacing legacy bytes", async () => {
  const { appendSubmission } = await import("../lib/submissions.ts");
  for (const raw of ["{broken", "{}", "[null]", '[{"email":"legacy@example.test","submittedAt":"invalid"}]']) {
    await writeFile(file("contact"), raw);
    await expect(appendSubmission("contact", { email: "new@example.test", submittedAt: new Date().toISOString() })).rejects.toThrow();
    expect(await readFile(file("contact"), "utf8")).toBe(raw);
  }
});

test("concurrent writes do not lose submissions or expose partial JSON", async () => {
  const { appendSubmission } = await import("../lib/submissions.ts");
  await seed("contact", []);
  let reading = true;
  const reader = (async () => { while (reading) { await records("contact"); await new Promise((r) => setTimeout(r, 1)); } })();
  try {
    await Promise.all(Array.from({ length: 40 }, (_, i) => appendSubmission("contact", { email: `parallel${i}@example.test`, submittedAt: new Date().toISOString() })));
  } finally { reading = false; await reader; }
  expect(await records("contact")).toHaveLength(40);
});

test("contact retention removes only entries older than twelve calendar months", async () => {
  const { appendSubmission } = await import("../lib/submissions.ts");
  const now = new Date();
  const old = new Date(now); old.setUTCFullYear(old.getUTCFullYear() - 2);
  await seed("contact", [{ email: "old@example.test", submittedAt: old.toISOString(), ip: "192.0.2.1" }]);
  await appendSubmission("contact", { email: "recent@example.test", submittedAt: now.toISOString() });
  expect((await records("contact")).map((entry) => entry.email)).toEqual(["recent@example.test"]);
});

test("waitlist registration omits IP and duplicate registration never reveals an existing unsubscribe link", async () => {
  const waitlist = await import("../app/api/waitlist/route.ts");
  await seed("waitlist", []);
  const register = (email) => waitlist.POST(new Request("http://localhost/api/waitlist", { method: "POST", headers: { "content-type": "application/json", "x-forwarded-for": "192.0.2.99" }, body: JSON.stringify({ email, role: "club" }) }));
  const first = await register("owner@example.test");
  const result = await first.json();
  expect(first.status).toBe(200);
  expect(result.unsubscribeUrl).toMatch(/^\/uitschrijven#[a-f0-9]{64}$/);
  expect(await (await register("OWNER@example.test")).json()).toEqual({ ok: true });
  const saved = await records("waitlist");
  expect(saved).toHaveLength(1);
  expect(saved[0]).not.toHaveProperty("ip");
  expect(JSON.stringify(saved)).not.toContain(result.unsubscribeUrl.split("#")[1]);
});

test("unsubscribe requires explicit POST capability and removes only its owner, idempotently", async () => {
  const route = await import("../app/api/waitlist/unsubscribe/route.ts").catch(() => ({}));
  expect(typeof route.POST).toBe("function");
  expect(route.GET).toBeUndefined();
  const { appendSubmission } = await import("../lib/submissions.ts");
  await seed("waitlist", []);
  const link = await appendSubmission("waitlist", { email: "owner@example.test", submittedAt: new Date().toISOString() });
  await appendSubmission("waitlist", { email: "other@example.test", submittedAt: new Date().toISOString() });
  const request = (token) => new Request("http://localhost/api/waitlist/unsubscribe", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ token }) });
  expect((await route.POST(request("guess"))).status).toBe(400);
  await route.POST(request("a".repeat(64)));
  expect(await records("waitlist")).toHaveLength(2);
  const token = link.split("#")[1];
  expect((await route.POST(request(token))).status).toBe(200);
  expect((await records("waitlist")).map((entry) => entry.email)).toEqual(["other@example.test"]);
  expect((await route.POST(request(token))).status).toBe(200);
});

test("waitlist closes at six calendar months after explicitly configured launch, absent date retains records", async () => {
  const { appendSubmission } = await import("../lib/submissions.ts");
  delete process.env.WEBSITE_LAUNCH_DATE;
  await seed("waitlist", [{ email: "legacy@example.test", submittedAt: "2020-01-01T00:00:00.000Z", ip: "192.0.2.1" }]);
  await appendSubmission("waitlist", { email: "recent@example.test", submittedAt: new Date().toISOString() });
  expect(await records("waitlist")).toHaveLength(2);
  process.env.WEBSITE_LAUNCH_DATE = "2020-01-31";
  try {
    const result = await appendSubmission("waitlist", { email: "late@example.test", submittedAt: new Date().toISOString() });
    expect(result).toBeUndefined();
    expect(await records("waitlist")).toEqual([]);
  } finally { delete process.env.WEBSITE_LAUNCH_DATE; }
});

test("maintenance removes expired contacts and scrubs both files without a new submission", async () => {
  const store = await import("../lib/submissions.ts");
  expect(typeof store.cleanupSubmissions).toBe("function");
  await seed("contact", [
    { email: "expired@example.test", submittedAt: "2000-01-01T00:00:00.000Z", ip: "192.0.2.1" },
    { email: "active@example.test", submittedAt: new Date().toISOString(), ip: "192.0.2.2" },
  ]);
  await seed("waitlist", [{ email: "waiting@example.test", submittedAt: "2020-01-01T00:00:00.000Z", ip: "192.0.2.3" }]);
  await store.cleanupSubmissions();
  expect((await records("contact")).map((entry) => entry.email)).toEqual(["active@example.test"]);
  expect(await records("waitlist")).toHaveLength(1);
  expect([...(await records("contact")), ...(await records("waitlist"))].every((entry) => !("ip" in entry))).toBe(true);
});

test("runtime maintenance cleans up on startup and on the periodic timer", async () => {
  const maintenance = await import("../lib/submission-maintenance.ts").catch(() => ({}));
  expect(typeof maintenance.startSubmissionMaintenance).toBe("function");
  await seed("contact", [{ email: "expired@example.test", submittedAt: "2000-01-01T00:00:00.000Z" }]);
  await seed("waitlist", []);
  const originalTimeout = globalThis.setTimeout;
  let scheduled;
  let unreferenced = false;
  let delay;
  process.env.WEBSITE_LAUNCH_DATE = "2026-08-31";
  setSystemTime(new Date("2027-02-27T23:45:00.000Z"));
  globalThis.setTimeout = (callback, milliseconds) => { scheduled = callback; delay = milliseconds; return { unref: () => { unreferenced = true; } }; };
  try {
    await maintenance.startSubmissionMaintenance();
    expect(await records("contact")).toEqual([]);
    expect(unreferenced).toBe(true);
    expect(delay).toBe(15 * 60 * 1000);
    await seed("contact", [{ email: "expired-later@example.test", submittedAt: "2000-01-01T00:00:00.000Z" }]);
    await scheduled();
    expect(await records("contact")).toEqual([]);
  } finally { globalThis.setTimeout = originalTimeout; setSystemTime(); delete process.env.WEBSITE_LAUNCH_DATE; }
});

test("calendar boundaries clamp leap days and six-month month-ends without early deletion", async () => {
  const store = await import("../lib/submissions.ts");
  try {
    setSystemTime(new Date("2028-02-29T00:00:00.000Z"));
    await seed("contact", [
      { email: "boundary@example.test", submittedAt: "2027-02-28T00:00:00.000Z" },
      { email: "too-old@example.test", submittedAt: "2027-02-27T23:59:59.999Z" },
    ]);
    await store.cleanupSubmissions();
    expect((await records("contact")).map((entry) => entry.email)).toEqual(["boundary@example.test"]);
    process.env.WEBSITE_LAUNCH_DATE = "2026-08-31";
    setSystemTime(new Date("2027-02-27T23:59:59.999Z"));
    await seed("waitlist", [{ email: "waiting@example.test", submittedAt: "2026-09-01T00:00:00.000Z" }]);
    await store.cleanupSubmissions();
    expect(await records("waitlist")).toHaveLength(1);
    setSystemTime(new Date("2027-02-28T00:00:00.000Z"));
    await store.cleanupSubmissions();
    expect(await records("waitlist")).toEqual([]);
  } finally { setSystemTime(); delete process.env.WEBSITE_LAUNCH_DATE; }
});

test("invalid supplied launch date fails closed rather than guessing a date", async () => {
  const store = await import("../lib/submissions.ts");
  await seed("waitlist", [{ email: "preserved@example.test", submittedAt: new Date().toISOString() }]);
  const before = await readFile(file("waitlist"), "utf8");
  process.env.WEBSITE_LAUNCH_DATE = "2026-02-31";
  try {
    await expect(store.cleanupSubmissions()).rejects.toThrow("valid YYYY-MM-DD");
    expect(await readFile(file("waitlist"), "utf8")).toBe(before);
  } finally { delete process.env.WEBSITE_LAUNCH_DATE; }
});

test("waitlist API reports closure instead of acknowledging a discarded registration", async () => {
  const waitlist = await import("../app/api/waitlist/route.ts");
  process.env.WEBSITE_LAUNCH_DATE = "2020-01-01";
  try {
    const response = await waitlist.POST(new Request("http://localhost/api/waitlist", {
      method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ email: "late@example.test" }),
    }));
    expect(response.status).toBe(410);
  } finally { delete process.env.WEBSITE_LAUNCH_DATE; }
});

test("Next instrumentation cleans at Node startup but never during a build or edge runtime", async () => {
  const modulePath = path.resolve(originalCwd, "instrumentation.ts");
  for (const [runtime, phase, shouldClean] of [
    ["nodejs", "phase-production-build", false],
    ["edge", "", false],
    ["nodejs", "", true],
  ]) {
    await seed("contact", [{ email: "expired@example.test", submittedAt: "2000-01-01T00:00:00.000Z" }]);
    await seed("waitlist", []);
    const child = Bun.spawn([process.execPath, "-e", `const module = await import(${JSON.stringify(modulePath)}); if (typeof module.register !== 'function') throw new Error('runtime registration missing'); await module.register();`], {
      cwd: directory, env: { ...process.env, NEXT_RUNTIME: runtime, NEXT_PHASE: phase, WEBSITE_LAUNCH_DATE: "" }, stdout: "pipe", stderr: "pipe",
    });
    const errors = await new Response(child.stderr).text();
    expect(await child.exited, errors).toBe(0);
    expect(await records("contact")).toHaveLength(shouldClean ? 0 : 1);
  }
});

const file = (type) => path.join(directory, "data", `${type}-submissions.json`);
const seed = async (type, values) => { await mkdir(path.dirname(file(type)), { recursive: true }); await writeFile(file(type), JSON.stringify(values)); };

test("store strips legacy and incoming IP while preserving records", async () => {
  const { appendSubmission } = await import("../lib/submissions.ts");
  await seed("contact", [{ email: "legacy@example.test", submittedAt: new Date().toISOString(), ip: "192.0.2.20" }]);
  await appendSubmission("contact", { email: "new@example.test", submittedAt: new Date().toISOString(), ip: "192.0.2.30" });
  const saved = await records("contact");
  expect(saved).toHaveLength(2);
  expect(saved.every((entry) => !("ip" in entry))).toBe(true);
});
