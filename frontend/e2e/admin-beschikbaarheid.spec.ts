import { test, expect, type Page } from "@playwright/test";
import { loginViaStorage, mockAllDashboardApis, mockApi } from "./helpers";

// Een admin die zelf lesgeeft (AdminsActAsTrainers) vult zijn eigen beschikbaarheid in op de
// instellingenpagina, net als een trainer op zijn eigen pagina (#382).

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

  test("toont de sectie op de instellingenpagina wanneer de admin zelf lesgeeft", async ({ page }) => {
    await setup(page, true);
    await page.goto("/dashboard/settings");

    await expect(page.getByRole("heading", { name: "Mijn beschikbaarheid" })).toBeVisible();
    await expect(
      page.getByText("Geef per weekdag aan wanneer je zelf kunt lesgeven. Dit gebruik je bij het plannen van lessen."),
    ).toBeVisible();
  });

  test("toont de sectie niet wanneer de admin niet zelf lesgeeft", async ({ page }) => {
    await setup(page, false);
    await page.goto("/dashboard/settings");

    await expect(page.getByRole("heading", { name: "Instellingen" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Mijn beschikbaarheid" })).toHaveCount(0);
  });

  test("voegt geen extra item toe aan de navigatie", async ({ page }) => {
    await setup(page, true);
    await page.goto("/dashboard");

    await expect(page.getByRole("link", { name: "Instellingen" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Mijn beschikbaarheid" })).toHaveCount(0);
  });
});
