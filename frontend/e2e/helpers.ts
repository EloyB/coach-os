import { Page, Route } from "@playwright/test";

const API_URL = "http://localhost:5142/api";

// ─── Mock data ──────────────────────────────────────────────────────────────

export const TEST_USER = {
  token: "fake-jwt-token-for-testing",
  expiresAt: "2099-01-01T00:00:00Z",
  userId: "11111111-1111-1111-1111-111111111111",
  email: "coach@test.be",
  firstName: "Jan",
  lastName: "Janssen",
  organizationId: "22222222-2222-2222-2222-222222222222",
  role: "Admin",
};

export const TEST_TRAINERS = [
  {
    id: "33333333-3333-3333-3333-333333333333",
    firstName: "Jan",
    lastName: "Janssen",
    email: "coach@test.be",
    isActive: true,
    inviteAccepted: true,
  },
  {
    id: "44444444-4444-4444-4444-444444444444",
    firstName: "Piet",
    lastName: "Pieters",
    email: "piet@test.be",
    isActive: true,
    inviteAccepted: true,
  },
];

export const TEST_CLUBS = [
  {
    id: "55555555-5555-5555-5555-555555555555",
    name: "TC De Aces",
    address: "Sportlaan 1, Antwerpen",
    organizationId: TEST_USER.organizationId,
  },
];

export const TEST_SERIES: unknown[] = [
  {
    id: "66666666-6666-6666-6666-666666666666",
    organizationId: TEST_USER.organizationId,
    trainerId: TEST_TRAINERS[0].id,
    trainerName: "Jan Janssen",
    name: "Voorjaarslessen Beginners",
    description: "Lessen voor beginners",
    level: 1,
    price: 150,
    startDate: "2026-04-01",
    endDate: "2026-06-30",
    durationMinutes: 60,
    isActive: true,
    tennisClubId: TEST_CLUBS[0].id,
    tennisClubName: "TC De Aces",
    tennisClubAddress: "Sportlaan 1, Antwerpen",
    lessonCount: 2,
    createdAt: "2026-03-15T10:00:00Z",
    lessons: [
      {
        id: "77777777-7777-7777-7777-777777777777",
        lessonSeriesId: "66666666-6666-6666-6666-666666666666",
        date: "2026-04-07",
        startTime: "10:00",
        endTime: "11:00",
        courtName: "Baan 1",
        maxStudents: 8,
        notes: null,
        isCancelled: false,
      },
      {
        id: "88888888-8888-8888-8888-888888888888",
        lessonSeriesId: "66666666-6666-6666-6666-666666666666",
        date: "2026-04-14",
        startTime: "10:00",
        endTime: "11:00",
        courtName: "Baan 1",
        maxStudents: 8,
        notes: null,
        isCancelled: false,
      },
    ],
  },
];

export const TEST_PUBLIC_SERIES = {
  id: "66666666-6666-6666-6666-666666666666",
  name: "Voorjaarslessen Beginners",
  description: "Lessen voor beginners",
  level: 1,
  price: 150,
  startDate: "2026-04-01",
  endDate: "2026-06-30",
  durationMinutes: 60,
  tennisClubName: "TC De Aces",
  tennisClubAddress: "Sportlaan 1, Antwerpen",
  trainerName: "Jan Janssen",
  enrollmentCount: 3,
  lessons: [
    {
      id: "77777777-7777-7777-7777-777777777777",
      lessonSeriesId: "66666666-6666-6666-6666-666666666666",
      date: "2026-04-07",
      startTime: "10:00",
      endTime: "11:00",
      courtName: "Baan 1",
      maxStudents: 8,
      notes: null,
      isCancelled: false,
    },
  ],
};

// ─── Auth helpers ───────────────────────────────────────────────────────────

export async function loginViaStorage(page: Page): Promise<void> {
  await page.goto("/login");
  await page.evaluate(
    ({ token, user }) => {
      localStorage.setItem("token", token);
      localStorage.setItem("auth_user", JSON.stringify(user));
      document.cookie = "has_token=1; path=/; SameSite=Lax";
    },
    {
      token: TEST_USER.token,
      user: {
        userId: TEST_USER.userId,
        email: TEST_USER.email,
        firstName: TEST_USER.firstName,
        lastName: TEST_USER.lastName,
        organizationId: TEST_USER.organizationId,
        role: TEST_USER.role,
      },
    }
  );
}

// ─── API mock helpers ───────────────────────────────────────────────────────

export async function mockApi(
  page: Page,
  method: string,
  path: string,
  response: unknown,
  status = 200
): Promise<void> {
  const url = `${API_URL}${path}`;
  await page.route(`**${path}*`, (route: Route) => {
    if (
      route.request().method() === method.toUpperCase() &&
      route.request().url().includes(url)
    ) {
      return route.fulfill({
        status,
        contentType: "application/json",
        body: JSON.stringify(response),
      });
    }
    return route.continue();
  });
}

export const TEST_DASHBOARD_SUMMARY = {
  activeSeriesCount: 1,
  lessonsThisWeekCount: 2,
  totalEnrollmentCount: 5,
  activeTrainerCount: 2,
  tennisClubCount: 1,
  upcomingLessons: [
    {
      id: "77777777-7777-7777-7777-777777777777",
      seriesName: "Voorjaarslessen Beginners",
      date: "2026-04-07",
      startTime: "10:00",
      endTime: "11:00",
      courtName: "Baan 1",
      trainerName: "Jan Janssen",
    },
  ],
};

export async function mockAllDashboardApis(page: Page): Promise<void> {
  await mockApi(page, "GET", "/dashboard", TEST_DASHBOARD_SUMMARY);
  await mockApi(page, "GET", "/lessonseries", TEST_SERIES);
  await mockApi(page, "GET", "/trainers", TEST_TRAINERS);
  await mockApi(page, "GET", "/tennisclubs", TEST_CLUBS);
}
