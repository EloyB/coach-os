import { test, expect } from "@playwright/test";
import { loginViaStorage, mockApi, mockAllDashboardApis, TEST_CLUBS } from "./helpers";

test.describe("Tennis Club Settings", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test("shows tennis clubs list", async ({ page }) => {
    await page.goto("/dashboard/settings");

    await expect(page.getByText("TC De Aces")).toBeVisible();
    await expect(page.getByText("Sportlaan 1, Antwerpen")).toBeVisible();
  });

  test("shows empty state when no clubs", async ({ page }) => {
    await mockApi(page, "GET", "/tennisclubs", []);

    await page.goto("/dashboard/settings");

    await expect(page.getByText("Nog geen tennisclubs toegevoegd")).toBeVisible();
  });

  test("add tennis club form validates required fields", async ({ page }) => {
    await page.goto("/dashboard/settings");

    // Submit the add form without filling in
    await page.getByRole("button", { name: /Tennisclub toevoegen/i }).click();

    await expect(page.getByText("Naam is verplicht")).toBeVisible();
  });

  test("add tennis club successfully", async ({ page }) => {
    const newClub = {
      id: "99999999-9999-9999-9999-999999999999",
      name: "TC Brederode",
      address: "Parkstraat 10, Brussel",
      organizationId: "22222222-2222-2222-2222-222222222222",
    };
    await mockApi(page, "POST", "/tennisclubs", newClub.id);

    await page.goto("/dashboard/settings");

    // Use the specific placeholders from the form
    await page.getByPlaceholder("TC Brederode").fill("TC Brederode");
    await page.getByPlaceholder("Brederodestraat 5, Haarlem").fill("Parkstraat 10, Brussel");
    await page.getByRole("button", { name: /Tennisclub toevoegen/i }).click();

    // After success, the mutation refetches — mock the updated list
    await mockApi(page, "GET", "/tennisclubs", [...TEST_CLUBS, newClub]);

    await expect(page.getByText("TC Brederode")).toBeVisible();
  });

  test("delete button opens confirmation dialog", async ({ page }) => {
    await page.goto("/dashboard/settings");

    // The delete button is a small icon button (Trash2 icon) — use the trash icon trigger
    const deleteButton = page.locator("button").filter({ has: page.locator("svg.lucide-trash-2") });
    await deleteButton.first().click();

    // Confirm dialog should appear
    const dialog = page.getByRole("alertdialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText("Annuleren")).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Verwijderen" })).toBeVisible();
  });
});
