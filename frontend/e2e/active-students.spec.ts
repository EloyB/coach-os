import { test, expect, type Page } from "@playwright/test";
import type { ActiveStudentsDto } from "@/lib/api/subscription";
import { API_URL, TEST_CLUBS, loginViaStorage, mockAllDashboardApis, mockApi } from "./helpers";

/** Issue #391: teller "actieve leerlingen" in de zijbalk, enkel voor admins. */

const PERIOD = { periodStart: "2026-01-01", periodEnd: "2026-12-31" };

async function openDashboardWith(page: Page, dto: ActiveStudentsDto) {
  await loginViaStorage(page);
  await mockAllDashboardApis(page);
  // Later geregistreerde routes winnen: overschrijft de standaardmock.
  await mockApi(page, "GET", "/subscription/active-students", dto);
  await page.goto("/dashboard");
}

const card = (page: Page) => page.getByTestId("active-students-card");
const bar = (page: Page) => page.getByTestId("active-students-bar");

test.describe("Teller actieve leerlingen", () => {
  test("admin ziet 'X / Y' met voortgangsbalk en de periode", async ({ page }) => {
    await openDashboardWith(page, { count: 142, limit: 200, ...PERIOD });

    await expect(card(page)).toContainText("142 / 200 actieve leerlingen");
    await expect(card(page)).toContainText("Abonnementsjaar tot 31/12/2026");
    await expect(bar(page)).toBeVisible();
    await expect(bar(page)).toHaveAttribute("role", "progressbar");
    await expect(bar(page)).toHaveAttribute("aria-valuenow", "142");
    await expect(bar(page)).toHaveAttribute("aria-valuemax", "200");
    await expect(card(page)).toHaveAttribute("data-level", "normal");
  });

  test("enkelvoud bij 1 leerling", async ({ page }) => {
    await openDashboardWith(page, { count: 1, limit: null, ...PERIOD });

    await expect(card(page)).toContainText("1 actieve leerling");
    await expect(card(page)).not.toContainText("1 actieve leerlingen");
  });

  test("zonder limiet: alleen het aantal, geen balk", async ({ page }) => {
    await openDashboardWith(page, { count: 142, limit: null, ...PERIOD });

    await expect(card(page)).toContainText("142 actieve leerlingen");
    await expect(card(page)).not.toContainText(/\d+ \/ \d+/);
    await expect(bar(page)).toHaveCount(0);
  });

  test("limiet 0 geldt als geen limiet", async ({ page }) => {
    await openDashboardWith(page, { count: 10, limit: 0, ...PERIOD });

    await expect(card(page)).toContainText("10 actieve leerlingen");
    await expect(bar(page)).toHaveCount(0);
  });

  test("onder 80% is normaal, vanaf 80% oranje", async ({ page }) => {
    await openDashboardWith(page, { count: 160, limit: 200, ...PERIOD });

    await expect(card(page)).toHaveAttribute("data-level", "warning");
    await expect(card(page)).not.toContainText("Limiet bereikt of overschreden");
  });

  test("vanaf 100% rood met melding", async ({ page }) => {
    await openDashboardWith(page, { count: 200, limit: 200, ...PERIOD });

    await expect(card(page)).toHaveAttribute("data-level", "exceeded");
    await expect(card(page)).toContainText("Limiet bereikt of overschreden");
  });

  test("boven de limiet blijft de balk op 100%", async ({ page }) => {
    await openDashboardWith(page, { count: 250, limit: 200, ...PERIOD });

    await expect(card(page)).toContainText("250 / 200");
    await expect(bar(page).locator("> div")).toHaveAttribute("style", /width:\s*100%/);
  });

  test("een mislukte call laat de zijbalk intact en toont niets", async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
    await mockApi(page, "GET", "/subscription/active-students", {}, 500);
    await page.goto("/dashboard");

    await expect(page.getByRole("heading", { name: "Vandaag", level: 1 })).toBeVisible();
    await expect(card(page)).toHaveCount(0);
  });

  test("trainer ziet de teller niet en de endpoint wordt niet aangeroepen", async ({ page, baseURL }) => {
    await page.context().addCookies([
      { name: "has_token", value: "1", url: baseURL! },
      { name: "user_role", value: "Trainer", url: baseURL! },
    ]);
    await page.addInitScript(() => {
      localStorage.setItem("token", "fake-jwt-token-for-testing");
      localStorage.setItem(
        "auth_user",
        JSON.stringify({
          userId: "11111111-1111-1111-1111-111111111111",
          email: "trainer@test.be",
          firstName: "Piet",
          lastName: "Pieters",
          organizationId: "22222222-2222-2222-2222-222222222222",
          role: "Trainer",
        }),
      );
    });
    // Onbekende calls krijgen een 404 zodat de testgebruiker niet uitgelogd wordt.
    await page.route(`${API_URL}/**`, (route) =>
      route.fulfill({ status: 404, contentType: "application/json", body: "{}" }),
    );
    await mockApi(page, "GET", "/tennisclubs", TEST_CLUBS);
    await mockApi(page, "GET", "/me/trainer-availabilities", []);

    let called = false;
    await page.route(`${API_URL}/subscription/active-students*`, (route) => {
      called = true;
      return route.fulfill({ status: 403, body: "{}" });
    });

    await page.goto("/dashboard/trainer-settings");
    // Zijbalk is geladen (toont de naam van de trainer) voordat we op afwezigheid controleren.
    await expect(page.getByText("Piet Pieters")).toBeVisible();
    await page.waitForLoadState("networkidle");

    await expect(card(page)).toHaveCount(0);
    expect(called).toBe(false);
  });
});
