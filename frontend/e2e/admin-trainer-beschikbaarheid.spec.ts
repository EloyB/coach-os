import { test, expect, type Page } from "@playwright/test";
import { TEST_CLUBS, TEST_TRAINERS, TEST_USER } from "./helpers";

// De beheerpagina leest /trainers?all=true en verwacht het volledige TrainerDto.
const MEMBERS = TEST_TRAINERS.map((trainer) => ({
  ...trainer,
  role: "Trainer",
  invitePending: false,
  lessonSeriesCount: 0,
  currentWeekHoursBooked: 0,
  weeklyCapacityHours: 20,
  notes: null,
  createdAt: "2026-01-01T00:00:00Z",
  headTrainerClubIds: [],
}));

// Host-onafhankelijk: .env.local kan NEXT_PUBLIC_API_URL op een LAN-adres zetten.
const API_BASE = "**/api";
const PIET = TEST_TRAINERS[1];
const JAN = TEST_TRAINERS[0];
const PIET_SLOT_ID = "aaaaaaaa-0000-0000-0000-000000000001";
const JAN_SLOT_ID = "aaaaaaaa-0000-0000-0000-000000000002";

const CORS = {
  "access-control-allow-origin": "*",
  "access-control-allow-headers": "*",
  "access-control-allow-methods": "GET,POST,PUT,DELETE,OPTIONS",
};

function slot(id: string, trainerId: string, startTime: string, endTime: string) {
  return { id, trainerId, tennisClubId: null, tennisClubName: null, dayOfWeek: 0, startTime, endTime };
}

interface Recorded {
  posts: unknown[];
  puts: { id: string; body: unknown }[];
  deletes: string[];
}

async function loginAsAdmin(page: Page, baseURL: string | undefined) {
  await page.context().addCookies([
    { name: "has_token", value: "1", url: baseURL! },
    { name: "user_role", value: "Admin", url: baseURL! },
  ]);
  await page.addInitScript((user) => {
    localStorage.setItem("token", user.token);
    localStorage.setItem("auth_user", JSON.stringify(user));
  }, TEST_USER);
}

async function mockAdminAvailabilityApi(page: Page): Promise<Recorded> {
  const recorded: Recorded = { posts: [], puts: [], deletes: [] };
  const slots = [
    slot(PIET_SLOT_ID, PIET.id, "09:00", "12:00"),
    slot(JAN_SLOT_ID, JAN.id, "18:00", "20:00"),
  ];
  const json = (body: unknown) => ({
    status: 200,
    contentType: "application/json",
    headers: CORS,
    body: JSON.stringify(body),
  });

  // Lage prioriteit (eerst geregistreerd): alles wat niet gemockt is raakt nooit de echte backend,
  // want een 401 daar logt de testgebruiker uit.
  await page.route(`${API_BASE}/**`, (route) => {
    if (route.request().method() === "OPTIONS") return route.fulfill({ status: 204, headers: CORS });
    return route.fulfill({ ...json({}), status: 404 });
  });
  await page.route(`${API_BASE}/subscription/status`, (route) =>
    route.fulfill(
      json({
        status: "Active",
        accessLevel: "Full",
        trialEndsAt: null,
        trialDaysRemaining: null,
        isTrialExpired: false,
        currentPeriodEnd: null,
        cancelAt: null,
      }),
    ),
  );
  await page.route(`${API_BASE}/tennisclubs`, (route) => route.fulfill(json(TEST_CLUBS)));
  await page.route((url) => url.pathname.endsWith("/api/trainers"), (route) =>
    route.request().method() === "OPTIONS"
      ? route.fulfill({ status: 204, headers: CORS })
      : route.fulfill(json(MEMBERS)),
  );
  await page.route(`${API_BASE}/trainer-availabilities`, (route) => {
    const method = route.request().method();
    if (method === "OPTIONS") return route.fulfill({ status: 204, headers: CORS });
    if (method === "POST") {
      recorded.posts.push(route.request().postDataJSON());
      return route.fulfill({ status: 201, contentType: "application/json", headers: CORS, body: '"new-id"' });
    }
    return route.fulfill({ status: 200, contentType: "application/json", headers: CORS, body: JSON.stringify(slots) });
  });
  await page.route(`${API_BASE}/trainer-availabilities/*`, (route) => {
    const method = route.request().method();
    const id = route.request().url().split("/").pop()!;
    if (method === "OPTIONS") return route.fulfill({ status: 204, headers: CORS });
    if (method === "PUT") recorded.puts.push({ id, body: route.request().postDataJSON() });
    if (method === "DELETE") recorded.deletes.push(id);
    return route.fulfill({ status: 204, headers: CORS });
  });
  return recorded;
}

async function openAvailabilityDialog(page: Page) {
  await page.goto("/dashboard/trainers");
  await page.getByRole("button", { name: "Acties voor Piet Pieters" }).click();
  await page.getByRole("button", { name: "Beschikbaarheid" }).click();
  await expect(page.getByRole("dialog", { name: /Beschikbaarheid van Piet Pieters/ })).toBeVisible();
}

test.describe("Admin: beschikbaarheid van een trainer (desktop)", () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page, baseURL }) => {
    await loginAsAdmin(page, baseURL);
  });

  test("toont het weekoverzicht met alleen de tijdvakken van die trainer", async ({ page }) => {
    await mockAdminAvailabilityApi(page);
    await openAvailabilityDialog(page);

    await expect(page.getByTestId("availability-week-grid")).toBeVisible();
    await expect(page.getByTestId("availability-block")).toHaveCount(1);
    await expect(page.getByTestId("availability-block")).toContainText("09:00 - 12:00");
  });

  test("een tijdvak wijzigen stuurt een PUT met het trainerId", async ({ page }) => {
    const recorded = await mockAdminAvailabilityApi(page);
    await openAvailabilityDialog(page);

    await page.getByTestId("availability-block").click();
    await page.getByLabel("Tot").fill("13:00");
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => recorded.puts.length).toBe(1);
    expect(recorded.puts[0]).toEqual({
      id: PIET_SLOT_ID,
      body: { trainerId: PIET.id, tennisClubId: null, dayOfWeek: 0, startTime: "09:00", endTime: "13:00" },
    });
  });

  test("klikken op een leeg stuk voegt een tijdvak toe voor die trainer", async ({ page }) => {
    const recorded = await mockAdminAvailabilityApi(page);
    await openAvailabilityDialog(page);

    await page.getByTestId("availability-column-1").click({ position: { x: 20, y: 320 } });
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => recorded.posts.length).toBe(1);
    expect(recorded.posts[0]).toMatchObject({ trainerId: PIET.id, dayOfWeek: 1 });
  });

  test("een tijdvak verwijderen stuurt een DELETE", async ({ page }) => {
    const recorded = await mockAdminAvailabilityApi(page);
    await openAvailabilityDialog(page);

    await page.getByTestId("availability-block").click();
    await page.getByRole("button", { name: "Verwijderen" }).click();

    await expect.poll(() => recorded.deletes).toEqual([PIET_SLOT_ID]);
  });
});

test.describe("Admin: beschikbaarheid van een trainer (mobiel)", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test("toont de lijst per weekdag", async ({ page, baseURL }) => {
    await loginAsAdmin(page, baseURL);
    await mockAdminAvailabilityApi(page);
    await openAvailabilityDialog(page);

    await expect(page.getByTestId("availability-day-list")).toBeVisible();
    await expect(page.getByTestId("availability-week-grid")).toBeHidden();
  });
});
