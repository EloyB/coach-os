import {
  CalendarRange,
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

export const SHOWCASE_HEADING =
  "Bekijk waar je administratie naartoe gaat";
export const SHOWCASE_SUB =
  "Vier kernschermen die het verschil maken — van seizoensplanning tot inschrijving op de gsm.";

export const SHOWCASE: ShowcaseItem[] = [
  {
    id: "lesreeksen",
    icon: CalendarRange,
    kicker: "LESREEKSEN",
    heading: "Eén lesreeks. Een heel seizoen vooruit.",
    body: "Stel een terugkerende reeks één keer in. Lessen worden automatisch gegenereerd over de hele periode — maandagen tot mei, zaterdagochtend tot eind juni. Geen Excel-tab per week.",
    bullets: [
      "Wekelijkse, tweewekelijkse of vrije ritmes",
      "Automatische uitzonderingen op feestdagen en clubsluitingen",
      "Capaciteit per slot in één oogopslag — wie staat waar",
    ],
    chrome: "dashboard",
    image: {
      src: "",
      alt: "CoachOS dashboard met een lesreeks-overzicht: weken van een seizoen met lessen per dag en capaciteit per groep.",
      width: 1600,
      height: 1000,
    },
  },
  {
    id: "anonieme-inschrijving",
    icon: ClipboardList,
    kicker: "INSCHRIJVING",
    heading: "Geen accounts. Geen wachtwoorden. Geen drempel.",
    body: "Leerlingen schrijven zich in via een publieke link met enkel hun naam en e-mailadres. Hun voorkeuren komen direct binnen, gekoppeld aan de juiste lesreeks.",
    bullets: [
      "Publieke link per lesreeks, deelbaar via e-mail of socials",
      "Werkt op de gsm — geen app, geen registratie",
      "AVG-conform: enkel wat strikt nodig is voor lesplanning",
    ],
    chrome: "phone",
    image: {
      src: "",
      alt: "Inschrijfformulier op een telefoon: leerling vult naam, e-mailadres en voorkeurstijden in voor een lesreeks.",
      width: 720,
      height: 1520,
    },
  },
  {
    id: "planningsalgoritme",
    icon: BrainCircuit,
    kicker: "PLANNING",
    heading: "Honderden inschrijvingen. Eén klik tot planning.",
    body: "Het algoritme verdeelt leerlingen over weekslots op basis van hun voorkeuren, niveau en groepsverbanden. Handmatige aanpassingen blijven bewaard wanneer je opnieuw plant.",
    bullets: [
      "Voorkeurstijden, niveaus en vaste groepen meegenomen",
      "Drag-and-drop verfijnen na de eerste run",
      "Conflict-detectie bij overlappende reservaties",
    ],
    chrome: "dashboard",
    image: {
      src: "",
      alt: "Planning-overzicht met leerlingen verdeeld over weekslots, met voorkeurmatch-indicatoren per groep.",
      width: 1600,
      height: 1000,
    },
  },
  {
    id: "formulierbouwer",
    icon: FormInput,
    kicker: "FORMULIEREN",
    heading: "Vraag exact wat je nodig hebt — niets meer.",
    body: "Bouw per lesreeks een formulier uit tekstvelden, meerkeuze-opties en ja/nee-vragen. De antwoorden zie je direct bij de inschrijving in het overzicht.",
    bullets: [
      "Tekst, meerkeuze, ja/nee — drie blokken volstaan",
      "Per lesreeks anders: junior-niveau hier, voorkeurstijd daar",
      "Antwoorden zichtbaar op elke leerlingenrij",
    ],
    chrome: "dashboard",
    image: {
      src: "",
      alt: "Formulierbouwer waarin een trainer velden toevoegt aan een inschrijfformulier voor een lesreeks.",
      width: 1600,
      height: 1000,
    },
  },
];
