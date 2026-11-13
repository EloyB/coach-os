import { test, expect, type Page, type Route } from "@playwright/test";
import type { StudentLesson } from "@/lib/api/student";
import type { AuthUser } from "@/lib/auth";
import { API_URL, TEST_USER, mockAllDashboardApis, mockApi } from "./helpers";

/**
 * Issue #358: wie op de pagina van de andere rol belandt, gaat naar zijn eigen startpagina,
 * zonder uitloggen en zonder heen-en-weer tussen middleware en layout.
 */

const STAFF_STORAGE_USER: AuthUser = {
  userId: TEST_USER.userId,
  email: TEST_USER.email,
  firstName: TEST_USER.firstName,
  lastName: TEST_USER.lastName,
  organizationId: TEST_USER.organizationId,
  role: "Admin",
};

const STUDENT_STORAGE_USER: AuthUser = { email: "emma.claes@gmail.com", role: "Student" };

const STUDENT_LESSONS: StudentLesson[] = [];

interface SessionOptions {
  /** Gebruiker in localStorage; `null` = geen localStorage-sessie (enkel cookies). */
  user: AuthUser | null;
  /** Cookies die de middleware leest. */
  cookies: Partial<Record<"has_token" | "user_role" | "has_super_admin", string>>;
}

/** Zet een sessie zonder de middleware te passeren: eerst een publieke pagina zonder cookies. */
async function setSession(page: Page, { user, cookies }: SessionOptions): Promise<void> {
  await page.goto("/student-login");
  if (user) {
    await page.evaluate(
      ({ token, u }) => {
        localStorage.setItem("token", token);
        localStorage.setItem("auth_user", JSON.stringify(u));
      },
      { token: TEST_USER.token, u: user },
    );
  }
  const url = new URL(page.url()).origin;
  await page
    .context()
    .addCookies(Object.entries(cookies).map(([name, value]) => ({ name, value: value ?? "", url })));
}

/**
 * Elke API-call die niet expliciet gemockt is, krijgt een 401. Zo bewijzen we dat een student
 * geen staff-endpoint aanroept: de api-client zou hem bij een 401 meteen uitloggen.
 */
async function rejectUnmockedApiCalls(page: Page): Promise<void> {
  await page.route(`${API_URL}/**`, (route: Route) =>
    route.fulfill({ status: 401, contentType: "application/json", body: "[]" }),
  );
}

async function cookieValue(page: Page, name: string): Promise<string | undefined> {
  const cookies = await page.context().cookies();
  return cookies.find((c) => c.name === name)?.value;
}

async function expectStudentLessons(page: Page): Promise<void> {
  await expect(page).toHaveURL(/\/student\/lessons$/);
  await expect(page.getByText("Mijn lessen", { exact: true })).toBeVisible();
  // Niet uitgelogd: token staat er nog.
  expect(await page.evaluate(() => localStorage.getItem("token"))).toBe(TEST_USER.token);
}

async function expectStaffDashboard(page: Page): Promise<void> {
  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole("heading", { name: "Vandaag", level: 1 })).toBeVisible();
}

test.describe("Rolgebaseerde routing — student op staffpagina", () => {
  test.beforeEach(async ({ page }) => {
    await rejectUnmockedApiCalls(page);
    await mockApi(page, "GET", "/student/lessons", STUDENT_LESSONS);
  });

  for (const path of ["/dashboard", "/dashboard/lessons", "/super-admin/dashboard", "/verify-email"]) {
    test(`student op ${path} gaat naar zijn lessen zonder uitloggen`, async ({ page }) => {
      await setSession(page, {
        user: STUDENT_STORAGE_USER,
        cookies: { has_token: "1", user_role: "Student" },
      });

      await page.goto(path);

      await expectStudentLessons(page);
    });
  }

  test("student zonder rolcookie (oudere sessie) op /dashboard gaat via de layout naar zijn lessen", async ({ page }) => {
    await setSession(page, { user: STUDENT_STORAGE_USER, cookies: { has_token: "1" } });

    await page.goto("/dashboard");

    await expectStudentLessons(page);
    expect(await cookieValue(page, "user_role")).toBe("Student");
  });

  test("student op /verify-email zonder rolcookie gaat naar zijn lessen", async ({ page }) => {
    await setSession(page, { user: STUDENT_STORAGE_USER, cookies: { has_token: "1" } });

    await page.goto("/verify-email");

    await expectStudentLessons(page);
  });

  test("ingelogde student op /login gaat naar zijn lessen", async ({ page }) => {
    await setSession(page, {
      user: STUDENT_STORAGE_USER,
      cookies: { has_token: "1", user_role: "Student" },
    });

    await page.goto("/login");

    await expectStudentLessons(page);
  });
});

