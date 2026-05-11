import {
  CalendarRange,
  ClipboardList,
  FormInput,
  BrainCircuit,
  MailCheck,
  CreditCard,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

export interface Feature {
  icon: LucideIcon;
  title: string;
  body: string;
}

export const FEATURES: Feature[] = [
  {
    icon: CalendarRange,
    title: "Lessenreeksen",
    body: "Stel een lessenreeks één keer in en laat je agenda automatisch gegenereerd worden.",
  },
  {
    icon: ClipboardList,
    title: "Anonieme inschrijving",
    body: "Leerlingen schrijven zich in via een publieke link — geen account, geen wachtwoord, geen drempel.",
  },
  {
    icon: FormInput,
    title: "Formulierbouwer",
    body: "Voeg per lessenreeks eigen vragen toe: tekstvelden, meerkeuze, voorkeurstijden. Verzamel exact wat je nodig hebt.",
  },
  {
    icon: BrainCircuit,
    title: "Planningsalgoritme",
    body: "Verdeelt leerlingen automatisch over weekslots op basis van hun voorkeuren en groepsverbanden. Handmatige aanpassingen blijven bewaard.",
  },
  {
    icon: MailCheck,
    title: "Bevestiging via magic link",
    body: "Leerlingen bevestigen hun lestijd met één klik in een e-mail. Geen telefoontjes, geen WhatsApp-discussies.",
  },
  {
    icon: CreditCard,
    title: "Betalingen (op de roadmap)",
    body: "Mollie-integratie voor Bancontact en iDEAL. Cash betalingen handmatig registreren kan vandaag al.",
  },
];

export const FEATURES_HEADING =
  "Alles wat je nodig hebt om een lesseizoen te runnen";
export const FEATURES_SUB =
  "Geen losse tools, geen koppelingen onderhouden, geen Excel meer.";
