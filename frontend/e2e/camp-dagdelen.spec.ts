import { test, expect, type Page } from "@playwright/test";

// Host-onafhankelijk: .env.local kan NEXT_PUBLIC_API_URL op een LAN-adres zetten.
const API_BASE = "**/api";
const CAMP_ID = "aaaaaaaa-0000-0000-0000-000000000001";
const DAY_IDS = ["d1", "d2", "d3", "d4"].map(
  (d, i) => `${d}000000-0000-0000-0000-00000000000${i + 1}`,
);

const camp = {
  id: CAMP_ID,
  name: "Kerstkamp",
  description: null,
  level: null,
  dayPrice: 32.5,
  halfDayPrice: 20,
  allowHalfDays: true,
  startDate: "2026-12-21",
  endDate: "2026-12-24",
  registrationDeadline: "2026-12-01T00:00:00Z",
  tennisClubName: "TC De Aces",
  maxParticipants: 12,
  participantCount: 3,
  days: DAY_IDS.map((id, i) => ({
    id,
    date: `2026-12-${21 + i}`,
    morningStart: "09:15",
    morningEnd: "12:00",
    // Donderdag enkel voormiddag (zoals de club); maandag voormiddag is vol.
    afternoonStart: i === 3 ? null : "13:15",
    afternoonEnd: i === 3 ? null : "16:00",
    morningFull: i === 0,
    afternoonFull: false,
  })),
};

const CORS = {
  "access-control-allow-origin": "*",
  "access-control-allow-headers": "*",
  "access-control-allow-methods": "GET,POST,OPTIONS",
};

async function mockCampApi(page: Page, onEnroll?: (body: unknown) => void) {
  await page.route(`${API_BASE}/public/camps/${CAMP_ID}`, (route) =>
    route.fulfill({ status: 200, contentType: "application/json", headers: CORS, body: JSON.stringify(camp) }),
  );
  await page.route(`${API_BASE}/public/camps/${CAMP_ID}/form`, (route) =>
    route.fulfill({ status: 200, contentType: "application/json", headers: CORS, body: "null" }),
  );
  await page.route(`${API_BASE}/public/camps/${CAMP_ID}/enroll`, (route) => {
    if (route.request().method() === "OPTIONS")
      return route.fulfill({ status: 204, headers: CORS });
    onEnroll?.(route.request().postDataJSON());
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      headers: CORS,
      body: JSON.stringify({ campEnrollmentId: "e1", requiresPayment: true }),
    });
  });
  await page.route(`${API_BASE}/public/camps/${CAMP_ID}/payment-options*`, (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      headers: CORS,
      body: JSON.stringify({ price: 85, onlineAvailable: false }),
    }),
  );
}

test.describe("Kamp: dagdelen kiezen op het publieke formulier", () => {
  test("heel kamp kiest alle vrije dagdelen en toont het totaal", async ({ page }) => {
    await mockCampApi(page);
    await page.goto(`/camp/${CAMP_ID}`);

    await page.getByLabel("Ik kom heel het kamp").check();

    // Maandag voormiddag is vol: enkel namiddag (20), di en wo hele dag (2 x 32,50), do voormiddag (20).
    await expect(page.getByText("€105,00")).toBeVisible();
  });

  test("een dagdeel aanpassen herrekent het totaal; vol dagdeel kan niet gekozen worden", async ({ page }) => {
    await mockCampApi(page);
    await page.goto(`/camp/${CAMP_ID}`);

    const tuesday = page.getByRole("group").filter({ hasText: "22" }).first();
    await tuesday.getByLabel("Hele dag").check();
    await expect(page.getByText("€32,50").last()).toBeVisible();

    await tuesday.getByLabel(/Voormiddag/).check();
    await expect(page.getByText("€20,00").last()).toBeVisible();

    // Maandag: voormiddag vol -> geen "Hele dag" of "Voormiddag" keuze, wel een melding.
    const monday = page.getByRole("group").filter({ hasText: "21" }).first();
    await expect(monday.getByLabel("Hele dag")).toHaveCount(0);
    await expect(monday.getByText(/Voormiddag vol/)).toBeVisible();
    await expect(monday.getByLabel(/Namiddag/)).toBeVisible();
  });

  test("inschrijven stuurt de gekozen dagdelen mee en leidt naar de betaalpagina", async ({ page }) => {
    let posted: { days?: unknown[] } | undefined;
    await mockCampApi(page, (body) => {
      posted = body as { days?: unknown[] };
    });
    await page.goto(`/camp/${CAMP_ID}`);

    await page.getByLabel("Ik kom heel het kamp").check();
    const textboxes = page.getByRole("textbox");
    await textboxes.nth(0).fill("Emma"); // voornaam
    await textboxes.nth(1).fill("Peeters"); // achternaam
    await textboxes.nth(2).fill("emma@example.com"); // e-mailadres
    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect(page).toHaveURL(/camp-enrollment\/payment/);
    expect(posted?.days).toEqual([
      { campDayId: DAY_IDS[0], morning: false, afternoon: true },
      { campDayId: DAY_IDS[1], morning: true, afternoon: true },
      { campDayId: DAY_IDS[2], morning: true, afternoon: true },
      { campDayId: DAY_IDS[3], morning: true, afternoon: false },
    ]);
  });

  test("zonder gekozen dag toont het formulier een fout", async ({ page }) => {
    await mockCampApi(page);
    await page.goto(`/camp/${CAMP_ID}`);

    const textboxes = page.getByRole("textbox");
    await textboxes.nth(0).fill("Emma"); // voornaam
    await textboxes.nth(1).fill("Peeters"); // achternaam
    await textboxes.nth(2).fill("emma@example.com"); // e-mailadres
    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect(page.getByText("Kies minstens één dag")).toBeVisible();
  });
});
