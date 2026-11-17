/** Kleurklassen per club-index: 0 = "elke club" (groen), daarna een vaste reeks die herhaalt. */
const COLOR_CLASSES = [
  "bg-tennis-green/80 border-tennis-green text-white",
  "bg-blue-500/80 border-blue-600 text-white",
  "bg-amber-500/80 border-amber-600 text-white",
  "bg-purple-500/80 border-purple-600 text-white",
  "bg-rose-500/80 border-rose-600 text-white",
  "bg-teal-500/80 border-teal-600 text-white",
];

const DOT_CLASSES = [
  "bg-tennis-green",
  "bg-blue-500",
  "bg-amber-500",
  "bg-purple-500",
  "bg-rose-500",
  "bg-teal-500",
];

export function blockColorClass(colorIndex: number): string {
  return COLOR_CLASSES[colorIndex % COLOR_CLASSES.length];
}

export function dotColorClass(colorIndex: number): string {
  return DOT_CLASSES[colorIndex % DOT_CLASSES.length];
}
