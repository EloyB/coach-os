import { test, expect } from "@playwright/test";
import { loginViaStorage, mockAllDashboardApis } from "./helpers";

test.describe("Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("shows stat strip with real data from API", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByRole("heading", { name: "Vandaag", level: 1 })).toBeVisible();
    // Waarden uit TEST_DASHBOARD_SUMMARY; waarde en label zijn aparte elementen.
    const stat = (label: string) => page.getByText(label, { exact: true }).locator("..");
    await expect(stat("lessen vandaag")).toHaveText(/^0\s*lessen vandaag$/);
    await expect(stat("deze week")).toHaveText(/^2\s*deze week$/);
    await expect(stat("inschrijvingen")).toHaveText(/^5\s*inschrijvingen$/);
    await expect(stat("trainers")).toHaveText(/^2\s*trainers$/);
    await expect(stat("reeks")).toHaveText(/^1\s*reeks$/);
  });

  test("shows empty state for today's lessons", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByRole("heading", { name: "Lessen van vandaag" })).toBeVisible();
    await expect(page.getByText("Geen lessen vandaag")).toBeVisible();
  });

  test("shows quick actions", async ({ page }) => {
    await page.goto("/dashboard");

    const main = page.getByRole("main");
    await expect(main.getByRole("link", { name: /^Nieuwe lesreeks/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /^Nieuwe losse les/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /^Trainer uitnodigen/ })).toBeVisible();
  });

  test("has navigation sidebar with all menu items", async ({ page }) => {
    await page.goto("/dashboard");

    const nav = page.getByRole("navigation");
    await expect(nav.getByRole("link", { name: "Vandaag" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Lesreeksen" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Losse lessen" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Kampen" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Trainers" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Instellingen", exact: true })).toBeVisible();
  });

  test("quick action navigates to create lesson series page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("main").getByRole("link", { name: /^Nieuwe lesreeks/ }).click();

    await expect(page).toHaveURL(/\/dashboard\/lessons\/new/);
  });

  test("invite quick action opens the invite form on the trainers page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("main").getByRole("link", { name: /^Trainer uitnodigen/ }).click();

    await expect(page).toHaveURL(/\/dashboard\/trainers\?invite=1/);
    await expect(page.getByRole("heading", { name: "Trainer uitnodigen" })).toBeVisible();
  });
});
