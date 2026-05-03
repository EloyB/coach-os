export const CTA_SECTION = {
  heading: "Klaar om je lesplanning te automatiseren?",
  sub: "Zet jezelf op de wachtlijst voor vroege toegang, of neem direct contact op voor een persoonlijk gesprek.",
  tabs: {
    waitlist: "Op de wachtlijst",
    contact: "Neem contact op",
  },
  waitlist: {
    title: "Vroege toegang",
    body: "Geef je e-mail door en we sturen je een berichtje zodra CoachOS open gaat voor jouw type organisatie.",
    submit: "Inschrijven",
    success: "Bedankt — je staat op de wachtlijst.",
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
