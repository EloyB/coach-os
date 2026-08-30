import { test, expect } from "@playwright/test";
import { loginViaStorage, mockApi, TEST_SERIES } from "./helpers";

const SERIES_ID = "66666666-6666-6666-6666-666666666666";
const SLOT_ID = "11111111-2222-3333-4444-555555555555";
const GROUP_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const ASSIGNMENT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const planningOverview = {
  planningStatus: "Planning",
  planningLastEditedAt: "2026-08-05T12:00:00Z",
  timeSlots: [
    {
      id: SLOT_ID,
      dayOfWeek: 0,
      startTime: "18:00",
      endTime: "19:00",
      courtName: "Baan 1",
      trainerId: null,
      trainerName: null,
      maxCapacity: 4,
    },
  ],
  enrollments: [
    {
      id: "e1111111-1111-1111-1111-111111111111",
      studentName: "Anna Peeters",
      studentEmail: "anna@example.test",
      studentPhone: null,
      isOpenToGrouping: false,
      groupId: GROUP_ID,
      preferences: { [SLOT_ID]: "Preferred" },
    },
    {
      id: "e2222222-2222-2222-2222-222222222222",
      studentName: "Bram Janssens",
      studentEmail: "bram@example.test",
      studentPhone: null,
      isOpenToGrouping: false,
      groupId: GROUP_ID,
      preferences: { [SLOT_ID]: "Preferred" },
    },
  ],
  groups: [
    {
      id: GROUP_ID,
      name: "Groep Anna",
      leaderEnrollmentId: "e1111111-1111-1111-1111-111111111111",
      memberEnrollmentIds: [
        "e1111111-1111-1111-1111-111111111111",
        "e2222222-2222-2222-2222-222222222222",
      ],
    },
  ],
  assignments: [
    {
      id: ASSIGNMENT_ID,
      timeSlotId: SLOT_ID,
      enrollmentId: null,
      groupId: GROUP_ID,
      status: "Proposed",
      isAutoMerged: false,
      isLocked: false,
    },
  ],
  conflicts: [],
};

test.describe("planning assignment locking", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", `/lessonseries/${SERIES_ID}`, TEST_SERIES[0]);
    await mockApi(page, "GET", `/lessonseries/${SERIES_ID}/planning`, planningOverview);
  });

  test("shows lock action for proposed assignment and calls lock endpoint", async ({ page }) => {
    let lockCalled = false;
    await page.route(`**/lessonseries/${SERIES_ID}/planning/assignments/${ASSIGNMENT_ID}/lock`, async (route) => {
      if (route.request().method() === "POST") {
        lockCalled = true;
        return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ ...planningOverview.assignments[0], isLocked: true }) });
      }
      return route.continue();
    });

    await page.goto(`/dashboard/lessons/${SERIES_ID}/planning`);

    await expect(page.getByText("Zet een volledige groep vast zodra het voorstel klopt")).toBeVisible();
    await page.getByText("Anna Peeters").hover();
    await page.getByRole("button", { name: "Vastzetten" }).click();

    await expect.poll(() => lockCalled).toBe(true);
  });

  test("clearly marks a locked group as preserved on regenerate", async ({ page }) => {
    await mockApi(page, "GET", `/lessonseries/${SERIES_ID}/planning`, {
      ...planningOverview,
      assignments: [{ ...planningOverview.assignments[0], isLocked: true }],
    });

    await page.goto(`/dashboard/lessons/${SERIES_ID}/planning`);

    await expect(page.getByText("1 vastgezet")).toBeVisible();
    await expect(page.getByText("Vastgezet").first()).toBeVisible();
    await expect(page.getByText("Blijft behouden bij opnieuw genereren")).toBeVisible();
    await expect(page.getByRole("button", { name: "Vrijgeven" }).first()).toBeVisible();
  });

  test("offers a proposed group definitively and calls send-confirmation endpoint", async ({ page }) => {
    let sendCalled = false;
    await page.route(`**/lessonseries/${SERIES_ID}/planning/assignments/${ASSIGNMENT_ID}/send-confirmation`, async (route) => {
      if (route.request().method() === "POST") {
        sendCalled = true;
        return route.fulfill({ status: 200, contentType: "application/json", body: "true" });
      }
      return route.continue();
    });

    await page.goto(`/dashboard/lessons/${SERIES_ID}/planning`);

    await expect(page.getByText("Definitief aanbieden")).toBeVisible();
    await page.getByText("Definitief aanbieden").click();

    await expect.poll(() => sendCalled).toBe(true);
  });
});
