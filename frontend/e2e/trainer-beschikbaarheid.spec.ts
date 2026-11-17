import { test, expect, type Page } from "@playwright/test";

// Host-onafhankelijk: .env.local kan NEXT_PUBLIC_API_URL op een LAN-adres zetten.
const API_BASE = "**/api";
const CLUB_ID = "55555555-5555-5555-5555-555555555555";
const SLOT_ID = "aaaaaaaa-0000-0000-0000-000000000001";

const CORS = {
  "access-control-allow-origin": "*",
  "access-control-allow-headers": "*",
  "access-control-allow-methods": "GET,POST,PUT,DELETE,OPTIONS",
};

interface Slot {
  id: string;
  trainerId: string;
  tennisClubId: string | null;
  tennisClubName: string | null;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

function mondaySlot(overrides: Partial<Slot> = {}): Slot {
  return {
    id: SLOT_ID,
    trainerId: "t1",
    tennisClubId: null,
    tennisClubName: null,
    dayOfWeek: 0,
    startTime: "09:00",
    endTime: "12:00",
    ...overrides,
  };
}

interface Recorded {
  posts: unknown[];
  puts: { id: string; body: unknown }[];
  deletes: string[];
}

async function loginAsTrainer(page: Page, baseURL: string | undefined) {
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
}

async function mockTrainerApi(
  page: Page,
  slots: Slot[],
  options: { postStatus?: number; postBody?: unknown } = {},
): Promise<Recorded> {
  const recorded: Recorded = { posts: [], puts: [], deletes: [] };
  const json = (body: unknown, status = 200) => ({
    status,
    contentType: "application/json",
    headers: CORS,
    body: JSON.stringify(body),
  });

  // Lage prioriteit (eerst geregistreerd): alles wat niet gemockt is raakt nooit de echte backend,
  // want een 401 daar logt de testgebruiker uit.
  await page.route(`${API_BASE}/**`, (route) => {
    if (route.request().method() === "OPTIONS") return route.fulfill({ status: 204, headers: CORS });
    return route.fulfill(json({}, 404));
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
  await page.route(`${API_BASE}/tennisclubs`, (route) =>
    route.fulfill(
      json([{ id: CLUB_ID, name: "TC De Aces", address: "Sportlaan 1" }]),
    ),
  );
  await page.route(`${API_BASE}/me/trainer-availabilities`, (route) => {
    const method = route.request().method();
    if (method === "OPTIONS") return route.fulfill({ status: 204, headers: CORS });
    if (method === "POST") {
      recorded.posts.push(route.request().postDataJSON());
      return route.fulfill(json(options.postBody ?? "new-id", options.postStatus ?? 201));
    }
    return route.fulfill(json(slots));
  });
  await page.route(`${API_BASE}/me/trainer-availabilities/*`, (route) => {
    const method = route.request().method();
    const id = route.request().url().split("/").pop()!;
    if (method === "OPTIONS") return route.fulfill({ status: 204, headers: CORS });
    if (method === "PUT") {
      recorded.puts.push({ id, body: route.request().postDataJSON() });
    } else if (method === "DELETE") {
      recorded.deletes.push(id);
    }
    return route.fulfill({ status: 204, headers: CORS });
  });
  return recorded;
}

test.describe("Trainer: beschikbaarheid (desktop)", () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page, baseURL }) => {
    await loginAsTrainer(page, baseURL);
  });

  test("toont het weekoverzicht met het bestaande tijdvak", async ({ page }) => {
    await mockTrainerApi(page, [mondaySlot()]);
    await page.goto("/dashboard/trainer-settings");

    await expect(page.getByTestId("availability-week-grid")).toBeVisible();
    await expect(page.getByTestId("availability-day-list")).toBeHidden();
    await expect(page.getByTestId("availability-block")).toContainText("09:00 - 12:00");
  });

