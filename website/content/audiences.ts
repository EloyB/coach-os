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
    title: "Tennisscholen",
    body: "Van eerste inschrijving tot laatste les van het seizoen — één dashboard voor je hele school.",
    bullets: [
      "Lessenreeksen voor jeugd en volwassenen",
      "Onbeperkt aantal banen en trainers",
      "Magic-link bevestigingen per leerling",
    ],
    slug: "voor-tennisscholen",
  },
  {
    icon: Building2,
    title: "Padelscholen",
    body: "Mobile-first inschrijvingen voor je spelers, automatische groepering op niveau achter de schermen.",
    bullets: [
      "Geen app of account voor leden",
      "Voorkeur- en niveaumatch",
      "Schaalt mee met snelle groei",
    ],
    slug: "voor-padelscholen",
  },
  {
    icon: User,
    title: "Zelfstandige trainers",
    body: "Eén tool voor inschrijvingen, planning en bevestigingen. Lesgeven in plaats van administreren.",
    bullets: [
      "Publieke inschrijflink per reeks",
      "Meerdere scholen via één account",
      "Gratis tijdens pilot",
    ],
    slug: "voor-trainers",
  },
];

export const AUDIENCES_HEADING = "Voor alle tennis- en padeltrainers?";
export const AUDIENCES_SUB =
  "Of je nu een school met 10 trainers runt of solo lesgeeft — CoachOS doet het werk.";
