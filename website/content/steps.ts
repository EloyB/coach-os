export interface Step {
  num: string;
  title: string;
  body: string;
}

export const STEPS: Step[] = [
  {
    num: "/01",
    title: "Maak een lesreeks aan",
    body: "Bepaal data, prijs en capaciteit. Bouw een eigen inschrijfformulier met alle vragen die jij wil stellen — tekst, meerkeuze, ja/nee.",
  },
  {
    num: "/02",
    title: "Leerlingen schrijven zich in",
    body: "Deel een publieke link. Leerlingen vullen het formulier in zonder account te maken en geven hun voorkeurstijden door.",
  },
  {
    num: "/03",
    title: "Algoritme plant, leerlingen bevestigen",
    body: "CoachOS verdeelt leerlingen over je weekplanning. Iedereen krijgt zijn lestijd via e-mail en bevestigt met één klik.",
  },
];

export const STEPS_HEADING = "Hoe het werkt";
export const STEPS_SUB =
  "Drie stappen — van lege agenda naar bevestigde planning.";