  test("een tijdvak wijzigen stuurt een PUT met de nieuwe eindtijd", async ({ page }) => {
    const recorded = await mockTrainerApi(page, [mondaySlot()]);
    await page.goto("/dashboard/trainer-settings");

    await page.getByTestId("availability-block").click();
    await expect(page.getByRole("heading", { name: "Tijdvak wijzigen" })).toBeVisible();
    await page.getByLabel("Tot").fill("13:00");
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => recorded.puts.length).toBe(1);
    expect(recorded.puts[0]).toEqual({
      id: SLOT_ID,
      body: { tennisClubId: null, dayOfWeek: 0, startTime: "09:00", endTime: "13:00" },
    });
  });

  test("klikken op een leeg stuk voegt een tijdvak toe, standaard voor elke club", async ({ page }) => {
    const recorded = await mockTrainerApi(page, [mondaySlot()]);
    await page.goto("/dashboard/trainer-settings");

    // Dinsdag, halverwege de as (07:00 tot 23:00) = 15:00 gesnapt.
    await page.getByTestId("availability-column-1").click({ position: { x: 20, y: 320 } });
    await expect(page.getByRole("heading", { name: "Tijdvak toevoegen" })).toBeVisible();
    await expect(page.getByLabel("Dag")).toHaveValue("1");
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => recorded.posts.length).toBe(1);
    expect(recorded.posts[0]).toEqual({
      tennisClubId: null,
      dayOfWeek: 1,
      startTime: "15:00",
      endTime: "16:00",
    });
  });

  test("een gekozen club wordt meegestuurd", async ({ page }) => {
    const recorded = await mockTrainerApi(page, []);
    await page.goto("/dashboard/trainer-settings");

    await page.getByTestId("availability-column-2").click({ position: { x: 20, y: 160 } });
    await page.getByLabel("Club").selectOption(CLUB_ID);
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => recorded.posts.length).toBe(1);
    expect(recorded.posts[0]).toMatchObject({ tennisClubId: CLUB_ID, dayOfWeek: 2 });
  });

  test("een overlappend tijdvak wordt aan de clientzijde geweigerd", async ({ page }) => {
    const recorded = await mockTrainerApi(page, [mondaySlot()]);
    await page.goto("/dashboard/trainer-settings");

    await page.getByTestId("availability-column-1").click({ position: { x: 20, y: 320 } });
    await page.getByLabel("Dag").selectOption("0");
    await page.getByLabel("Van").fill("10:00");
    await page.getByLabel("Tot").fill("11:00");
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect(page.getByRole("alert")).toContainText("overlapt met een ander tijdvak");
    expect(recorded.posts).toHaveLength(0);
  });

  test("een fout van de API wordt in het formulier getoond", async ({ page }) => {
    await mockTrainerApi(page, [], {
      postStatus: 409,
      postBody: ["Deze trainer heeft op deze dag al een beschikbaarheid die overlapt"],
    });
    await page.goto("/dashboard/trainer-settings");

    await page.getByTestId("availability-column-3").click({ position: { x: 20, y: 160 } });
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect(page.getByRole("alert")).toContainText("al een beschikbaarheid die overlapt");
  });

  test("verwijderen stuurt een DELETE", async ({ page }) => {
    const recorded = await mockTrainerApi(page, [mondaySlot()]);
    await page.goto("/dashboard/trainer-settings");

    await page.getByTestId("availability-block").click();
    await page.getByRole("button", { name: "Verwijderen" }).click();

    await expect.poll(() => recorded.deletes).toEqual([SLOT_ID]);
  });

  test("een tijdvak buiten 07:00-23:00 blijft zichtbaar", async ({ page }) => {
    await mockTrainerApi(page, [mondaySlot({ startTime: "06:00", endTime: "08:00" })]);
    await page.goto("/dashboard/trainer-settings");

    const block = page.getByTestId("availability-block");
    await expect(block).toContainText("06:00 - 08:00");
    expect((await block.boundingBox())?.height ?? 0).toBeGreaterThan(0);
    await expect(page.getByText("06u")).toBeVisible();
  });
});

test.describe("Trainer: beschikbaarheid (mobiel)", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test("toont een lijst per weekdag en voegt een tijdvak toe", async ({ page, baseURL }) => {
    await loginAsTrainer(page, baseURL);
    const recorded = await mockTrainerApi(page, [mondaySlot()]);
    await page.goto("/dashboard/trainer-settings");

    await expect(page.getByTestId("availability-day-list")).toBeVisible();
    await expect(page.getByTestId("availability-week-grid")).toBeHidden();
    await expect(page.getByTestId("availability-day-list").getByText("09:00 - 12:00")).toBeVisible();

    await page.getByRole("button", { name: "Tijdvak toevoegen" }).nth(1).click(); // Dinsdag
    await expect(page.getByLabel("Dag")).toHaveValue("1");
    await page.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => recorded.posts.length).toBe(1);
    expect(recorded.posts[0]).toMatchObject({ dayOfWeek: 1, tennisClubId: null });
  });
});
