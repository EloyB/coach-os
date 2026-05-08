import { Building2, Trophy, User } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { PersonaSlug } from "@/content/personas";

export interface Audience {
  icon: LucideIcon;
  title: string;
  body: string;
  bullets: string[];
  /** Slug of the persona page this card links to. */
  slug: PersonaSlug;
}

export const AUDIENCES: Audience[] = [
  {
    icon: Trophy,
    title: "Tennisclubs",
    body: "Van eerste inschrijving tot laatste les van het seizoen — één dashboard voor je hele club.",
    bullets: [
      "Lesreeksen voor jeugd en volwassenen",
      "Onbeperkt aantal banen en trainers",
      "Magic-link bevestigingen per leerling",
    ],
    slug: "voor-tennisclubs",
  },
  {
    icon: Building2,
    title: "Padelclubs",
    body: "Mobile-first inschrijvingen voor je spelers, automatische groepering op niveau achter de schermen.",
    bullets: [
      "Geen app of account voor leden",
      "Voorkeur- en niveaumatch",
      "Schaalt mee met snelle groei",
    ],
    slug: "voor-padelclubs",
  },
  {
    icon: User,
    title: "Zelfstandige trainers",
    body: "Eén tool voor inschrijvingen, planning en bevestigingen. Lesgeven in plaats van administreren.",
    bullets: [
      "Publieke inschrijflink per reeks",
      "Multi-club via één account",
      "Gratis tijdens pilot",
    ],
    slug: "voor-trainers",
  },
];

export const AUDIENCES_HEADING = "Voor wie is CoachOS gemaakt?";
export const AUDIENCES_SUB =
  "Of je nu een club met tien trainers runt of solo lesgeeft — CoachOS schaalt mee.";
