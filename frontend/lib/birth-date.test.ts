import { describe, expect, it } from "vitest";
import { formatTodayAsIsoDate, getBirthDateError } from "./birth-date";

const today = new Date(2026, 10, 19, 15, 30);

describe("getBirthDateError", () => {
  it("vraagt om een waarde", () => {
    expect(getBirthDateError("", today)).toBe("required");
    expect(getBirthDateError("   ", today)).toBe("required");
  });

  it("weigert een ongeldige datum", () => {
    expect(getBirthDateError("geen-datum", today)).toBe("invalid");
  });

  it("weigert een datum in de toekomst maar aanvaardt vandaag", () => {
    expect(getBirthDateError("2026-11-20", today)).toBe("future");
    expect(getBirthDateError("2026-11-19", today)).toBeNull();
  });

  it("weigert een onrealistisch oude datum", () => {
    expect(getBirthDateError("1900-01-01", today)).toBe("implausible");
  });

  it("aanvaardt een gewone geboortedatum", () => {
    expect(getBirthDateError("2015-06-01", today)).toBeNull();
  });
});

describe("formatTodayAsIsoDate", () => {
  it("geeft yyyy-MM-dd in lokale tijd", () => {
    expect(formatTodayAsIsoDate(today)).toBe("2026-11-19");
  });
});
