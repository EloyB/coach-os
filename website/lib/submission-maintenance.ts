import { cleanupSubmissions, nextSubmissionCleanupDelay } from "./submissions";

let started = false;

export async function startSubmissionMaintenance(): Promise<void> {
  if (started) return;
  started = true;
  if (!process.env.WEBSITE_LAUNCH_DATE) {
    console.warn("[submissions] WEBSITE_LAUNCH_DATE ontbreekt: zesmaandenlimiet wachtlijst nog niet geactiveerd; bevestig de lanceringsdatum.");
  }
  const clean = async () => {
    try {
      await cleanupSubmissions();
    } catch {
      // Never log message contents, addresses, IPs or unsubscribe capabilities.
      console.error("[submissions] Opschoning mislukt. Controleer bestandsformaat, schrijfrechten en achtergebleven .lock-map; bestanden worden niet overschreven bij ongeldige inhoud.");
    }
  };
  const schedule = () => {
    let delay = 60 * 60 * 1000;
    try { delay = nextSubmissionCleanupDelay(); } catch { /* clean() reports invalid supplied configuration. */ }
    setTimeout(async () => { await clean(); schedule(); }, delay).unref();
  };
  await clean();
  schedule();
}