test.describe("Rolgebaseerde routing — staff op studentpagina", () => {
  test.beforeEach(async ({ page }) => {
    await mockAllDashboardApis(page);
  });

  const staffCases: { name: string; user: AuthUser }[] = [
    { name: "admin", user: STAFF_STORAGE_USER },
    { name: "trainer", user: { ...STAFF_STORAGE_USER, role: "Trainer" } },
    {
      name: "hoofdtrainer",
      user: { ...STAFF_STORAGE_USER, role: "Trainer", headTrainerClubIds: ["33333333-3333-3333-3333-333333333333"] },
    },
  ];

  for (const { name, user } of staffCases) {
    test(`${name} op /student/lessons gaat naar /dashboard`, async ({ page }) => {
      await setSession(page, { user, cookies: { has_token: "1", user_role: user.role } });

      await page.goto("/student/lessons");

      await expectStaffDashboard(page);
    });
  }

  test("super-admin op /student/lessons gaat naar het super-admin-dashboard", async ({ page }) => {
    await setSession(page, {
      user: { ...STAFF_STORAGE_USER, isSuperAdmin: true },
      cookies: { has_token: "1", user_role: "Admin", has_super_admin: "1" },
    });

    await page.goto("/student/lessons");

    await expect(page).toHaveURL(/\/super-admin\/dashboard$/);
  });

  test("admin zonder rolcookie (oudere sessie) op /student/lessons gaat via de layout naar /dashboard", async ({ page }) => {
    await setSession(page, { user: STAFF_STORAGE_USER, cookies: { has_token: "1" } });

    await page.goto("/student/lessons");

    await expectStaffDashboard(page);
    expect(await cookieValue(page, "user_role")).toBe("Admin");
  });

  test("ingelogde admin op /student-login gaat naar /dashboard", async ({ page }) => {
    await setSession(page, {
      user: STAFF_STORAGE_USER,
      cookies: { has_token: "1", user_role: "Admin" },
    });

    await page.goto("/student-login");

    await expectStaffDashboard(page);
  });
});

test.describe("Rolgebaseerde routing — verouderde cookies blokkeren nooit de login", () => {
  test("rolcookie zonder sessie: /login en /student-login blijven bereikbaar", async ({ page }) => {
    await setSession(page, { user: null, cookies: { user_role: "Student" } });

    await page.goto("/login");
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.locator('input[type="password"]')).toBeVisible();

    await page.context().addCookies([{ name: "user_role", value: "Admin", url: new URL(page.url()).origin }]);
    await page.goto("/student-login");
    await expect(page).toHaveURL(/\/student-login$/);
    await expect(page.getByPlaceholder("naam@voorbeeld.be")).toBeVisible();
  });

  test("studentcookies zonder token in localStorage: eindigt op /student-login en ruimt cookies op", async ({ page }) => {
    await setSession(page, { user: null, cookies: { has_token: "1", user_role: "Student" } });

    await page.goto("/login");

    await expect(page).toHaveURL(/\/student-login/);
    await expect(page.getByPlaceholder("naam@voorbeeld.be")).toBeVisible();
    expect(await cookieValue(page, "has_token")).toBeUndefined();
    expect(await cookieValue(page, "user_role")).toBeUndefined();
  });

  test("staffcookies zonder token in localStorage: eindigt op /login en ruimt cookies op", async ({ page }) => {
    await setSession(page, { user: null, cookies: { has_token: "1", user_role: "Admin" } });

    await page.goto("/login");

    await expect(page).toHaveURL(/\/login$/);
    await expect(page.locator('input[type="password"]')).toBeVisible();
    expect(await cookieValue(page, "has_token")).toBeUndefined();
  });

  test("rolcookie die niet klopt met de sessie: de layout zet hem recht, zonder lus", async ({ page }) => {
    await mockAllDashboardApis(page);
    // Cookie zegt Student, maar localStorage hoort bij een admin.
    await setSession(page, {
      user: STAFF_STORAGE_USER,
      cookies: { has_token: "1", user_role: "Student" },
    });

    await page.goto("/dashboard");

    await expectStaffDashboard(page);
    expect(await cookieValue(page, "user_role")).toBe("Admin");
  });
});
