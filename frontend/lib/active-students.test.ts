import { describe, it, expect } from "vitest";
import { activeStudentsQueryKey, getActiveStudentsUsage } from "./active-students";

describe("getActiveStudentsUsage", () => {
  it("is normaal onder 80%", () => {
    expect(getActiveStudentsUsage(159, 200)).toEqual({ limit: 200, level: "normal", percentage: 79.5 });
  });

  it("geeft een waarschuwing vanaf exact 80%", () => {
    expect(getActiveStudentsUsage(160, 200).level).toBe("warning");
    expect(getActiveStudentsUsage(199, 200).level).toBe("warning");
  });

  it("is overschreden vanaf exact 100%", () => {
    expect(getActiveStudentsUsage(200, 200).level).toBe("exceeded");
  });

  it("begrenst de balk op 100% bij overschrijding", () => {
    expect(getActiveStudentsUsage(250, 200)).toEqual({ limit: 200, level: "exceeded", percentage: 100 });
  });

  it.each([null, undefined, 0, -5, Number.NaN])("behandelt limiet %s als geen limiet", (limit) => {
    expect(getActiveStudentsUsage(500, limit)).toEqual({ limit: null, level: "normal", percentage: 0 });
  });

  it("verwerkt nul leerlingen", () => {
    expect(getActiveStudentsUsage(0, 100)).toEqual({ limit: 100, level: "normal", percentage: 0 });
  });
});

describe("activeStudentsQueryKey", () => {
  it("verschilt per organisatie", () => {
    expect(activeStudentsQueryKey("a")).not.toEqual(activeStudentsQueryKey("b"));
  });
});
