import { describe, it, expect } from "vitest";
import { subscriptionStatusQueryKey } from "./subscription-query";

describe("subscriptionStatusQueryKey", () => {
  it("verschilt per organisatie, zodat de banner van org A nooit voor org B getoond wordt", () => {
    expect(subscriptionStatusQueryKey("org-a")).not.toEqual(subscriptionStatusQueryKey("org-b"));
  });

  it("is stabiel voor dezelfde organisatie, zodat de cache hergebruikt wordt", () => {
    expect(subscriptionStatusQueryKey("org-a")).toEqual(subscriptionStatusQueryKey("org-a"));
  });

  it("valt onder de prefix ['subscription'], zodat invalidatie van alle abonnementsdata blijft werken", () => {
    expect(subscriptionStatusQueryKey("org-a").slice(0, 1)).toEqual(["subscription"]);
  });
});
