import { describe, it, expect } from "vitest";
import { buildDays } from "../app/(dashboard)/dashboard/camps/new/_types";
const d = (date: string) => ({
  date,
  morningStart: "09:15",
  morningEnd: "12:00",
  afternoonStart: "13:15",
  afternoonEnd: "16:00",
  trainers: [],
});
describe("buildDays", () => {
  it("houdt geschrapte dagen weg bij uitbreiden", () => {
    const r = buildDays(
      "2026-11-09",
      "2026-11-14",
      [d("2026-11-09"), d("2026-11-11")],
      { start: "2026-11-09", end: "2026-11-11" },
    );
    expect(r.map((x) => x.date)).toEqual(
      [
        "2026-11-09",
        "2026-11-11",
        "2026-11-12",
        "2026-11-13",
        "2026-11-14",
      ].filter((x) => x !== "2026-11-10"),
    );
  });
  it("geeft nieuwe dagen de standaarduren van de dagdelen", () => {
    const parts = {
      morningStart: "10:00",
      morningEnd: "12:30",
      afternoonStart: null,
      afternoonEnd: null,
    };
    const [day] = buildDays("2026-11-09", "2026-11-09", [], null, parts);
    expect(day).toMatchObject(parts);
    expect(day.trainers).toEqual([]);
  });
  it("zonder vorige periode vult alles aan", () => {
    expect(buildDays("2026-11-09", "2026-11-10", [], null)).toHaveLength(2);
  });
});
