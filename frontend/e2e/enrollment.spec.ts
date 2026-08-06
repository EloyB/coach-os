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
  test.beforeEach(async ({ page }) => {
    await mockPublicApi(
      page,
      "GET",
      `/public/lessonseries/${seriesId}/timeslots`,
      []
    );
  });

  test("loads public enrollment without sending an existing auth token", async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("token", "token-from-other-organization");
      localStorage.setItem(
        "auth_user",
        JSON.stringify({
          email: "steve@example.be",
          role: "Admin",
          organizationId: "99999999-9999-9999-9999-999999999999",
        })
      );
    });

    const requestsWithAuthHeader: string[] = [];
    await page.route(`**/public/lessonseries/${seriesId}**`, (route) => {
      if (route.request().headers().authorization) {
        requestsWithAuthHeader.push(route.request().url());
      }

      const url = new URL(route.request().url());
      if (url.pathname.endsWith(`/public/lessonseries/${seriesId}`)) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify(TEST_PUBLIC_SERIES),
        });
      }
      if (url.pathname.endsWith(`/public/lessonseries/${seriesId}/form`)) {
        return route.fulfill({
          status: 204,
          contentType: "application/json",
          body: JSON.stringify(null),
        });
      }
      if (url.pathname.endsWith(`/public/lessonseries/${seriesId}/timeslots`)) {
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify([]),
        });
      }
      return route.continue();
    });

    const formResponse = page.waitForResponse((response) => {
      const url = new URL(response.url());
      return url.pathname.endsWith(`/public/lessonseries/${seriesId}/form`);
    });
    const timeslotsResponse = page.waitForResponse((response) => {
      const url = new URL(response.url());
      return url.pathname.endsWith(`/public/lessonseries/${seriesId}/timeslots`);
    });

    await page.goto(`/enroll/${seriesId}`);
    await Promise.all([formResponse, timeslotsResponse]);

    await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
    expect(requestsWithAuthHeader).toEqual([]);
  });

  test("shows public lesson series info", async ({ page }) => {
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, null, 204);

    await page.goto(`/enroll/${seriesId}`);

    await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
    await expect(page.getByText("TC De Aces")).toBeVisible();
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

    await expect(page.getByText("Inschrijven mislukt.")).toBeVisible();
  });

  test("submits a group member without own email as studentEmail null", async ({
    page,
  }) => {
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, null, 204);

    // Capture the enrollment POST body so we can assert on groupMembers.
    let postBody: Record<string, unknown> | null = null;
    await page.route(`${API_BASE}/public/lessonseries/${seriesId}/enroll`, (route) => {
      if (route.request().method() === "POST") {
        postBody = route.request().postDataJSON();
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ enrollmentId: "11111111-1111-1111-1111-111111111111" }),
        });
      }
      return route.continue();
    });

    await page.goto(`/enroll/${seriesId}`);

    // Leader (group leader) — labels aren't associated via htmlFor, so use
    // input locators like the other tests. DOB has an associated label.
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill("Els");
    await inputs.nth(1).fill("Peeters");
    await page.locator('input[type="email"]').fill("ouder@example.com");
    await page.getByLabel(/geboortedatum/i).first().fill("1985-01-01");

    // Switch to group enrollment. The radio input is visually hidden (sr-only),
    // so click the label via its unique descriptive text.
    await page.getByText("Ik schrijf meerdere personen in").click();

    // Add a group member and fill it, leaving "eigen e-mailadres" unchecked.
    await page.getByRole("button", { name: "Lid toevoegen" }).click();
    await page.getByPlaceholder("Naam").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum 1/i).fill("2015-03-04");

    // Without an own email, the UI routes all communication via the leader.
    await expect(
      page.getByText("Alle communicatie loopt via ouder@example.com")
    ).toBeVisible();

    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect
      .poll(() => postBody?.groupMembers)
      .toEqual([
        expect.objectContaining({
          studentName: "Lotte Peeters",
          studentEmail: null,
        }),
      ]);
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

    await expect(page.getByLabel("Geboortedatum *")).toBeVisible();
    await expect(page.getByText("Eerder les gehad?")).toBeVisible();
  });
});
