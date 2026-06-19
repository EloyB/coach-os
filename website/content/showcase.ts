import {
  CalendarRange,
  Tent,
  ClipboardList,
  BrainCircuit,
  FormInput,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

export type ChromeVariant = "dashboard" | "phone";

export interface ShowcaseImage {
  /**
   * Path under /public. Leave empty until the screenshot is ready —
   * the frame will render a labeled placeholder showing the expected filename.
   */
  src: string;
  alt: string;
  /** Intrinsic image dimensions, used for aspect ratio + next/image. */
  width: number;
  height: number;
}

export interface ShowcaseItem {
  id: string;
  icon: LucideIcon;
  kicker: string;
  heading: string;
  body: string;
  bullets: string[];
  chrome: ChromeVariant;
  image: ShowcaseImage;
}

export const SHOWCASE_HEADING = "Overzichtelijk, eenvoudig en intuïtief";
export const SHOWCASE_SUB =
  "Eén gebruiksvriendelijke tool voor inschrijven, plannen en betalen.";

export const SHOWCASE: ShowcaseItem[] = [
  {
    id: "lessenreeksen",
    icon: CalendarRange,
    kicker: "LESREEKSEN",
    heading: "Eén lessenreeks. Een heel seizoen vooruit.",
    body: "Stel een lessenreeks één keer in en laat je agenda automatisch vullen. — elke maandag voor de komende 8 weken, zaterdagochtenden tot eind juni? CoachOS plant het naar jouw wensen.",
    bullets: [
      "Wekelijkse, tweewekelijkse of losse trainingen",
      "Voor 1 of meerdere trainers",
      "Aantal deelnemers naar keuze",
    ],
    chrome: "dashboard",
    image: {
      src: "",
      alt: "CoachOS dashboard met een lessenreeks-overzicht: weken van een seizoen met lessen per dag en capaciteit per groep.",
      width: 1600,
      height: 1000,
    },
  },
  {
    id: "tenniskampen",
    icon: Tent,
    kicker: "KAMPEN",
    heading: "Meerdaagse kampen en stages, zonder gedoe.",
    body: "Naast lessenreeksen organiseer je ook tenniskampen en stages: een aaneengesloten periode waarvoor spelers zich eenmalig inschrijven. Stel per dag de uren in en wijs trainers toe met hun eigen aanwezigheidsuren.",
    bullets: [
      "Een kamp over meerdere dagen, met eigen uren per dag",
      "Meerdere trainers per kamp, elk met hun eigen uren per dag",
      "Publieke inschrijving en betaling: cash of online",
    ],
    chrome: "dashboard",
    image: {
      src: "",
      alt: "CoachOS dashboard met een tenniskamp: meerdere dagen met kampuren en de toegewezen trainers per dag.",
      width: 1600,
      height: 1000,
    },
  },
  {
    id: "formulierbouwer",
    icon: FormInput,
    kicker: "FORMULIEREN",
    heading: "Vraag exact wat je nodig hebt.",
    body: "Bouw per lessenreeks een formulier uit tekstvelden, meerkeuze-opties en ja/nee-vragen. De antwoorden zie je direct bij de inschrijving in het overzicht.",
    bullets: [
      "Personaliseer je formulier: tekst, meerkeuze, ja/nee",
      "Per lessenreeks aanpasbaar",
      "Bekijk de antwoorden in een handig overzicht",
    ],
    chrome: "dashboard",
    image: {
      src: "",
      alt: "Formulierbouwer waarin een trainer velden toevoegt aan een inschrijvingsformulier voor een lessenreeks.",
      width: 1600,
      height: 1000,
    },
  },
  {
    id: "anonieme-inschrijving",
    icon: ClipboardList,
    kicker: "INSCHRIJVING",
    heading: "Geen accounts. Geen wachtwoorden. Geen drempel.",
    body: "Leerlingen schrijven zich eenvoudig in via een link naar het inschrijvingsformulier en ontvangen een automatische bevestiging per mail.",
    bullets: [
      "Unieke link per lessenreeks, deelbaar via e-mail of socials",
      "Werkt op je smartphone zonder app of registratie",
      "GDPR-conform: enkel wat strikt nodig is voor lessenplanning",
    ],
    chrome: "phone",
    image: {
      src: "",
      alt: "Inschrijvingsformulier op een telefoon: leerling vult naam, e-mailadres en voorkeurstijden in voor een lessenreeks.",
      width: 720,
      height: 1520,
    },
  },
  {
    id: "planningsalgoritme",
    icon: BrainCircuit,
    kicker: "PLANNING",
    heading: "Honderden inschrijvingen. Eén klik en je planning is klaar.",
    body: "De automatische planningstool verdeelt leerlingen over slots op basis van hun beschikbaarheden, voorkeuren en niveau. De planning kan steeds handmatig worden aangepast waar nodig.",
    bullets: [
      "Rekening houdend met beschikbaarheden, niveaus en groepen",
      "Altijd mogelijkheid tot handmatig verfijnen",
      "Conflict-detectie bij beperkte beschikbaarheden",
    ],
    chrome: "dashboard",
    image: {
      src: "",
      alt: "Planning-overzicht met leerlingen verdeeld over weekslots, met voorkeurmatch-indicatoren per groep.",
      width: 1600,
      height: 1000,
    },
  },
];
