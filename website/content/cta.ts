import { SITE } from "@/content/meta";

/**
 * Vooringevulde aanmeldmail: onderwerp + een kort sjabloon met de gegevens die
 * we nodig hebben om persoonlijk op te starten. Zo moet de club zelf weinig typen.
 */
const MAIL_SUBJECT = "Interesse in CoachOS";
const MAIL_BODY = [
  "Hallo CoachOS,",
  "",
  "Wij zijn geïnteresseerd in jullie platform en zouden graag wat meer info hebben over hoe CoachOS voor onze club zou werken.",
  "",
  "Een paar gegevens om alvast mee te geven:",
  "",
  "- Club of school: ",
  "- Contactpersoon: ",
  "- Aantal trainers: ",
  "- Aantal leerlingen (ongeveer): ",
  "- Telefoonnummer: ",
  "",
  "Wanneer kunnen we even bellen of een demo inplannen?",
  "",
  "Alvast bedankt!",
].join("\r\n");

export const CONTACT_MAILTO = `mailto:${SITE.contactEmail}?subject=${encodeURIComponent(
  MAIL_SUBJECT,
)}&body=${encodeURIComponent(MAIL_BODY)}`;

export const CTA_SECTION = {
  heading: "Klaar om je lessenplanning te automatiseren?",
  sub: "Bel of mail rechtstreeks: we plannen een korte demo in en beantwoorden je vragen.",
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
      href: CONTACT_MAILTO,
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
