import { test, expect } from "@playwright/test";
import { TEST_PUBLIC_SERIES } from "./helpers";

const seriesId = "66666666-6666-6666-6666-666666666666";
const API_BASE = "http://localhost:5142/api";

function mockPublicApi(
  page: import("@playwright/test").Page,
  method: string,
  path: string,
  response: unknown,
  status = 200
) {
  return page.route(`${API_BASE}${path}`, (route) => {
    if (route.request().method() === method.toUpperCase()) {
      return route.fulfill({
        status,
        contentType: "application/json",
        body: JSON.stringify(response),
      });
    }
    return route.continue();
  });
}

test.describe("Public Enrollment", () => {
  test("shows public lesson series info", async ({ page }) => {
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, null, 204);

    await page.goto(`/enroll/${seriesId}`);

    await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
    await expect(page.getByText("TC De Aces")).toBeVisible();
    await expect(page.getByText("Jan Janssen")).toBeVisible();
  });

  test("shows enrollment form fields", async ({ page }) => {
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, null, 204);

    await page.goto(`/enroll/${seriesId}`);

    await expect(page.getByText("Voornaam")).toBeVisible();
    await expect(page.getByText("Achternaam")).toBeVisible();
    await expect(page.getByText("E-mailadres")).toBeVisible();
    await expect(page.getByRole("button", { name: "Inschrijven" })).toBeVisible();
  });

  test("submits enrollment successfully", async ({ page }) => {
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, null, 204);
    await mockPublicApi(page, "POST", `/public/lessonseries/${seriesId}/enroll`, "enrollment-id-123");

    await page.goto(`/enroll/${seriesId}`);

    // Labels aren't associated with inputs via htmlFor, use input locators
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill("Sophie");
    await inputs.nth(1).fill("De Vries");
    await page.locator('input[type="email"]').fill("sophie@example.be");
    // Geboortedatum is verplicht sinds de tariefcategorieën (volwassene/jeugd).
    await page.locator('input[type="date"]').fill("1990-05-12");
    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect(page.getByText("Ingeschreven!")).toBeVisible();
  });

  test("shows error on failed enrollment", async ({ page }) => {
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, null, 204);
    await mockPublicApi(page, "POST", `/public/lessonseries/${seriesId}/enroll`, "Inschrijving mislukt", 400);

    await page.goto(`/enroll/${seriesId}`);

    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill("Sophie");
    await inputs.nth(1).fill("De Vries");
    await page.locator('input[type="email"]').fill("sophie@example.be");
    // Geboortedatum is verplicht sinds de tariefcategorieën (volwassene/jeugd).
    await page.locator('input[type="date"]').fill("1990-05-12");
    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect(page.getByText(/mislukt/i)).toBeVisible();
  });

  test("shows enrollment form with custom fields", async ({ page }) => {
    const customForm = {
      id: "form-1",
      lessonSeriesId: seriesId,
      fields: [
        {
          id: "field-1",
          label: "Geboortedatum",
          type: 1,
          isRequired: true,
          options: [],
          order: 0,
        },
        {
          id: "field-2",
          label: "Eerder les gehad?",
          type: 3,
          isRequired: false,
          options: [],
          order: 1,
        },
      ],
    };

    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, customForm);

    await page.goto(`/enroll/${seriesId}`);

    await expect(page.getByText("Geboortedatum")).toBeVisible();
    await expect(page.getByText("Eerder les gehad?")).toBeVisible();
  });
});
