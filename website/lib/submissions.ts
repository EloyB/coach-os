import { promises as fs } from "node:fs";
import path from "node:path";

const DATA_DIR = path.join(process.cwd(), "data");

export type SubmissionType = "contact" | "waitlist";

export async function appendSubmission(
  type: SubmissionType,
  payload: Record<string, unknown>,
) {
  await fs.mkdir(DATA_DIR, { recursive: true });
  const file = path.join(DATA_DIR, `${type}-submissions.json`);

  let existing: unknown[] = [];
  try {
    const raw = await fs.readFile(file, "utf-8");
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) existing = parsed;
  } catch {
    // first write — file doesn't exist yet
  }

  existing.push(payload);
  await fs.writeFile(file, JSON.stringify(existing, null, 2), "utf-8");
}
