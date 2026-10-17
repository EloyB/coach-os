import { describe, it, expect } from "vitest";
import {
  canPayByBankTransfer,
  exceedsLimit,
  formatEuro,
  grossAmount,
  isValidBelgianVat,
  planPrice,
  suggestPlan,
  type PlanOption,
} from "./billing";

const plans: PlanOption[] = [
  { plan: 1, name: "Game", monthlyPrice: 25, yearlyPrice: 250, studentLimit: 60 },
  { plan: 2, name: "Set", monthlyPrice: 49, yearlyPrice: 490, studentLimit: 200 },
  { plan: 3, name: "Match", monthlyPrice: 89, yearlyPrice: 890, studentLimit: 500 },
];

describe("grossAmount", () => {
  it("telt 21% btw op en rondt af op centen", () => {
    expect(grossAmount(25)).toBe(30.25);
    expect(grossAmount(49)).toBe(59.29);
    expect(grossAmount(890)).toBe(1076.9);
  });
});

describe("suggestPlan", () => {
  it("kiest het kleinste plan dat het aantal leerlingen dekt", () => {
    expect(suggestPlan(plans, 0).name).toBe("Game");
    expect(suggestPlan(plans, 60).name).toBe("Game");
    expect(suggestPlan(plans, 70).name).toBe("Set");
  });

  it("valt terug op het grootste plan boven de hoogste limiet", () => {
    expect(suggestPlan(plans, 600).name).toBe("Match");
  });
});

describe("exceedsLimit", () => {
  it("laat 10% marge boven de limiet toe", () => {
    expect(exceedsLimit(plans[0], 66)).toBe(false);
    expect(exceedsLimit(plans[0], 67)).toBe(true);
  });
});

describe("canPayByBankTransfer", () => {
  it("kan enkel bij een jaarabonnement", () => {
    expect(canPayByBankTransfer("Yearly")).toBe(true);
    expect(canPayByBankTransfer("Monthly")).toBe(false);
  });
});

describe("planPrice", () => {
  it("geeft de prijs excl. btw voor het gekozen interval", () => {
    expect(planPrice(plans[1], "Monthly")).toBe(49);
    expect(planPrice(plans[1], "Yearly")).toBe(490);
  });
});

describe("formatEuro", () => {
  it("toont bedragen in Belgische notatie", () => {
    expect(formatEuro(1076.9)).toMatch(/^€\s?1\.076,90$/);
    expect(formatEuro(30.25)).toMatch(/^€\s?30,25$/);
  });
});

describe("isValidBelgianVat", () => {
  it("aanvaardt Belgische ondernemingsnummers met of zonder BE, punten of spaties", () => {
    expect(isValidBelgianVat("BE0123456789")).toBe(true);
    expect(isValidBelgianVat("0123.456.789")).toBe(true);
    expect(isValidBelgianVat("be 1234 567 890")).toBe(true);
  });

  it("weigert andere vormen", () => {
    expect(isValidBelgianVat("BE2123456789")).toBe(false);
    expect(isValidBelgianVat("BE012345678")).toBe(false);
    expect(isValidBelgianVat("NL123456789B01")).toBe(false);
    expect(isValidBelgianVat("")).toBe(false);
  });
});
