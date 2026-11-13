import { describe, expect, it } from "vitest";
import type { AuthUser } from "./auth";
import { organizationToSwitchTo } from "./organization-param";

const user: AuthUser = {
  email: "jan@deaces.be",
  role: "Admin",
  organizationId: "org-a",
  memberships: [
    { organizationId: "org-a", organizationName: "TC De Aces", role: "Admin", isActive: true },
    { organizationId: "org-b", organizationName: "Padel Brugge", role: "Admin", isActive: true },
    { organizationId: "org-c", organizationName: "Oude club", role: "Admin", isActive: false },
  ],
};

describe("organizationToSwitchTo", () => {
  it("wisselt naar een andere organisatie waarvan de gebruiker lid is", () => {
    expect(organizationToSwitchTo("org-b", user)).toBe("org-b");
  });

  it("wisselt niet als de organisatie al actief is", () => {
    expect(organizationToSwitchTo("org-a", user)).toBeNull();
  });

  it("wisselt niet naar een organisatie zonder (actief) lidmaatschap", () => {
    expect(organizationToSwitchTo("org-x", user)).toBeNull();
    expect(organizationToSwitchTo("org-c", user)).toBeNull();
  });

  it("doet niets zonder parameter of zonder gebruiker", () => {
    expect(organizationToSwitchTo(null, user)).toBeNull();
    expect(organizationToSwitchTo("org-b", null)).toBeNull();
  });
});
