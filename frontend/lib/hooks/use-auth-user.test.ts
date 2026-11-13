import { describe, expect, it } from "vitest";
import { createAuthUserSnapshot } from "./use-auth-user";
import {
  canEditEnrollment,
  isEnrollmentManager,
  isHeadTrainerViewer,
  parseAuthUser,
  type AuthUser,
} from "../auth";

describe("createAuthUserSnapshot", () => {
  it("geeft null zonder opgeslagen gebruiker", () => {
    const snapshot = createAuthUserSnapshot(() => null);
    expect(snapshot()).toBeNull();
  });

  it("geeft dezelfde referentie zolang de ruwe waarde gelijk blijft", () => {
    const raw = JSON.stringify({ email: "a@b.be", role: "Admin" });
    const snapshot = createAuthUserSnapshot(() => raw);
    const first = snapshot();
    expect(first).toEqual({ email: "a@b.be", role: "Admin" });
    expect(snapshot()).toBe(first);
  });

  it("parset opnieuw als de ruwe waarde wijzigt", () => {
    let raw: string | null = JSON.stringify({ email: "a@b.be", role: "Admin" });
    const snapshot = createAuthUserSnapshot(() => raw);
    const first = snapshot();
    raw = JSON.stringify({ email: "a@b.be", role: "Trainer" });
    const second = snapshot();
    expect(second).not.toBe(first);
    expect(second?.role).toBe("Trainer");
    raw = null;
    expect(snapshot()).toBeNull();
  });

  it("geeft null bij ongeldige JSON", () => {
    const snapshot = createAuthUserSnapshot(() => "{kapot");
    expect(snapshot()).toBeNull();
  });
});

describe("parseAuthUser", () => {
  it("geeft null voor een lege string", () => {
    expect(parseAuthUser("")).toBeNull();
  });
});

describe("rolhelpers met expliciete gebruiker", () => {
  const admin: AuthUser = { email: "a@b.be", role: "Admin" };
  const headTrainer: AuthUser = { email: "h@b.be", role: "Trainer", headTrainerClubIds: ["club-1"] };
  const trainer: AuthUser = { email: "t@b.be", role: "Trainer", headTrainerClubIds: [] };

  it("isHeadTrainerViewer herkent enkel trainers met hoofdtrainer-clubs", () => {
    expect(isHeadTrainerViewer(headTrainer)).toBe(true);
    expect(isHeadTrainerViewer(trainer)).toBe(false);
    expect(isHeadTrainerViewer(admin)).toBe(false);
    expect(isHeadTrainerViewer(null)).toBe(false);
  });

  it("isEnrollmentManager geldt voor admins en hoofdtrainers", () => {
    expect(isEnrollmentManager(admin)).toBe(true);
    expect(isEnrollmentManager(headTrainer)).toBe(true);
    expect(isEnrollmentManager(trainer)).toBe(false);
    expect(isEnrollmentManager(null)).toBe(false);
  });

  it("canEditEnrollment geldt enkel voor admins", () => {
    expect(canEditEnrollment(admin)).toBe(true);
    expect(canEditEnrollment(headTrainer)).toBe(false);
    expect(canEditEnrollment(null)).toBe(false);
  });
});
