import { Page, Route } from "@playwright/test";
import type { AuthResponse } from "@/lib/api/auth";
import type { DashboardSummaryDto, MyCampDayDto, MyLessonDto } from "@/lib/api/dashboard";
import type { OnboardingStateDto } from "@/lib/api/onboarding";
import type { PublicLessonSeriesDto } from "@/lib/api/enrollments";
import type { LessonSeriesDto } from "@/lib/api/lessonSeries";
import type { TennisClubDto } from "@/lib/api/tennisClubs";
import type { TrainerDto } from "@/lib/api/trainers";

const API_URL = "http://localhost:5142/api";

// ─── Mock data ──────────────────────────────────────────────────────────────
// Typed against the DTOs in lib/api/*.ts: when the API contract changes,
// `bun run typecheck:e2e` fails here instead of a page crashing mid-test.

export const TEST_USER = {
  token: "fake-jwt-token-for-testing",
  expiresAt: "2099-01-01T00:00:00Z",
  userId: "11111111-1111-1111-1111-111111111111",
  email: "coach@test.be",
  firstName: "Jan",
  lastName: "Janssen",
  organizationId: "22222222-2222-2222-2222-222222222222",
  role: "Admin",
  memberships: [
    {
      organizationId: "22222222-2222-2222-2222-222222222222",
      organizationName: "TC De Aces",
      role: "Admin",
      isActive: true,
    },
  ],
} satisfies AuthResponse;

export const TEST_TRAINERS: TrainerDto[] = [
  {
    id: "33333333-3333-3333-3333-333333333333",
    firstName: "Jan",
    lastName: "Janssen",
    email: "coach@test.be",
    role: "Admin",
    isActive: true,
    invitePending: false,
    lessonSeriesCount: 1,
    currentWeekHoursBooked: 2,
    weeklyCapacityHours: 20,
    notes: null,
    createdAt: "2026-01-01T10:00:00Z",
    headTrainerClubIds: [],
  },
  {
    id: "44444444-4444-4444-4444-444444444444",
    firstName: "Piet",
    lastName: "Pieters",
    email: "piet@test.be",
    role: "Trainer",
    isActive: true,
    invitePending: false,
    lessonSeriesCount: 0,
    currentWeekHoursBooked: 0,
    weeklyCapacityHours: 10,
    notes: null,
    createdAt: "2026-01-02T10:00:00Z",
    headTrainerClubIds: [],
  },
];

export const TEST_CLUBS: TennisClubDto[] = [
  {
    id: "55555555-5555-5555-5555-555555555555",
    name: "TC De Aces",
    address: "Sportlaan 1, Antwerpen",
  },
];

export const TEST_SERIES: LessonSeriesDto[] = [
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
    enrolledCount: 5,
    totalCapacity: 16,
    minAge: 6,
    maxAge: 99,
    allowSoloEnrollment: true,
    allowGroupEnrollment: true,
    acceptOnlinePayment: false,
    acceptManualPayment: true,
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
        isCancelled: false,
      },
    ],
  },
];

export const TEST_PUBLIC_SERIES: PublicLessonSeriesDto = {
  id: "66666666-6666-6666-6666-666666666666",
  name: "Voorjaarslessen Beginners",
  description: "Lessen voor beginners",
  level: 1,
  price: 150,
  startDate: "2026-04-01",
  endDate: "2026-06-30",
  durationMinutes: 60,
  tennisClubName: "TC De Aces",
  trainerName: "Jan Janssen",
  enrollmentCount: 3,
  maxRegistrations: 12,
  minAge: 6,
  maxAge: 99,
  allowSoloEnrollment: true,
  allowGroupEnrollment: true,
  priceOptions: [],
  lessons: [
    {
      id: "77777777-7777-7777-7777-777777777777",
      lessonSeriesId: "66666666-6666-6666-6666-666666666666",
      date: "2026-04-07",
      startTime: "10:00",
      endTime: "11:00",
      courtName: "Baan 1",
      maxStudents: 8,
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

export const TEST_DASHBOARD_SUMMARY: DashboardSummaryDto = {
  activeSeriesCount: 1,
  lessonsTodayCount: 0,
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

export const TEST_ONBOARDING_DONE: OnboardingStateDto = {
  shouldShow: false,
  allCompleted: true,
  steps: [],
  startedAt: null,
  dismissedAt: null,
};

export async function mockAllDashboardApis(page: Page): Promise<void> {
  await mockApi(page, "GET", "/dashboard", TEST_DASHBOARD_SUMMARY);
  // Agenda op "Vandaag": leeg, zodat de pagina niet afhangt van de echte datum.
  const noLessons: MyLessonDto[] = [];
  const noCampDays: MyCampDayDto[] = [];
  await mockApi(page, "GET", "/dashboard/my-lessons", noLessons);
  await mockApi(page, "GET", "/dashboard/organization-lessons", noLessons);
  await mockApi(page, "GET", "/dashboard/my-camp-days", noCampDays);
  await mockApi(page, "GET", "/dashboard/organization-camp-days", noCampDays);
  await mockApi(page, "GET", "/onboarding/state", TEST_ONBOARDING_DONE);
  await mockApi(page, "GET", "/lessonseries", TEST_SERIES);
  await mockApi(page, "GET", "/trainers", TEST_TRAINERS);
  await mockApi(page, "GET", "/tennisclubs", TEST_CLUBS);
}
