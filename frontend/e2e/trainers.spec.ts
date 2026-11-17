import { test, expect } from "@playwright/test";
import { loginViaStorage, mockApi, mockAllDashboardApis } from "./helpers";

test.describe("Trainer Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("shows trainer list", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    const table = page.getByRole("table");
    await expect(table.getByRole("row", { name: /Jan Janssen/ })).toBeVisible();
    await expect(table.getByRole("row", { name: /Piet Pieters/ })).toBeVisible();
  });

  test("shows invite form when clicking invite button", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    await page.getByRole("button", { name: /Uitnodigen/ }).click();

    await expect(page.getByRole("heading", { name: "Trainer uitnodigen" })).toBeVisible();
    await expect(page.getByPlaceholder("Jan", { exact: true })).toBeVisible();
    await expect(page.getByPlaceholder("Janssen", { exact: true })).toBeVisible();
    await expect(page.getByPlaceholder("jan@tennisclub.be")).toBeVisible();
    await expect(page.getByRole("button", { name: "Uitnodigen", exact: true })).toBeVisible();
  });

  test("invite trainer shows success state", async ({ page }) => {
    await mockApi(page, "POST", "/trainers/invite", "new-trainer-id");

    await page.goto("/dashboard/trainers");
    await page.getByRole("button", { name: /Uitnodigen/ }).click();

    // Use exact placeholders to avoid strict mode violations
    await page.getByPlaceholder("Jan", { exact: true }).fill("Koen");
    await page.getByPlaceholder("Janssen", { exact: true }).fill("De Smet");
    await page.getByPlaceholder("jan@tennisclub.be").fill("koen@test.be");

    await page.getByRole("button", { name: "Uitnodigen", exact: true }).click();

    await expect(page.getByText("Uitnodiging verzonden")).toBeVisible();
    await expect(page.getByText("koen@test.be")).toBeVisible();
  });

  test("shows active trainer status badges", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    await expect(page.getByRole("cell", { name: "Actief" }).first()).toBeVisible();
  });

  test("page header shows title and counts", async ({ page }) => {
    await page.goto("/dashboard/trainers");

    await expect(page.getByRole("heading", { name: "Trainers", level: 1 })).toBeVisible();
    await expect(page.getByText("2 actief · 0 uitgenodigd")).toBeVisible();
  });
});
