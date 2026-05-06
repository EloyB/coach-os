export const CTA_SECTION = {
  heading: "Klaar om je lesplanning te automatiseren?",
  sub: "Plan een korte demo in waarin we CoachOS laten zien voor jouw situatie, of stel je vraag direct via het contactformulier.",
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
