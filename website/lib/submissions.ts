import { promises as fs } from "node:fs";
import path from "node:path";
import { randomUUID, randomBytes, createHash } from "node:crypto";

const DATA_DIR = path.join(process.cwd(), "data");
export type SubmissionType = "contact" | "waitlist";
type Submission = Record<string, unknown> & { email: string; submittedAt: string };

function validate(records: unknown): asserts records is Submission[] {
  if (!Array.isArray(records) || records.some((record) =>
    !record || typeof record !== "object" || Array.isArray(record) ||
    typeof record.email !== "string" || typeof record.submittedAt !== "string" ||
    !Number.isFinite(Date.parse(record.submittedAt)))) {
    throw new Error("Invalid submissions file");
  }
}

function sixMonthDeadline(): number | undefined {
  // Explicitly approved by Lorenz; not inferred from a deployment.
  const launch = process.env.WEBSITE_LAUNCH_DATE || "2026-11-15";
  const date = new Date(`${launch}T00:00:00.000Z`);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(launch) || !Number.isFinite(date.getTime()) || date.toISOString().slice(0, 10) !== launch) {
    throw new Error("WEBSITE_LAUNCH_DATE must be a valid YYYY-MM-DD date");
  }
  const day = date.getUTCDate();
  date.setUTCDate(1);
  date.setUTCMonth(date.getUTCMonth() + 6);
  const lastDay = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + 1, 0)).getUTCDate();
  date.setUTCDate(Math.min(day, lastDay));
  return date.getTime();
}

export function nextSubmissionCleanupDelay(): number {
  const maximum = 60 * 60 * 1000;
  const deadline = sixMonthDeadline();
  const remaining = deadline === undefined ? 0 : deadline - Date.now();
  return remaining > 0 ? Math.min(maximum, remaining) : maximum;
}

export function isWaitlistClosed(): boolean {
  const deadline = sixMonthDeadline();
  return deadline !== undefined && Date.now() >= deadline;
}

// The filesystem lock covers the entire read/modify/rename transaction, including
// separate Node workers. Never steal a stale lock: an operator must investigate it.
async function transaction<T>(type: SubmissionType, update: (records: Submission[]) => T) {
  await fs.mkdir(DATA_DIR, { recursive: true, mode: 0o700 });
  const file = path.join(DATA_DIR, `${type}-submissions.json`);
  const lock = `${file}.lock`;
  const deadline = Date.now() + 5000;
  for (;;) {
    try { await fs.mkdir(lock, { mode: 0o700 }); break; }
    catch (error) {
      if ((error as NodeJS.ErrnoException).code !== "EEXIST" || Date.now() >= deadline) throw error;
      await new Promise((resolve) => setTimeout(resolve, 10));
    }
  }
  const temporary = `${file}.${randomUUID()}.tmp`;
  try {
    let records: Submission[] = [];
    try {
      const parsed: unknown = JSON.parse(await fs.readFile(file, "utf8"));
      validate(parsed);
      records = parsed;
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== "ENOENT") throw error;
    }
    const result = update(records);
    if (type === "contact") {
      const cutoff = new Date();
      const day = cutoff.getUTCDate();
      cutoff.setUTCDate(1);
      cutoff.setUTCFullYear(cutoff.getUTCFullYear() - 1);
      const lastDay = new Date(Date.UTC(cutoff.getUTCFullYear(), cutoff.getUTCMonth() + 1, 0)).getUTCDate();
      cutoff.setUTCDate(Math.min(day, lastDay));
      records = records.filter((entry) => Date.parse(entry.submittedAt) >= cutoff.getTime());
    }
    if (type === "waitlist" && isWaitlistClosed()) records = [];
    validate(records);
    const clean = records.map((record) => {
      const copy = { ...record };
      delete copy.ip;
      return copy;
    });
    const handle = await fs.open(temporary, "wx", 0o600);
    try { await handle.writeFile(JSON.stringify(clean, null, 2), "utf8"); await handle.sync(); }
    finally { await handle.close(); }
    await fs.rename(temporary, file);
    const directory = await fs.open(DATA_DIR, "r");
    try { await directory.sync(); } finally { await directory.close(); }
    return result;
  } finally {
    await fs.rm(temporary, { force: true });
    await fs.rmdir(lock);
  }
}

export async function cleanupSubmissions(): Promise<void> {
  // Independent files: a malformed contact file must not prevent waitlist cleanup.
  const results = await Promise.allSettled([
    transaction("contact", () => undefined),
    transaction("waitlist", () => undefined),
  ]);
  const failed = results.find((result) => result.status === "rejected");
  if (failed?.status === "rejected") throw failed.reason;
}

export async function unsubscribeWaitlist(token: string) {
  const hash = createHash("sha256").update(token).digest("hex");
  await transaction("waitlist", (records) => {
    for (let i = records.length - 1; i >= 0; i--) {
      if (records[i].unsubscribeTokenHash === hash) records.splice(i, 1);
    }
  });
}

export async function appendSubmission(type: SubmissionType, payload: Record<string, unknown>) {
  const incoming: unknown = [payload];
  validate(incoming);
  const submission = incoming[0];
  return transaction(type, (records) => {
    if (type === "waitlist") {
      if (isWaitlistClosed()) return undefined;
      const email = submission.email.trim().toLowerCase();
      if (records.some((entry) => entry.email.trim().toLowerCase() === email)) return undefined;
      const token = randomBytes(32).toString("hex");
      records.push({ ...submission, email, unsubscribeTokenHash: createHash("sha256").update(token).digest("hex") });
      return `/uitschrijven#${token}`;
    }
    records.push(submission);
    return undefined;
  });
}
