export async function register() {
  if (process.env.NEXT_RUNTIME === "nodejs" && process.env.NEXT_PHASE !== "phase-production-build") {
    const { startSubmissionMaintenance } = await import("./lib/submission-maintenance");
    await startSubmissionMaintenance();
  }
}
