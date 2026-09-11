import { describe, it, expect } from "vitest";
import { collapseParallelPrefs, MIXED_PREF } from "./preferences";

describe("collapseParallelPrefs", () => {
  it("geeft de gedeelde waarde als alle parallelle banen gelijk zijn", () => {
    expect(collapseParallelPrefs(["Preferred", "Preferred", "Preferred"])).toBe(
      "Preferred"
    );
    expect(collapseParallelPrefs([2, 2])).toBe(2);
  });

  it("geeft null als er geen voorkeuren zijn", () => {
    expect(collapseParallelPrefs([])).toBeNull();
    expect(collapseParallelPrefs([undefined, null])).toBeNull();
  });

  it("negeert ontbrekende banen en vouwt de rest samen", () => {
    expect(
      collapseParallelPrefs(["Available", undefined, "Available"])
    ).toBe("Available");
  });

  // Regressie: legacy-inschrijving met verschillende voorkeur per baan (zelfde uur)
  // mag NIET stilzwijgend als de eerste waarde tonen.
  it("geeft 'mixed' als parallelle banen het oneens zijn", () => {
    expect(collapseParallelPrefs(["Preferred", "Unavailable"])).toBe(MIXED_PREF);
    expect(collapseParallelPrefs([2, 1, 3])).toBe(MIXED_PREF);
    expect(collapseParallelPrefs(["Available", null, "Preferred"])).toBe(
      MIXED_PREF
    );
  });
});
