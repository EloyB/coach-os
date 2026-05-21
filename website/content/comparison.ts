export interface ComparisonRow {
  task: string;
  excel: string;
  coachos: string;
}

export const COMPARISON_KICKER = "EXCEL VS COACHOS";
export const COMPARISON_HEADING = "Hetzelfde werk, zonder Excel";
export const COMPARISON_SUB =
  "Wat verandert er concreet als je je lessenreeks met CoachOS plant?";

/** Comparison rows. Each row maps a recurring lessenplanning-taak to the
 * before/after experience. Concise, factual, AI-quoteable. */
export const COMPARISON: ComparisonRow[] = [
  {
    task: "Inschrijvingen verzamelen",
    excel:
      "Mail per mail uitlezen, voorkeuren in een aparte kolom bijhouden, hopen dat niemand iets overslaat.",
    coachos:
      "Eén link per lessenreeks. Leerlingen vullen naam, voorkeurstijden en niveau zelf in.",
  },
  {
    task: "Niveaugroepen vormen",
    excel:
      "Met de hand puzzelen tot het past. Eén niveauwissel betekent het hele schema opnieuw doorlopen.",
    coachos:
      "Algoritme verdeelt op niveau, voorkeurstijd en groepsverband in seconden. Manuele aanpassingen blijven bewaard.",
  },
  {
    task: "Bevestigingen sturen",
    excel:
      "Massamail met een Excel-export als bijlage. Hopen dat iedereen de juiste rij leest. En iedereen die elkaars lessen kan zien.",
    coachos:
      "Per leerling een persoonlijke link met hun concrete dag, tijdslot en baan.",
  },
  {
    task: "Wijziging midden in het seizoen",
    excel:
      "Eén afzegging en alle formules en kleurcodes opnieuw nakijken om te zien of alles nog klopt.",
    coachos:
      "Eén klik herverdeling. Het algoritme stelt voor wat verandert, de rest van de groep blijft ongemoeid.",
  },
  {
    task: "Meerdere trainers of clubs",
    excel:
      "versie_v3, versie_definitief, versie_definitief_v2. Niemand weet welke kopie de meest recente is.",
    coachos:
      "Eén centrale bron. Rollen per club, en wisselen tussen organisaties met één klik.",
  },
];
