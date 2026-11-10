import { promises as fs } from "node:fs";
import path from "node:path";
import { randomUUID, createHash } from "node:crypto";

// Legacy-brug: tot de historische JSON-inschrijvingen in PostgreSQL geïmporteerd zijn en de
// bestanden verwijderd, moet een oude uitschrijflink ook het JSON-record weghalen. Er komen geen
// nieuwe JSON-records meer bij; nieuwe inschrijvingen gaan uitsluitend naar de API.
const DATA_DIR = path.join(process.cwd(), "data");
const WAITLIST_FILE = path.join(DATA_DIR, "waitlist-submissions.json");

type LegacyRecord = Record<string, unknown> & { unsubscribeTokenHash?: unknown };

// Serialiseert schrijfacties binnen dit Node-proces (de website draait als één container).
let queue: Promise<unknown> = Promise.resolve();

async function readLegacyWaitlist(): Promise<LegacyRecord[] | undefined> {
  try {
    const parsed: unknown = JSON.parse(await fs.readFile(WAITLIST_FILE, "utf8"));
    if (!Array.isArray(parsed) || parsed.some((r) => !r || typeof r !== "object" || Array.isArray(r))) {
      throw new Error("Invalid legacy waitlist file");
    }
    return parsed as LegacyRecord[];
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code === "ENOENT") return undefined;
    throw error;
  }
}

async function removeLegacyCapability(hash: string): Promise<void> {
  const records = await readLegacyWaitlist();
  if (!records) return;
  const remaining = records.filter((record) => record.unsubscribeTokenHash !== hash);
  if (remaining.length === records.length) return;
  // Atomisch vervangen: nooit een half geschreven bestand achterlaten.
  const temporary = `${WAITLIST_FILE}.${randomUUID()}.tmp`;
  try {
    await fs.writeFile(temporary, JSON.stringify(remaining, null, 2), { encoding: "utf8", mode: 0o600, flag: "wx" });
    await fs.rename(temporary, WAITLIST_FILE);
  } finally {
    await fs.rm(temporary, { force: true });
  }
}

export function unsubscribeWaitlist(token: string): Promise<void> {
  const hash = createHash("sha256").update(token).digest("hex");
  const next = queue.then(() => removeLegacyCapability(hash));
  queue = next.catch(() => undefined);
  return next;
}
