import { test, expect } from "@playwright/test";
import { loginViaStorage, mockApi, TEST_TRAINERS } from "./helpers";

const STANDALONE_LESSON_ID = "11111111-aaaa-bbbb-cccc-111111111111";
const NEW_LESSON_ID = "11111111-aaaa-bbbb-cccc-222222222222";

const STANDALONE_LESSON_DETAIL = {
  id: STANDALONE_LESSON_ID,
  date: "2026-06-09",
  startTime: "10:00",
  endTime: "11:00",
  durationMinutes: 60,
  courtName: "Baan 1",
  level: 2,
  trainerId: TEST_TRAINERS[0].id,
  trainerName: "Jan Janssen",
  maxParticipants: 4,
  notes: null,
  isCancelled: false,
  invitations: [],
};

test.describe("Lesson reschedule — standalone lesson", () => {
  test("shows reschedule button when lesson is not cancelled", async ({
    page,
  }) => {
    await loginViaStorage(page);
    await mockApi(
      page,
      "GET",
      `/standalone-lessons/${STANDALONE_LESSON_ID}`,
      STANDALONE_LESSON_DETAIL
    );

    await page.goto(`/dashboard/standalone-lessons/${STANDALONE_LESSON_ID}`);

    await expect(
      page.getByRole("button", { name: /Verplaatsen/ })
    ).toBeVisible();
  });

  test("opens reschedule dialog and posts new date + reason", async ({
    page,
  }) => {
    await loginViaStorage(page);
    await mockApi(
      page,
      "GET",
      `/standalone-lessons/${STANDALONE_LESSON_ID}`,
      STANDALONE_LESSON_DETAIL
    );
    await mockApi(
      page,
      "POST",
      `/lessons/${STANDALONE_LESSON_ID}/reschedule`,
      { newLessonId: NEW_LESSON_ID, notifiedCount: 2 }
    );

    await page.goto(`/dashboard/standalone-lessons/${STANDALONE_LESSON_ID}`);

    await page.getByRole("button", { name: /Verplaatsen/ }).click();

    await expect(page.getByText("Lesmoment verplaatsen")).toBeVisible();
    await expect(page.getByText("Nieuwe datum", { exact: true })).toBeVisible();
    await expect(page.getByText("Starttijd", { exact: true })).toBeVisible();

    // Reden invullen
    await page
      .getByPlaceholder(/Andere zaal beschikbaar/)
      .fill("Trainer ziek");

    // Submit (bovenste actie in de dialog)
    await page.getByRole("button", { name: "Verplaats les" }).click();

    // Success toast met aantal genotifieerd
    await expect(page.getByText(/2 deelnemer/)).toBeVisible();
  });

  test("cancel dialog has reason textarea", async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(
      page,
      "GET",
      `/standalone-lessons/${STANDALONE_LESSON_ID}`,
      STANDALONE_LESSON_DETAIL
    );

    await page.goto(`/dashboard/standalone-lessons/${STANDALONE_LESSON_ID}`);

    await page.getByRole("button", { name: /Les annuleren/ }).click();

    await expect(page.getByText("Les annuleren?")).toBeVisible();
    await expect(
      page.getByPlaceholder(/Trainer ziek — wordt meegezonden/)
    ).toBeVisible();
  });
});
