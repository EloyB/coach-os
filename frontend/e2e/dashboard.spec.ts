import { test, expect } from "@playwright/test";
import { loginViaStorage, mockAllDashboardApis } from "./helpers";

test.describe("Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("shows stat cards", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByText("Actieve banen")).toBeVisible();
    await expect(page.getByText("Lessen deze week")).toBeVisible();
    await expect(page.getByText("Openstaande betalingen")).toBeVisible();
  });

  test("shows upcoming lessons section", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByText("Komende lessen")).toBeVisible();
  });

  test("shows quick actions section", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByText("Snelle acties")).toBeVisible();
  });

  test("has navigation sidebar with all menu items", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page.getByRole("link", { name: /Dashboard/ })).toBeVisible();
    await expect(page.getByRole("link", { name: /Lessen/ })).toBeVisible();
    await expect(page.getByRole("link", { name: /Instellingen/ })).toBeVisible();
  });

  test("quick action navigates to create lesson page", async ({ page }) => {
    await page.goto("/dashboard");

    // Use first matching link (there are two "Nieuwe les" links on dashboard)
    await page.getByRole("link", { name: "Nieuwe les" }).first().click();

    await expect(page).toHaveURL(/\/dashboard\/lessons\/new/);
  });
});
