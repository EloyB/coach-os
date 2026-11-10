import { test, expect } from "@playwright/test";
import { loginViaStorage, TEST_SERIES } from "./helpers";

const id = "66666666-6666-6666-6666-666666666666";
const warningTitle = "Let op met medische vragen";
const warningText = "Medische vragen zijn niet toegelaten, behalve vragen naar allergieën of aandachtspunten voor de veiligheid van deelnemers. De club is verantwoordelijk voor de vragen die ze stelt en de gegevens die ze verzamelt.";

for (const kind of ["lessons", "camps"] as const) {
  for (const width of [1280, 390]) {
    test(`${kind}: privacy warning stays visible while editing at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 });
      const apiKind = kind === "lessons" ? "lessonseries" : "camps";
      const detail = kind === "lessons" ? TEST_SERIES[0] : {
        id, name: "Zomerkamp", description: null, tennisClubId: id,
        tennisClubName: "TC De Aces", level: null, price: 100,
        startDate: "2027-07-01", endDate: "2027-07-02",
        registrationDeadline: "2027-06-01T12:00:00Z", maxParticipants: 20,
        participantCount: 0, isActive: true, days: [], prices: [],
      };
      let savedFields: unknown;
      // Every API call is intercepted: this spec never needs a running backend.
      await page.route("**/api/**", async (route) => {
        const path = new URL(route.request().url()).pathname;
        let data: unknown = [];
        if (path === `/api/${apiKind}/${id}`) data = detail;
        if (path === `/api/public/${apiKind}/${id}/form`) data = { id, fields: [] };
        if (path === `/api/${apiKind}/${id}/form` && route.request().method() === "PUT") {
          savedFields = route.request().postDataJSON().fields;
          data = id;
        }
        await route.fulfill({ json: data });
      });
      await loginViaStorage(page);
      await page.goto(`/dashboard/${kind}/${id}`);
      const builder = page.getByRole("heading", { name: "Inschrijfformulier", exact: true })
        .locator('xpath=ancestor::div[.//button[contains(., "Veld toevoegen")]][1]');
      await expect(builder).toBeVisible();
      const warning = builder.getByRole("note");
      await expect(warning).toContainText(warningTitle);
      await expect(warning).toContainText(warningText);
      await builder.getByRole("button", { name: "Veld toevoegen" }).click();
      await builder.locator('input[type="text"]').fill("Allergieën voor de veiligheid");
      await expect(warning).toBeVisible();
      expect(await warning.evaluate((element) => {
        const builder = element.parentElement;
        const field = builder?.querySelector('input[type="text"]');
        return field !== null && field !== undefined && Boolean(element.compareDocumentPosition(field) & Node.DOCUMENT_POSITION_FOLLOWING);
      })).toBe(true);
      await builder.getByRole("button", { name: "Formulier opslaan" }).click();
      await expect.poll(() => savedFields).toEqual([
        { label: "Allergieën voor de veiligheid", type: 1, isRequired: false, order: 0 },
      ]);
      await expect(warning).toBeVisible();
      await warning.scrollIntoViewIfNeeded();
      await expect(page.locator("body")).toHaveJSProperty("scrollWidth", width);
      if (process.env.PR_SCREENSHOT_DIR) {
        await page.screenshot({ path: `${process.env.PR_SCREENSHOT_DIR}/issue-263-${kind}-${width}.png` });
      }
    });
  }
}
