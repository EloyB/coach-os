import { test, expect } from "@playwright/test";
import {
  loginViaStorage,
  mockApi,
  mockAllDashboardApis,
  TEST_SERIES,
} from "./helpers";

test.describe("Lesson Series", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test.describe("List", () => {
    test("shows lesson series cards", async ({ page }) => {
      await page.goto("/dashboard/lessons");

      await expect(page.getByRole("heading", { name: "Lessen", exact: true })).toBeVisible();
      await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
      await expect(page.getByText("Jan Janssen")).toBeVisible();
      await expect(page.getByText("Actief")).toBeVisible();
    });

    test("shows empty state when no series exist", async ({ page }) => {
      await mockApi(page, "GET", "/lessonseries", []);

      await page.goto("/dashboard/lessons");

      await expect(page.getByText("Nog geen lesreeksen")).toBeVisible();
    });

    test("has create button that navigates to new page", async ({ page }) => {
      await page.goto("/dashboard/lessons");

      await page.getByRole("link", { name: "Nieuwe lesreeks" }).click();

      await expect(page).toHaveURL(/\/dashboard\/lessons\/new/);
    });

    test("card links to detail page", async ({ page }) => {
      await page.goto("/dashboard/lessons");

      await page.getByRole("link", { name: "Bekijken" }).click();

      await expect(page).toHaveURL(/\/dashboard\/lessons\/66666666/);
    });
  });

  test.describe("Create", () => {
    test("shows create form with all required fields", async ({ page }) => {
      await page.goto("/dashboard/lessons/new");

      await expect(page.getByRole("heading", { name: "Nieuwe lesreeks" })).toBeVisible();
      await expect(page.getByText("Naam", { exact: false }).first()).toBeVisible();
      await expect(page.getByRole("button", { name: "Lesreeks aanmaken" })).toBeVisible();
    });

    test("shows validation errors on empty submit", async ({ page }) => {
      await page.goto("/dashboard/lessons/new");

      await page.getByRole("button", { name: "Lesreeks aanmaken" }).click();

      await expect(page.getByText("Naam is verplicht")).toBeVisible();
    });

    test("successful create redirects to lessons list", async ({ page }) => {
      await mockApi(page, "POST", "/lessonseries", "new-id-123");

      await page.goto("/dashboard/lessons/new");

      // Fill name
      await page.getByPlaceholder("Voorjaarslessen 2026").fill("Zomerlessen 2026");

      // Select trainer
      await page.locator("button").filter({ hasText: "Kies een trainer" }).click();
      await page.getByRole("option", { name: "Jan Janssen" }).click();

      // Select club
      await page.locator("button").filter({ hasText: "Kies een tennisclub" }).click();
      await page.getByRole("option", { name: "TC De Aces" }).click();

      // Select level — use the select trigger then option role
      await page.locator("button").filter({ hasText: "Kies een niveau" }).click();
      await page.getByRole("option", { name: "Beginner", exact: true }).click();

      // Fill dates
      await page.locator('input[name="startDate"]').fill("2026-07-01");
      await page.locator('input[name="endDate"]').fill("2026-09-30");

      await page.getByRole("button", { name: "Lesreeks aanmaken" }).click();

      await page.waitForURL("**/dashboard/lessons", { timeout: 5000 });
      await expect(page).toHaveURL(/\/dashboard\/lessons$/);
    });

    test("back link navigates to lessons list", async ({ page }) => {
      await page.goto("/dashboard/lessons/new");

      await page.getByRole("link", { name: "Terug naar lessen" }).click();

      await expect(page).toHaveURL(/\/dashboard\/lessons$/);
    });
  });

  test.describe("Detail", () => {
    test.beforeEach(async ({ page }) => {
      const seriesId = "66666666-6666-6666-6666-666666666666";
      await mockApi(page, "GET", `/lessonseries/${seriesId}`, TEST_SERIES[0]);
      await mockApi(page, "GET", `/lessonseries/${seriesId}/enrollments`, []);
    });

    test("shows series details and lessons", async ({ page }) => {
      await page.goto("/dashboard/lessons/66666666-6666-6666-6666-666666666666");

      await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
      await expect(page.getByText("Baan 1").first()).toBeVisible();
    });
  });
});
