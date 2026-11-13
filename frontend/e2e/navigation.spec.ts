import { test, expect } from "@playwright/test";
import { loginViaStorage, mockAllDashboardApis } from "./helpers";

test.describe("Navigation", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("sidebar navigates to lesson series page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("navigation").getByRole("link", { name: "Lesreeksen" }).click();

    await expect(page).toHaveURL(/\/dashboard\/lessons$/);
    await expect(page.getByRole("heading", { name: "Lesreeksen", exact: true })).toBeVisible();
  });

  test("sidebar navigates to trainers page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("navigation").getByRole("link", { name: "Trainers" }).click();

    await expect(page).toHaveURL(/\/dashboard\/trainers/);
  });

  test("sidebar navigates to settings page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("link", { name: "Instellingen", exact: true }).click();

    await expect(page).toHaveURL(/\/dashboard\/settings/);
  });

  test("sidebar navigates back to dashboard", async ({ page }) => {
    await page.goto("/dashboard/lessons");

    await page.getByRole("navigation").getByRole("link", { name: "Vandaag" }).click();

    await expect(page).toHaveURL(/\/dashboard$/);
  });

  test("logout button clears the session and returns to login", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("button", { name: "Uitloggen" }).click();

    await expect(page).toHaveURL(/\/login/);
    expect(await page.evaluate(() => localStorage.getItem("token"))).toBeNull();
  });
});
