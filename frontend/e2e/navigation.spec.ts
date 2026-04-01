import { test, expect } from "@playwright/test";
import { loginViaStorage, mockAllDashboardApis } from "./helpers";

test.describe("Navigation", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("sidebar navigates to lessons page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("link", { name: /Lessen/ }).click();

    await expect(page).toHaveURL(/\/dashboard\/lessons/);
    await expect(page.getByRole("heading", { name: "Lessen", exact: true })).toBeVisible();
  });

  test("sidebar navigates to trainers page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("link", { name: /Trainers/ }).click();

    await expect(page).toHaveURL(/\/dashboard\/trainers/);
  });

  test("sidebar navigates to settings page", async ({ page }) => {
    await page.goto("/dashboard");

    await page.getByRole("link", { name: /Instellingen/ }).click();

    await expect(page).toHaveURL(/\/dashboard\/settings/);
  });

  test("sidebar navigates back to dashboard", async ({ page }) => {
    await page.goto("/dashboard/lessons");

    await page.getByRole("link", { name: /Dashboard/ }).click();

    await expect(page).toHaveURL(/\/dashboard$/);
  });

  test("logout link exists in sidebar", async ({ page }) => {
    await page.goto("/dashboard");

    // Logout is a Link element in the sidebar
    const logoutLink = page.getByRole("link", { name: /Uitloggen/ });
    await expect(logoutLink).toBeVisible();
    await expect(logoutLink).toHaveAttribute("href", "/login");
  });
});
