import { Building2, User } from "lucide-react";
import type { LucideIcon } from "lucide-react";

export interface Audience {
  icon: LucideIcon;
  title: string;
  body: string;
  bullets: string[];
}

export const AUDIENCES: Audience[] = [
  {
    icon: Building2,
    title: "Tennis- en padelclubs",
    body: "Eén dashboard voor het hele lesseizoen — van eerste inschrijving tot laatste les.",
    bullets: [
      "Meerdere trainers per club",
      "Centrale ledenadministratie",
      "Multi-club via één account",
    ],
  },
  {
    icon: User,
    title: "Zelfstandige coaches",
    body: "Geen IT-rompslomp. Maak je eigen lesreeksen en laat leerlingen zichzelf inschrijven.",
    bullets: [
      "Publieke inschrijflink",
      "Eigen formulier per reeks",
      "Geen accounts voor leerlingen",
    ],
  },
];

export const AUDIENCES_HEADING = "Voor wie is CoachOS gemaakt?";
export const AUDIENCES_SUB =
  "Of je nu een club met tien trainers runt of solo lesgeeft — CoachOS schaalt mee.";
