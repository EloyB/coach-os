import { test, expect } from "@playwright/test";
import { mockApi } from "./helpers";

const STUDENT_AUTH = {
  token: "fake-student-jwt",
  expiresAt: "2099-01-01T00:00:00Z",
  email: "emma.claes@gmail.com",
  role: "Student",
};

const STUDENT_LESSON = {
  assignmentId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  seriesId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  seriesName: "Zomerlessen 2026",
  seriesStartDate: "2026-04-14",
  seriesEndDate: "2026-07-14",
  dayOfWeek: 1,
  startTime: "18:00",
  endTime: "19:00",
  courtName: "Baan 1",
  trainerName: "Jan Janssen",
  status: "AwaitingConfirmation",
  isGroup: true,
  groupSize: 2,
  price: 360,
  paymentStatus: null,
};

async function loginAsStudent(page: import("@playwright/test").Page) {
  await page.goto("/student-login");
  await page.evaluate((auth) => {
    localStorage.setItem("token", auth.token);
    localStorage.setItem(
      "auth_user",
      JSON.stringify({ email: auth.email, role: auth.role })
    );
    document.cookie = "has_token=1; path=/; SameSite=Lax";
  }, STUDENT_AUTH);
}

test.describe("Student portal — magic-link auth", () => {
  test("login page renders form and branding", async ({ page }) => {
    await page.goto("/student-login");
    await expect(
      page.getByRole("heading", { name: "Inloggen als leerling" })
    ).toBeVisible();
    await expect(page.getByLabel("E-mailadres")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Stuur inloglink" })
    ).toBeVisible();
  });

  test("submitting email shows 'check your inbox' state", async ({ page }) => {
    await mockApi(page, "POST", "/auth/magic/request", {}, 202);

    await page.goto("/student-login");
    await page.getByLabel("E-mailadres").fill("emma.claes@gmail.com");
    await page.getByRole("button", { name: "Stuur inloglink" }).click();

    await expect(page.getByText("Check je mailbox")).toBeVisible();
  });

  test("magic-link redeem stores token and redirects to lessons", async ({
    page,
  }) => {
    await mockApi(page, "POST", "/auth/magic/redeem", STUDENT_AUTH);
    await mockApi(page, "GET", "/student/lessons", [STUDENT_LESSON]);

    await page.goto("/auth/magic?token=anything");
    await page.waitForURL("**/student/lessons");

    await expect(
      page.getByRole("heading", { name: "Mijn lessen" })
    ).toBeVisible();
    await expect(page.getByText("Zomerlessen 2026")).toBeVisible();
  });

  test("invalid magic-link token shows error state", async ({ page }) => {
    await page.route("**/auth/magic/redeem*", (route) => {
      if (route.request().method() === "POST") {
        return route.fulfill({
          status: 400,
          contentType: "application/json",
          body: JSON.stringify(["Ongeldige of verlopen link"]),
        });
      }
      return route.continue();
    });

    await page.goto("/auth/magic?token=bad");
    await expect(page.getByText("Link is ongeldig")).toBeVisible();
    await expect(
      page.getByRole("link", { name: "Nieuwe link aanvragen" })
    ).toBeVisible();
  });

  test("authenticated student sees lessons list", async ({ page }) => {
    await mockApi(page, "GET", "/student/lessons", [STUDENT_LESSON]);

    await loginAsStudent(page);
    await page.goto("/student/lessons");

    await expect(
      page.getByRole("heading", { name: "Mijn lessen" })
    ).toBeVisible();
    await expect(page.getByText("Zomerlessen 2026")).toBeVisible();
    await expect(page.getByText("maandag 18:00 — 19:00")).toBeVisible();
    await expect(page.getByText("AwaitingConfirmation")).toBeVisible();
  });

  test("clicking a lesson opens detail page", async ({ page }) => {
    await mockApi(page, "GET", "/student/lessons", [STUDENT_LESSON]);
    await mockApi(
      page,
      "GET",
      `/student/lessons/${STUDENT_LESSON.assignmentId}`,
      STUDENT_LESSON
    );

    await loginAsStudent(page);
    await page.goto("/student/lessons");

    await page.getByText("Zomerlessen 2026").click();
    await page.waitForURL(`**/student/lessons/${STUDENT_LESSON.assignmentId}`);

    await expect(
      page.getByRole("heading", { name: "Zomerlessen 2026" })
    ).toBeVisible();
    await expect(page.getByText("Jan Janssen")).toBeVisible();
    await expect(page.getByText("Baan 1")).toBeVisible();
    await expect(page.getByText("€ 360.00")).toBeVisible();
  });

  test("unauthenticated student is redirected to login", async ({ page }) => {
    await page.goto("/student/lessons");
    await page.waitForURL(/\/student-login/);
    await expect(page).toHaveURL(/\/student-login/);
  });
});
