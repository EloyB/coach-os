import { test, expect, Page } from "@playwright/test";
import {
  loginViaStorage,
  mockApi,
  mockAllDashboardApis,
  TEST_SERIES,
} from "./helpers";

// ─── Wizard helpers ─────────────────────────────────────────────────────────

/** Select a date in the custom date-picker. */
async function selectDate(
  page: Page,
  pickerIndex: number,
  accessibleName: string
): Promise<void> {
  await page.getByRole("button", { name: "Kies een datum" }).nth(pickerIndex).click();
  const popover = page.locator('[data-radix-popper-content-wrapper]').last();
  await popover.locator("select").nth(0).selectOption("3"); // April
  await popover.locator("select").nth(1).selectOption("2026");
  await popover.getByRole("button", { name: accessibleName }).click();
}

/** Fill step 1 fields and click "Volgende" to advance to step 2 */
async function fillStep1AndAdvance(page: Page): Promise<void> {
  await page.getByPlaceholder("Voorjaarslessen 2026").fill("Zomerlessen 2026");

  // Price
  await page.locator('input[name="price"]').fill("150");

  // Max registrations
  await page.locator('input[name="maxRegistrations"]').fill("12");

  // Select club
  await page.locator("button").filter({ hasText: "Kies een tennisclub" }).click();
  await page.getByRole("option", { name: "TC De Aces" }).click();

  // Dates
  await selectDate(page, 0, "dinsdag 21 april 2026");
  await selectDate(page, 0, "donderdag 30 april 2026");
  await selectDate(page, 0, "dinsdag 14 april 2026");

  await page.getByRole("button", { name: "Volgende" }).click();
}

/**
 * Click on the calendar grid to add a 1-hour slot on a given day.
 * The calendar uses click-to-add: clicking an empty cell creates a slot
 * with default values at the snapped time position.
 * @param dayIndex 0 = Monday, 1 = Tuesday, ... 6 = Sunday
 */
async function addSlotOnDay(page: Page, dayIndex: number): Promise<void> {
  // The calendar grid body uses inline gridTemplateColumns: "60px repeat(7, 1fr)"
  // Children: [0] = time axis, [1–7] = day columns (Mon–Sun)
  const gridBody = page.locator('[style*="60px repeat(7, 1fr)"]').last();
  const dayColumn = gridBody.locator("> div").nth(dayIndex + 1);

  // Click in the middle of the column to create a slot
  await dayColumn.click({ position: { x: 50, y: 312 } });

  // Wait for the slot to appear
  await expect(page.locator("div[data-slot-id]").first()).toBeVisible();
}

// ─── Tests ──────────────────────────────────────────────────────────────────

