import { describe, it, expect } from "vitest";
import messages from "../messages/nl.json";
import { paymentMethodDisplay } from "./payment-method";

describe("paymentMethodDisplay", () => {
  it.each([
    ["Online", "paymentMethodOnline"],
    ["Cash", "paymentMethodCash"],
    ["Transfer", "paymentMethodTransfer"],
  ])("geeft voor %s het label %s", (method, labelKey) => {
    expect(paymentMethodDisplay(method)?.labelKey).toBe(labelKey);
  });

  it("laat cash opvallen tegenover online", () => {
    expect(paymentMethodDisplay("Cash")?.className).not.toBe(
      paymentMethodDisplay("Online")?.className,
    );
  });

  it.each([[null], [undefined], [""], ["Onbekend"]])(
    "toont niets zonder gekende betaalmethode (%s)",
    (method) => {
      expect(paymentMethodDisplay(method)).toBeUndefined();
    },
  );
});

describe("benaming van de manuele betaaloptie", () => {
  const term = "Cash of overschrijving";

  it("is overal dezelfde voor club en deelnemer", () => {
    expect(messages.lessonWizard.paymentMethods.manual).toBe(term);
    expect(messages.enrollmentDetail.paymentMethodCash).toBe(term);
    expect(messages.enrollmentsTable.paymentMethodCash).toBe(term);
    expect(messages.confirmation.cash.startsWith(term)).toBe(true);
  });
});
