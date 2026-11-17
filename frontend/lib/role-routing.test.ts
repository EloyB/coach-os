import { describe, expect, it } from "vitest";
import {
  homePathFor,
  isStudentRole,
  roleRedirectFor,
  STAFF_HOME,
  STUDENT_HOME,
  SUPER_ADMIN_HOME,
} from "./role-routing";

describe("isStudentRole", () => {
  it("herkent enkel de rol Student", () => {
    expect(isStudentRole("Student")).toBe(true);
    expect(isStudentRole("Admin")).toBe(false);
    expect(isStudentRole("Trainer")).toBe(false);
    expect(isStudentRole(null)).toBe(false);
    expect(isStudentRole(undefined)).toBe(false);
  });
});

describe("homePathFor", () => {
  it("stuurt een student naar zijn lessen", () => {
    expect(homePathFor("Student", false)).toBe(STUDENT_HOME);
  });

  it("stuurt admin en trainer naar het dashboard", () => {
    expect(homePathFor("Admin", false)).toBe(STAFF_HOME);
    expect(homePathFor("Trainer", false)).toBe(STAFF_HOME);
  });

  it("stuurt een super-admin naar het super-admin-dashboard", () => {
    expect(homePathFor("Admin", true)).toBe(SUPER_ADMIN_HOME);
  });

  it("laat de studentrol voorgaan op een achtergebleven super-admin-cookie", () => {
    expect(homePathFor("Student", true)).toBe(STUDENT_HOME);
  });

  it("valt zonder rol terug op de staffkant", () => {
    expect(homePathFor(undefined, false)).toBe(STAFF_HOME);
    expect(homePathFor(null, true)).toBe(SUPER_ADMIN_HOME);
  });
});

describe("roleRedirectFor", () => {
  it.each(["/dashboard", "/dashboard/lessons", "/super-admin", "/super-admin/dashboard", "/verify-email"])(
    "stuurt een student weg van staffpagina %s",
    (path) => {
      expect(roleRedirectFor(path, "Student", false)).toBe(STUDENT_HOME);
    },
  );

  it("laat een student op zijn eigen pagina's", () => {
    expect(roleRedirectFor("/student/lessons", "Student", false)).toBeNull();
    expect(roleRedirectFor("/student-login", "Student", false)).toBeNull();
  });

  it.each(["Admin", "Trainer"])("stuurt %s weg van de studentpagina naar het dashboard", (role) => {
    expect(roleRedirectFor("/student/lessons", role, false)).toBe(STAFF_HOME);
  });

  it("stuurt een super-admin weg van de studentpagina naar zijn eigen dashboard", () => {
    expect(roleRedirectFor("/student/lessons", "Admin", true)).toBe(SUPER_ADMIN_HOME);
  });

  it("laat staff op staffpagina's en verwart /student-login niet met de studentkant", () => {
    expect(roleRedirectFor("/dashboard", "Admin", false)).toBeNull();
    expect(roleRedirectFor("/verify-email", "Trainer", false)).toBeNull();
    expect(roleRedirectFor("/student-login", "Admin", false)).toBeNull();
    expect(roleRedirectFor("/studenten", "Admin", false)).toBeNull();
  });

  it("beslist niets zonder bekende rol", () => {
    expect(roleRedirectFor("/student/lessons", undefined, false)).toBeNull();
    expect(roleRedirectFor("/dashboard", "", false)).toBeNull();
  });
});
