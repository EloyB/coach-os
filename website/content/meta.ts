export const SITE = {
  name: "CoachOS",
  tagline: "Een planning die zichzelf bevestigt.",
  short: "Lesplanning, inschrijvingen en betalingen — één systeem.",
  appUrl: process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:5317",
  contactEmail: "info@coach-os.be",
  contactPhone: {
    /** As displayed on the page. */
    display: "+32 478 77 44 97",
    /** For tel: href — digits + leading plus only, no spaces. */
    href: "+32478774497",
  },
} as const;
