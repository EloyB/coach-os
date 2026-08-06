const { chromium } = require("@playwright/test");

const SERIES_ID = "66666666-6666-6666-6666-666666666666";
const SLOT_ID = "11111111-2222-3333-4444-555555555555";
const GROUP_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const ASSIGNMENT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const USER = {
  token: "fake-jwt-token-for-testing",
  user: {
    userId: "11111111-1111-1111-1111-111111111111",
    email: "coach@test.be",
    firstName: "Jan",
    lastName: "Janssen",
    organizationId: "22222222-2222-2222-2222-222222222222",
    role: "Admin",
  },
};
const series = {
  id: SERIES_ID,
  organizationId: USER.user.organizationId,
  trainerId: "33333333-3333-3333-3333-333333333333",
  trainerName: "Jan Janssen",
  name: "Voorjaarslessen Beginners",
  description: "Lessen voor beginners",
  level: 1,
  price: 150,
  startDate: "2026-04-01",
  endDate: "2026-06-30",
  durationMinutes: 60,
  isActive: true,
  tennisClubId: "55555555-5555-5555-5555-555555555555",
  tennisClubName: "TC De Aces",
  tennisClubAddress: "Sportlaan 1, Antwerpen",
  lessonCount: 0,
  createdAt: "2026-03-15T10:00:00Z",
  lessons: [],
};
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

async function login(page) {
  await page.goto("http://localhost:5317/login");
  await page.evaluate(({ token, user }) => {
    localStorage.setItem("token", token);
    localStorage.setItem("auth_user", JSON.stringify(user));
    document.cookie = "has_token=1; path=/; SameSite=Lax";
  }, USER);
}

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  let currentPlanningOverview = planningOverview;

  await page.route(`**/api/lessonseries/${SERIES_ID}`, (route) =>
    route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(series) })
  );
  await page.route(`**/api/lessonseries/${SERIES_ID}/planning`, (route) =>
    route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(currentPlanningOverview) })
  );
  await page.route(`**/api/lessonseries/${SERIES_ID}/planning/assignments/${ASSIGNMENT_ID}/lock`, (route) => {
    currentPlanningOverview = {
      ...currentPlanningOverview,
      assignments: currentPlanningOverview.assignments.map((assignment) =>
        assignment.id === ASSIGNMENT_ID ? { ...assignment, isLocked: true } : assignment
      ),
    };
    return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(currentPlanningOverview.assignments[0]) });
  });
  await page.route(`**/api/lessonseries/${SERIES_ID}/planning/assignments/${ASSIGNMENT_ID}/send-confirmation`, (route) => {
    currentPlanningOverview = {
      ...currentPlanningOverview,
      assignments: currentPlanningOverview.assignments.map((assignment) =>
        assignment.id === ASSIGNMENT_ID
          ? { ...assignment, status: "AwaitingConfirmation", isLocked: true }
          : assignment
      ),
    };
    return route.fulfill({ status: 200, contentType: "application/json", body: "true" });
  });

  await login(page);
  await page.goto(`http://localhost:5317/dashboard/lessons/${SERIES_ID}/planning`);

  // Screenshot 1: lock action + result state.
  await page.getByRole("button", { name: "Vastzetten" }).click();
  await page.getByText("Vastgezet").first().waitFor({ state: "visible" });
  await page.getByText("Blijft behouden bij opnieuw genereren").waitFor({ state: "visible" });
  await page.screenshot({ path: "../pr-screenshots/issue-178-planning-lock.png", fullPage: true });

  // Reset to Proposed, then screenshot 2: definitive offer action is discoverable.
  currentPlanningOverview = planningOverview;
  await page.reload();
  await page.getByText("Anna Peeters").hover();
  await page.getByText("Definitief aanbieden").waitFor({ state: "visible" });
  await page.screenshot({ path: "../pr-screenshots/issue-178-planning-offer-confirmation.png", fullPage: true });

  await browser.close();
})();
