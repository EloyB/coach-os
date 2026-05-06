export const SITE = {
  name: "CoachOS",
  tagline: "Een planning die zichzelf bevestigt.",
  short:
    "Lesreeksen, inschrijvingen en betalingen voor tennis- en padelclubs — één systeem.",
  appUrl: process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:5317",
  contactEmail: "hallo@coach-os.be",
} as const;
