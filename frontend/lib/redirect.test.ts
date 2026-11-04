import { describe, expect, it } from "vitest";
import { loginPathFor, safeRedirectPath } from "./redirect";

describe("safeRedirectPath", () => {
  it("accepteert een interne pagina met query", () => {
    expect(safeRedirectPath("/dashboard/settings/subscription?return=1")).toBe("/dashboard/settings/subscription?return=1");
  });

  it("weigert externe en protocol-relatieve adressen", () => {
    expect(safeRedirectPath("https://evil.example")).toBeNull();
    expect(safeRedirectPath("//evil.example")).toBeNull();
    expect(safeRedirectPath("/\\evil.example")).toBeNull();
    expect(safeRedirectPath("javascript:alert(1)")).toBeNull();
  });

  it("geeft null zonder waarde", () => {
    expect(safeRedirectPath(null)).toBeNull();
    expect(safeRedirectPath("")).toBeNull();
  });
});

describe("loginPathFor", () => {
  it("bewaart pad en query als redirect", () => {
    expect(loginPathFor("/dashboard/settings/subscription", "?return=1"))
      .toBe("/login?redirect=%2Fdashboard%2Fsettings%2Fsubscription%3Freturn%3D1");
  });

  it("stuurt vanaf de loginpagina gewoon naar /login", () => {
    expect(loginPathFor("/login", "?redirect=%2Fdashboard")).toBe("/login");
  });
});
