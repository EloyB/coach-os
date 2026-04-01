import { test, expect } from "@playwright/test";
import { loginViaStorage, mockApi, mockAllDashboardApis } from "./helpers";

test.describe("Trainer Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("shows trainer list", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    await expect(page.getByText("Jan Janssen")).toBeVisible();
    await expect(page.getByText("Piet Pieters")).toBeVisible();
  });

  test("shows invite form when clicking invite button", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    await page.getByRole("button", { name: /Trainer uitnodigen/i }).click();

    await expect(page.getByText("Voornaam")).toBeVisible();
    await expect(page.getByText("Achternaam")).toBeVisible();
    await expect(page.getByText("E-mailadres")).toBeVisible();
    await expect(page.getByRole("button", { name: /Uitnodiging sturen/i })).toBeVisible();
  });

  test("invite trainer shows success state", async ({ page }) => {
    await mockApi(page, "POST", "/trainers/invite", "new-trainer-id");

    await page.goto("/dashboard/trainers");
    await page.getByRole("button", { name: /Trainer uitnodigen/i }).click();

    // Use exact placeholders to avoid strict mode violations
    await page.getByPlaceholder("Jan", { exact: true }).fill("Koen");
    await page.getByPlaceholder("Janssen", { exact: true }).fill("De Smet");
    await page.getByPlaceholder("jan@tennisclub.be").fill("koen@test.be");

    await page.getByRole("button", { name: /Uitnodiging sturen/i }).click();

    await expect(page.getByText("Uitnodiging verzonden")).toBeVisible();
    await expect(page.getByText("koen@test.be")).toBeVisible();
  });

  test("shows active trainer status badges", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    await expect(page.getByText("Actief").first()).toBeVisible();
  });

  test("page header shows title and description", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    await expect(page.getByRole("heading", { name: "Trainers" })).toBeVisible();
    await expect(page.getByText("Beheer de trainers van je organisatie")).toBeVisible();
  });
});
