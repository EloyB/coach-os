import { test, expect } from "@playwright/test";
import { loginViaStorage, mockApi, TEST_TRAINERS } from "./helpers";

const LESSON_ID = "11111111-aaaa-bbbb-cccc-111111111111";
const INV_PENDING_ID = "22222222-aaaa-bbbb-cccc-111111111111";
const INV_ACCEPTED_ID = "33333333-aaaa-bbbb-cccc-111111111111";
const INV_DECLINED_ID = "44444444-aaaa-bbbb-cccc-111111111111";

const TEST_LESSON_LIST = [
  {
    id: LESSON_ID,
    date: "2026-05-09",
    startTime: "21:00",
    endTime: "22:00",
    courtName: "Baan 1",
    level: 2,
    trainerId: TEST_TRAINERS[0].id,
    trainerName: "Jan Janssen",
    maxParticipants: 4,
    invitedCount: 3,
    acceptedCount: 1,
    isCancelled: false,
  },
];

const TEST_LESSON_DETAIL = {
  id: LESSON_ID,
  date: "2026-05-09",
  startTime: "21:00",
  endTime: "22:00",
  durationMinutes: 60,
  courtName: "Baan 1",
  level: 2,
  trainerId: TEST_TRAINERS[0].id,
  trainerName: "Jan Janssen",
  maxParticipants: 4,
  notes: "Korte clinic op verzoek.",
  isCancelled: false,
  invitations: [
    {
      id: INV_PENDING_ID,
      email: "ben@test.com",
      firstName: null,
      status: 0,
      invitationSentAt: "2026-05-04T10:00:00Z",
      respondedAt: null,
    },
    {
      id: INV_ACCEPTED_ID,
      email: "tom@test.com",
      firstName: null,
      status: 1,
      invitationSentAt: "2026-05-04T10:00:00Z",
      respondedAt: "2026-05-04T11:00:00Z",
    },
    {
      id: INV_DECLINED_ID,
      email: "anna@test.com",
      firstName: null,
      status: 2,
      invitationSentAt: "2026-05-04T10:00:00Z",
      respondedAt: "2026-05-04T11:30:00Z",
    },
  ],
};

const TEST_PUBLIC_INVITATION_PENDING = {
  status: 0,
  email: "ben@test.com",
  firstName: null,
  date: "2026-05-09",
  startTime: "21:00",
  endTime: "22:00",
  durationMinutes: 60,
  courtName: "Baan 1",
  level: 2,
  trainerName: "Jan Janssen",
  notes: "Korte clinic op verzoek.",
  isCancelled: false,
  respondedAt: null,
};

test.describe("Standalone lessons — list", () => {
  test("renders empty state when no lessons", async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", "/standalone-lessons", []);

    await page.goto("/dashboard/standalone-lessons");

    await expect(page.getByText("Nog geen losse lessen")).toBeVisible();
    await expect(
      page.getByRole("link", { name: /Nieuwe losse les/ }).first()
    ).toBeVisible();
  });

  test("renders list with lesson row", async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", "/standalone-lessons", TEST_LESSON_LIST);

    await page.goto("/dashboard/standalone-lessons");

    await expect(
      page.getByRole("heading", { name: "Losse lessen" })
    ).toBeVisible();
    await expect(page.getByText("Jan Janssen").first()).toBeVisible();
    await expect(page.getByText("Baan 1")).toBeVisible();
    await expect(page.getByText("21:00")).toBeVisible();
    await expect(page.getByText("Gemiddeld")).toBeVisible();
    await expect(page.getByText("1 / 3 bevestigd")).toBeVisible();
  });

  test("sidebar shows Losse lessen nav item", async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", "/standalone-lessons", []);
    await mockApi(page, "GET", "/dashboard", {
      activeSeriesCount: 0,
      lessonsThisWeekCount: 0,
      totalEnrollmentCount: 0,
      activeTrainerCount: 0,
      tennisClubCount: 0,
      upcomingLessons: [],
    });

    await page.goto("/dashboard");

    await expect(page.getByRole("link", { name: /Losse lessen/ })).toBeVisible();
  });
});

