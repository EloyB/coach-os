import { test, expect } from "@playwright/test";
import { loginViaStorage, mockAllDashboardApis } from "./helpers";

test.describe("Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("shows stat cards with real data from API", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByText("Actieve lesreeksen")).toBeVisible();
    await expect(page.getByText("Lessen deze week")).toBeVisible();
    await expect(page.getByText("Inschrijvingen")).toBeVisible();
    // "Trainers" appears in sidebar + stat card, just check the stat label exists
    await expect(page.locator("text=Trainers").first()).toBeVisible();

    // Verify actual values from mock
    await expect(page.getByText("1").first()).toBeVisible(); // activeSeriesCount
    await expect(page.getByText("5")).toBeVisible(); // totalEnrollmentCount
  });

  test("shows upcoming lessons from API", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByText("Komende lessen")).toBeVisible();
    await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
    await expect(page.getByText("10:00 – 11:00")).toBeVisible();
    await expect(page.getByText("Jan Janssen")).toBeVisible();
  });

  test("shows quick actions section", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByText("Snelle acties")).toBeVisible();
  });

  test("has navigation sidebar with all menu items", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByRole("link", { name: /Dashboard/ })).toBeVisible();
    await expect(page.getByRole("link", { name: /Lessen/ })).toBeVisible();
    await expect(page.getByRole("link", { name: "Instellingen", exact: true })).toBeVisible();
  });

  test("quick action navigates to create lesson page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("link", { name: "Nieuwe les" }).first().click();

    await expect(page).toHaveURL(/\/dashboard\/lessons\/new/);
  });

  test("tip links to settings page", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByText("Tip")).toBeVisible();
    await expect(page.getByRole("link", { name: "Instellingen →" })).toBeVisible();
  });
});