test.describe("Lesson Series", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
  });

  test.describe("List", () => {
    test("shows lesson series cards", async ({ page }) => {
      await page.goto("/dashboard/lessons");

      await expect(page.getByRole("heading", { name: "Lessen", exact: true })).toBeVisible();
      await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
      await expect(page.getByText("Jan Janssen").first()).toBeVisible();
      await expect(page.getByText("Actief")).toBeVisible();
    });

    test("shows empty state when no series exist", async ({ page }) => {
      await mockApi(page, "GET", "/lessonseries", []);

      await page.goto("/dashboard/lessons");

      await expect(page.getByText("Nog geen lesreeksen")).toBeVisible();
    });

    test("has create button that navigates to new page", async ({ page }) => {
      await page.goto("/dashboard/lessons");

      await page.getByRole("link", { name: "Nieuwe lesreeks" }).click();

      await expect(page).toHaveURL(/\/dashboard\/lessons\/new/);
    });

    test("card links to detail page", async ({ page }) => {
      const seriesId = "66666666-6666-6666-6666-666666666666";
      await mockApi(page, "GET", `/lessonseries/${seriesId}`, TEST_SERIES[0]);
      await mockApi(page, "GET", `/lessonseries/${seriesId}/enrollments`, []);

      await page.goto("/dashboard/lessons");

      await page.getByRole("link", { name: "Bekijken" }).click();

      await expect(page).toHaveURL(/\/dashboard\/lessons\/66666666/);
    });
  });

  test.describe("Create Wizard", () => {
    test.describe("Step 1 — Basisinfo", () => {
      test("shows wizard heading and step indicator", async ({ page }) => {
        await page.goto("/dashboard/lessons/new");

        await expect(page.getByRole("heading", { name: "Nieuwe lesreeks" })).toBeVisible();
        await expect(page.getByText("Basisinfo")).toBeVisible();
        await expect(page.getByText("Planning")).toBeVisible();
        await expect(page.getByText("Validatie")).toBeVisible();
      });

      test("validates required fields on empty submit", async ({ page }) => {
        await page.goto("/dashboard/lessons/new");

        await page.getByRole("button", { name: "Volgende" }).click();

        // Form should not advance to step 2
        await expect(page.getByText("Weekindeling")).not.toBeVisible();
      });

      test("fills all fields and advances to step 2", async ({ page }) => {
        await page.goto("/dashboard/lessons/new");

        await fillStep1AndAdvance(page);

        // Should now be on step 2
        await expect(page.getByText("Weekindeling")).toBeVisible();
      });
    });

    test.describe("Step 2 — Planning", () => {
      test.beforeEach(async ({ page }) => {
        await page.goto("/dashboard/lessons/new");
        await fillStep1AndAdvance(page);
      });

      test("shows 7 day columns", async ({ page }) => {
        for (const day of ["Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo"]) {
          await expect(page.getByText(day, { exact: true }).first()).toBeVisible();
        }
      });

      test("clicking calendar adds a slot", async ({ page }) => {
        const slotsBefore = await page.locator("div[data-slot-id]").count();
        expect(slotsBefore).toBe(0);

        await addSlotOnDay(page, 1); // Tuesday

        const slotsAfter = await page.locator("div[data-slot-id]").count();
        expect(slotsAfter).toBe(1);
      });

      test("can add and label a parallel court at the same day and time", async ({ page }) => {
        await page.getByPlaceholder("Baan 1").fill("Baan 1");
        await addSlotOnDay(page, 1); // Tuesday

        await page.locator("div[data-slot-id]").first().hover();
        await page
          .getByRole("button", { name: "Parallel veld op dit moment toevoegen" })
          .click();

        await expect(page.locator("div[data-slot-id]")).toHaveCount(2);
        await expect(page.getByText("Parallel veld 1 van 2")).toBeVisible();
        await expect(page.getByText("Parallel veld 2 van 2")).toBeVisible();
        await expect(page.getByText("Dubbele les op di om")).toBeVisible();

        await page.locator("div[data-slot-id]").nth(1).click();
        await page.getByLabel("Naam baan").fill("Baan 2");
        await page.getByRole("button", { name: "Slot opslaan" }).click();

        await expect(page.getByText("Baan 1")).toBeVisible();
        await expect(page.getByText("Baan 2")).toBeVisible();
        await expect(page.getByText("Parallel veld 1 van 2")).toBeVisible();
        await expect(page.getByText("Parallel veld 2 van 2")).toBeVisible();
        await expect(page.getByText("Overlappende lesmomenten")).not.toBeVisible();
      });

      test("can remove a slot", async ({ page }) => {
        await addSlotOnDay(page, 1);

        // Verify slot exists
        await expect(page.locator("div[data-slot-id]")).toHaveCount(1);

        // Hover slot block and click X button
        const slotBlock = page.locator("div[data-slot-id]").first();
        await slotBlock.hover();
        await slotBlock.locator("button").click();

        // Slot should be gone
        await expect(page.locator("div[data-slot-id]")).toHaveCount(0);
      });

      test("navigating back to step 1 preserves data", async ({ page }) => {
        await page.getByRole("button", { name: "Vorige" }).click();

        // Step 1 should show with filled data
        await expect(page.getByPlaceholder("Voorjaarslessen 2026")).toHaveValue("Zomerlessen 2026");
      });

      test("advances to step 3", async ({ page }) => {
        await addSlotOnDay(page, 1);
        await page.getByRole("button", { name: "Volgende" }).click();

        // Should show step 3 header
        await expect(page.getByText("Controleer je planning")).toBeVisible();
      });
    });

    test.describe("Step 3 — Validation", () => {
      test.beforeEach(async ({ page }) => {
        await page.goto("/dashboard/lessons/new");
        await fillStep1AndAdvance(page);
        await addSlotOnDay(page, 1); // Tuesday — falls within series range in week 1
        await page.getByRole("button", { name: "Volgende" }).click();
      });

      test("shows week cards with slots", async ({ page }) => {
        await expect(page.getByText("Week 1", { exact: true })).toBeVisible();
        // Slot time should appear (en dash format in step 3)
        await expect(page.locator("text=/\\d{2}:\\d{2}–\\d{2}:\\d{2}/").first()).toBeVisible();
      });

      test("opens edit week dialog on Aanpassen click", async ({ page }) => {
        await page.getByRole("button", { name: "Aanpassen" }).first().click();

        await expect(page.getByText("Week aanpassen")).toBeVisible();
        // List layout should show full day names
        await expect(page.getByText("Maandag")).toBeVisible();
      });

      test("overriding a week shows Aangepast badge", async ({ page }) => {
        await page.getByRole("button", { name: "Aanpassen" }).first().click();

        // Save without changes — creates an override
        await page.getByRole("button", { name: "Week opslaan" }).click();

        await expect(page.getByText("Aangepast").first()).toBeVisible();
      });

      test("successful submit redirects to lessons list", async ({ page }) => {
        await mockApi(page, "POST", "/lessonseries", "new-id-123");

        await page.getByRole("button", { name: "Lesreeks aanmaken" }).click();

        await page.waitForURL("**/dashboard/lessons", { timeout: 5000 });
        await expect(page).toHaveURL(/\/dashboard\/lessons$/);
      });
    });

    test.describe("Leave guard", () => {
      test("no dialog when form is clean", async ({ page }) => {
        await page.goto("/dashboard/lessons/new");

        await page.getByRole("link", { name: "Terug naar lessen" }).click();

        // Should navigate directly — no dialog
        await expect(page).toHaveURL(/\/dashboard\/lessons$/);
      });

      test("shows dialog when navigating away with dirty form", async ({ page }) => {
        await page.goto("/dashboard/lessons/new");

        // Type something to make form dirty
        await page.getByPlaceholder("Voorjaarslessen 2026").fill("Test");

        await page.getByRole("link", { name: "Terug naar lessen" }).click();

        await expect(page.getByText("Wizard verlaten?")).toBeVisible();
      });

      test("cancel in dialog keeps user on wizard", async ({ page }) => {
        await page.goto("/dashboard/lessons/new");
        await page.getByPlaceholder("Voorjaarslessen 2026").fill("Test");

        await page.getByRole("link", { name: "Terug naar lessen" }).click();
        await page.getByRole("button", { name: "Annuleren" }).click();

        await expect(page).toHaveURL(/\/dashboard\/lessons\/new/);
      });

      test("confirm in dialog navigates to lessons list", async ({ page }) => {
        await page.goto("/dashboard/lessons/new");
        await page.getByPlaceholder("Voorjaarslessen 2026").fill("Test");

        await page.getByRole("link", { name: "Terug naar lessen" }).click();
        await page.getByRole("button", { name: "Ja, stoppen" }).click();

        await page.waitForURL("**/dashboard/lessons", { timeout: 5000 });
        await expect(page).toHaveURL(/\/dashboard\/lessons$/);
      });
    });
  });

  test.describe("Detail", () => {
    test.beforeEach(async ({ page }) => {
      const seriesId = "66666666-6666-6666-6666-666666666666";
      await mockApi(page, "GET", `/lessonseries/${seriesId}`, TEST_SERIES[0]);
      await mockApi(page, "GET", `/lessonseries/${seriesId}/enrollments`, []);
    });

    test("shows series details and lessons", async ({ page }) => {
      await page.goto("/dashboard/lessons/66666666-6666-6666-6666-666666666666");

      await expect(page.getByText("Voorjaarslessen Beginners")).toBeVisible();
      await expect(page.getByText("Baan 1").first()).toBeVisible();
    });
  });
});
