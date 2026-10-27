import { describe, it, expect } from "vitest";
import { buildDays } from "../app/(dashboard)/dashboard/camps/new/_types";
const d = (date: string) => ({
  date,
  startTime: "09:00",
  endTime: "16:00",
  trainers: [],
  parts: [],
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
  it("zonder vorige periode vult alles aan", () => {
    expect(buildDays("2026-11-09", "2026-11-10", [], null)).toHaveLength(2);
  });
});