test.describe("Standalone lessons — create form", () => {
  test("renders all form fields", async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", "/trainers", TEST_TRAINERS);

    await page.goto("/dashboard/standalone-lessons/new");

    await expect(page.getByText("Losse les plannen")).toBeVisible();
    // Form labels (each contains text + required marker, so use regex match)
    await expect(page.getByText(/^Datum/).first()).toBeVisible();
    await expect(page.getByText(/^Starttijd/).first()).toBeVisible();
    await expect(page.getByText(/^Duur/).first()).toBeVisible();
    await expect(page.getByText(/^Baan/).first()).toBeVisible();
    await expect(page.getByText(/^Trainer/).first()).toBeVisible();
    await expect(page.getByText(/^Max\. deelnemers/).first()).toBeVisible();
    await expect(page.getByText(/^Notities/).first()).toBeVisible();
    await expect(page.getByText(/^Deelnemers/).first()).toBeVisible();
    await expect(
      page.getByRole("button", { name: /Plannen & uitnodigingen/ })
    ).toBeVisible();
  });
});

test.describe("Standalone lessons — detail page", () => {
  test("renders lesson info + invitation rows with statuses", async ({
    page,
  }) => {
    await loginViaStorage(page);
    await mockApi(
      page,
      "GET",
      `/standalone-lessons/${LESSON_ID}`,
      TEST_LESSON_DETAIL
    );

    await page.goto(`/dashboard/standalone-lessons/${LESSON_ID}`);

    // Lesson info
    await expect(page.getByText("Lesinfo")).toBeVisible();
    await expect(page.getByText("Korte clinic op verzoek.")).toBeVisible();
    // Three invitations with three different status badges
    await expect(page.getByText("ben@test.com")).toBeVisible();
    await expect(page.getByText("tom@test.com")).toBeVisible();
    await expect(page.getByText("anna@test.com")).toBeVisible();
    // status badges
    await expect(page.getByText("Bevestigd").first()).toBeVisible();
    await expect(page.getByText("Afgemeld").first()).toBeVisible();
    await expect(page.getByText("Uitgenodigd").first()).toBeVisible();
  });

  test("shows cancel button when not cancelled", async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(
      page,
      "GET",
      `/standalone-lessons/${LESSON_ID}`,
      TEST_LESSON_DETAIL
    );

    await page.goto(`/dashboard/standalone-lessons/${LESSON_ID}`);

    await expect(
      page.getByRole("button", { name: /Les annuleren/ })
    ).toBeVisible();
  });

  test("hides cancel button when lesson is cancelled", async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", `/standalone-lessons/${LESSON_ID}`, {
      ...TEST_LESSON_DETAIL,
      isCancelled: true,
    });

    await page.goto(`/dashboard/standalone-lessons/${LESSON_ID}`);

    await expect(page.getByText("Les is geannuleerd.")).toBeVisible();
    await expect(
      page.getByRole("button", { name: /Les annuleren/ })
    ).not.toBeVisible();
  });
});

test.describe("Public invitation page", () => {
  test("renders pending invitation with confirm + decline buttons", async ({
    page,
  }) => {
    await mockApi(
      page,
      "GET",
      "/public/invitations/test-token-123",
      TEST_PUBLIC_INVITATION_PENDING
    );

    await page.goto("/invitation/test-token-123");

    await expect(page.getByText("Je bent uitgenodigd voor een les").first()).toBeVisible();
    await expect(page.getByText("Jan Janssen")).toBeVisible();
    await expect(page.getByText("Baan 1")).toBeVisible();
    await expect(page.getByText("Korte clinic op verzoek.")).toBeVisible();
    await expect(page.getByRole("button", { name: /^Bevestigen$/ })).toBeVisible();
    await expect(page.getByRole("button", { name: /Helaas niet aanwezig/ })).toBeVisible();
  });

  test("renders accepted state", async ({ page }) => {
    await mockApi(page, "GET", "/public/invitations/test-token-accepted", {
      ...TEST_PUBLIC_INVITATION_PENDING,
      status: 1,
      respondedAt: "2026-05-04T11:00:00Z",
    });

    await page.goto("/invitation/test-token-accepted");

    await expect(page.getByText("Ingeschreven!")).toBeVisible();
  });

  test("renders declined state", async ({ page }) => {
    await mockApi(page, "GET", "/public/invitations/test-token-declined", {
      ...TEST_PUBLIC_INVITATION_PENDING,
      status: 2,
      respondedAt: "2026-05-04T11:00:00Z",
    });

    await page.goto("/invitation/test-token-declined");

    await expect(page.getByText("Bedankt voor je antwoord")).toBeVisible();
  });

  test("renders not-found when API returns 404", async ({ page }) => {
    await page.route(
      "**/public/invitations/bad-token*",
      (route) =>
        route.fulfill({
          status: 404,
          contentType: "application/json",
          body: JSON.stringify(["Uitnodiging niet gevonden."]),
        })
    );

    await page.goto("/invitation/bad-token");

    await expect(
      page.getByRole("heading", { name: "Uitnodiging niet gevonden" })
    ).toBeVisible();
  });
});
