import { test, expect, type Page } from "@playwright/test";
import { loginViaStorage, mockAllDashboardApis, mockApi } from "./helpers";

// Een admin die zelf lesgeeft (AdminsActAsTrainers) moet zijn eigen beschikbaarheid kunnen invullen (#382).

const NAV_LABEL = "Mijn beschikbaarheid";

async function setup(page: Page, adminsActAsTrainers: boolean) {
  await loginViaStorage(page);
  await mockAllDashboardApis(page);
  await mockApi(page, "GET", "/organization/settings", {
    adminsActAsTrainers,
    currentUserUpcomingLessonsAsTrainer: 0,
  });
  await mockApi(page, "GET", "/me/trainer-availabilities", []);
}

test.describe("Admin als trainer: eigen beschikbaarheid", () => {
  test.use({ viewport: { width: 1280, height: 800 } });

  test("toont de pagina in de navigatie wanneer de admin zelf lesgeeft", async ({ page }) => {
    await setup(page, true);
    await page.goto("/dashboard");

    const link = page.getByRole("link", { name: NAV_LABEL });
    await expect(link).toBeVisible();
    // De gewone admin-instellingen blijven apart bestaan.
    await expect(page.getByRole("link", { name: "Instellingen" })).toBeVisible();

    await link.click();
    await expect(page).toHaveURL(/\/dashboard\/trainer-settings$/);
    await expect(page.getByRole("heading", { name: "Instellingen" })).toBeVisible();
    await expect(page.getByText("Beheer je eigen beschikbaarheid.")).toBeVisible();
  });

  test("verbergt de pagina wanneer de admin niet zelf lesgeeft en stuurt directe toegang terug", async ({ page }) => {
    await setup(page, false);
    await page.goto("/dashboard");
    await expect(page.getByRole("link", { name: "Instellingen" })).toBeVisible();
    await expect(page.getByRole("link", { name: NAV_LABEL })).toHaveCount(0);

    await page.goto("/dashboard/trainer-settings");
    await expect(page).toHaveURL(/\/dashboard$/);
  });
});
