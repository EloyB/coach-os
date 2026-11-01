import { describe, it, expect } from "vitest";
import {
  availableOptions,
  calcTotal,
  describeSelection,
  isWholeCamp,
  optionOf,
  selectionFor,
  wholeCampSelection,
  type PickerDay,
} from "./camp-day-selection";

const full = (id: string): PickerDay => ({
  id,
  date: "2026-12-21",
  morningStart: "09:15",
  morningEnd: "12:00",
  afternoonStart: "13:15",
  afternoonEnd: "16:00",
  morningFull: false,
  afternoonFull: false,
});
const morningOnly = (id: string): PickerDay => ({
  ...full(id),
  afternoonStart: null,
  afternoonEnd: null,
});

describe("calcTotal", () => {
  it("rekent hele dag en halve dag volgens de prijsregel", () => {
    const total = calcTotal(
      [
        { campDayId: "a", morning: true, afternoon: true },
        { campDayId: "b", morning: true, afternoon: true },
        { campDayId: "c", morning: true, afternoon: true },
        { campDayId: "d", morning: true, afternoon: false },
      ],
      32.5,
      20,
    );
    expect(total).toBe(117.5);
  });
  it("is 0 zonder keuze", () => expect(calcTotal([], 32.5, 20)).toBe(0));
});

describe("availableOptions", () => {
  it("biedt bij halve dagen voormiddag, namiddag en hele dag", () => {
    expect(availableOptions(full("a"), true)).toEqual(["morning", "afternoon", "full"]);
  });
  it("biedt zonder halve dagen enkel de hele dag", () => {
    expect(availableOptions(full("a"), false)).toEqual(["full"]);
  });
  it("biedt voor een dag met één dagdeel enkel dat dagdeel", () => {
    expect(availableOptions(morningOnly("a"), true)).toEqual(["morning"]);
    expect(availableOptions(morningOnly("a"), false)).toEqual(["morning"]);
  });
  it("laat een vol dagdeel weg en de hele dag erbij", () => {
    const day = { ...full("a"), morningFull: true };
    expect(availableOptions(day, true)).toEqual(["afternoon"]);
  });
  it("biedt niets als alle dagdelen vol zijn", () => {
    expect(availableOptions({ ...full("a"), morningFull: true, afternoonFull: true }, true)).toEqual([]);
  });
});

describe("selectionFor / optionOf", () => {
  it("zet een optie om naar een keuze en terug", () => {
    expect(selectionFor(full("a"), "full")).toEqual({ campDayId: "a", morning: true, afternoon: true });
    expect(selectionFor(full("a"), "afternoon")).toEqual({ campDayId: "a", morning: false, afternoon: true });
    expect(selectionFor(full("a"), "none")).toBeNull();
    expect(optionOf({ campDayId: "a", morning: true, afternoon: true })).toBe("full");
    expect(optionOf({ campDayId: "a", morning: true, afternoon: false })).toBe("morning");
    expect(optionOf(undefined)).toBe("none");
  });
});

describe("wholeCampSelection", () => {
  it("kiest elke dag volledig, ook een dag met één dagdeel", () => {
    const days = [full("a"), morningOnly("b")];
    expect(wholeCampSelection(days, true)).toEqual([
      { campDayId: "a", morning: true, afternoon: true },
      { campDayId: "b", morning: true, afternoon: false },
    ]);
  });
  it("slaat dagen met een vol dagdeel over als halve dagen niet mogen", () => {
    const days = [full("a"), { ...full("b"), morningFull: true }];
    expect(wholeCampSelection(days, false)).toEqual([
      { campDayId: "a", morning: true, afternoon: true },
    ]);
  });
  it("neemt bij halve dagen enkel het vrije dagdeel van een deels volle dag", () => {
    const days = [{ ...full("a"), morningFull: true }];
    expect(wholeCampSelection(days, true)).toEqual([
      { campDayId: "a", morning: false, afternoon: true },
    ]);
  });
  it("herkent een keuze als heel kamp", () => {
    const days = [full("a"), morningOnly("b")];
    expect(isWholeCamp(wholeCampSelection(days, true), days, true)).toBe(true);
    expect(isWholeCamp([{ campDayId: "a", morning: true, afternoon: true }], days, true)).toBe(false);
  });
});

describe("describeSelection", () => {
  it("beschrijft de keuze per dag op datum gesorteerd", () => {
    const days = [
      { id: "a", date: "2026-12-22" },
      { id: "b", date: "2026-12-21" },
    ];
    expect(
      describeSelection(
        [
          { campDayId: "a", morning: true, afternoon: false },
          { campDayId: "b", morning: true, afternoon: true },
          { campDayId: "x", morning: true, afternoon: true },
        ],
        days,
        { morning: "Voormiddag", afternoon: "Namiddag", wholeDay: "Hele dag" },
      ),
    ).toEqual(["21/12 hele dag", "22/12 voormiddag"]);
  });
});
