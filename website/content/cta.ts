import { SITE } from "@/content/meta";

export const CTA_SECTION = {
  heading: "Klaar om je lesplanning te automatiseren?",
  sub: "Bel of mail rechtstreeks — we plannen een korte demo in en beantwoorden je vragen.",
  tiles: {
    call: {
      label: "Bel ons",
      value: SITE.contactPhone.display,
      href: `tel:${SITE.contactPhone.href}`,
      hint: "Ma–zo · 9.00–21.00",
    },
    email: {
      label: "Stuur een e-mail",
      value: SITE.contactEmail,
      href: `mailto:${SITE.contactEmail}`,
      hint: "Reactie binnen één werkdag",
    },
  },

  // ─────────────────────────────────────────────────────────
  // Preserved legacy copy for the (currently unused) waitlist +
  // contact form components. Kept so the form files still
  // type-check; flip the FinalCta back to the tabbed form layout
  // by re-importing WaitlistForm / ContactForm if you ever want
  // forms back.
  // ─────────────────────────────────────────────────────────
  tabs: {
    waitlist: "Boek een demo",
    contact: "Neem contact op",
  },
  waitlist: {
    title: "Boek een demo",
    body: "Laat je e-mail achter en we plannen een korte demo in waarin we je live laten zien hoe CoachOS werkt voor jouw type organisatie.",
    submit: "Demo aanvragen",
    success: "Bedankt — we plannen je demo in.",
    fields: {
      email: "E-mailadres",
      role: "Wat doe je?",
      roleOptions: {
        club: "Tennis-/padelclub",
        coach: "Zelfstandige coach",
        anders: "Anders",
      },
    },
  },
  contact: {
    title: "Stel je vraag",
    body: "Vragen over features, prijzen, of een specifiek scenario? Stuur een bericht en we antwoorden binnen één werkdag.",
    submit: "Verstuur",
    success: "Bedankt — we nemen contact op.",
    fields: {
      name: "Naam",
      organization: "Organisatie (optioneel)",
      email: "E-mailadres",
      message: "Bericht",
    },
  },
} as const;
