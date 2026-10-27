import { describe, it, expect } from "vitest";
import { summarizePackageSlots } from "./camp-packages";
import type { CampDayDto } from "./api/camps";

const day = (date: string): CampDayDto => ({
  id: date,
  date,
  startTime: "09:00",
  endTime: "16:00",
  trainers: [],
  parts: [
    { order: 0, name: "Ochtend", startTime: "09:00", endTime: "12:00" },
    { order: 1, name: "Namiddag", startTime: "13:00", endTime: "16:00" },
  ],
});

describe("summarizePackageSlots", () => {
  it("geeft één regel per dag in datumvolgorde", () => {
    const lines = summarizePackageSlots(
      [day("2026-07-14"), day("2026-07-15")],
      [
        { date: "2026-07-15", partOrder: 0 },
        { date: "2026-07-14", partOrder: 0 },
      ],
    );
    expect(lines).toHaveLength(2);
    expect(lines[0]).toContain("14 juli");
    expect(lines[0]).toContain("Ochtend 09:00-12:00");
    expect(lines[1]).toContain("15 juli");
  });

  it("zet beide dagdelen van één dag op dezelfde regel", () => {
    const lines = summarizePackageSlots(
      [day("2026-07-14")],
      [
        { date: "2026-07-14", partOrder: 1 },
        { date: "2026-07-14", partOrder: 0 },
      ],
    );
    expect(lines).toHaveLength(1);
    expect(lines[0]).toContain("Ochtend 09:00-12:00, Namiddag 13:00-16:00");
  });

  it("slaat slots over die naar een onbekend dagdeel wijzen", () => {
    const lines = summarizePackageSlots(
      [day("2026-07-14")],
      [{ date: "2026-07-20", partOrder: 0 }],
    );
    expect(lines).toEqual([]);
  });
});
