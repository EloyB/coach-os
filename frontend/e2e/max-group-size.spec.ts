import { test, expect, type Page } from "@playwright/test";
import type { LessonSeriesEnrollmentDto, SubmitEnrollmentRequest } from "@/lib/api/enrollments";
import type { PlanningOverviewDto } from "@/lib/api/planning";
import { API_URL, TEST_PUBLIC_SERIES, loginViaStorage, mockApi, TEST_SERIES } from "./helpers";

// Maximum aantal personen per tijdslot (#387): de deelnemer kiest het bij het inschrijven, en de admin krijgt een
// waarschuwing (met doorgaan) als hij iemand in een groter slot zet.

const SERIES_ID = "66666666-6666-6666-6666-666666666666";

test.describe("Inschrijven: maximum aantal personen per les", () => {
  async function arrange(page: Page, maxGroupSize: number) {
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/timeslots`, (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: "[]" }));
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}`, { ...TEST_PUBLIC_SERIES, maxGroupSize });
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}/form`, null, 204);

    const submitted: { body?: SubmitEnrollmentRequest } = {};
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/enroll`, (route) => {
      if (route.request().method() !== "POST") return route.continue();
      submitted.body = route.request().postDataJSON() as SubmitEnrollmentRequest;
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify("enrollment-id") });
    });
    return submitted;
  }

  async function fillLeader(page: Page) {
    await page.goto(`/enroll/${SERIES_ID}`);
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill("Els");
    await inputs.nth(1).fill("Peeters");
    await page.locator('input[type="email"]').fill("els@example.com");
    await page.locator('input[type="tel"]').fill("0470 12 34 56");
    await page.getByLabel(/geboortedatum/i).first().fill("1985-01-01");
  }

  const choice = (page: Page) => page.getByRole("radiogroup", { name: "Maximum aantal personen per les" });

  test("solo kiest tussen solo, duo, trio en groep van 4 en start bij solo", async ({ page }) => {
    const submitted = await arrange(page, 4);
    await fillLeader(page);

    await expect(page.getByText("Met hoeveel personen wil je maximaal les volgen?")).toBeVisible();
    await expect(choice(page).getByRole("radio")).toHaveCount(4);
    await expect(choice(page).getByRole("radio", { name: /Solo/ })).toBeChecked();

    await choice(page).getByText("Trio").click();
    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.maxGroupSize).toBe(3);
  });

  test("zonder keuze blijft het maximum de eigen groepsgrootte", async ({ page }) => {
    const submitted = await arrange(page, 4);
    await fillLeader(page);

    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.maxGroupSize).toBe(1);
  });

  test("een groep van twee kan niet lager dan duo kiezen en de keuze volgt de groepsgrootte", async ({ page }) => {
    const submitted = await arrange(page, 4);
    await fillLeader(page);
    await page.getByText("Ik schrijf meerdere personen in").click();
    await page.getByRole("button", { name: "Lid toevoegen" }).click();
    await page.getByPlaceholder("Naam").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum 1/i).fill("2015-03-04");

    // Leider + lid = 2: "Solo" bestaat niet meer, "Duo" is de eigen groep.
    await expect(choice(page).getByRole("radio")).toHaveCount(3);
    await expect(choice(page).getByRole("radio", { name: /Solo/ })).toHaveCount(0);
    await expect(choice(page).getByRole("radio", { name: /Duo/ })).toBeChecked();
    await expect(page.getByText("Jullie zijn al met 2.")).toBeVisible();

    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.maxGroupSize).toBe(2);
  });

  test("een gekozen maximum schuift mee omhoog als de groep groter wordt", async ({ page }) => {
    await arrange(page, 4);
    await fillLeader(page);
    await page.getByText("Ik schrijf meerdere personen in").click();
    await page.getByRole("button", { name: "Lid toevoegen" }).click();
    await page.getByPlaceholder("Naam").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum 1/i).fill("2015-03-04");
    await expect(choice(page).getByRole("radio", { name: /Duo/ })).toBeChecked();

    await page.getByRole("button", { name: "Lid toevoegen" }).click();

    // Groep van drie: de laagste keuze is nu "Trio" en dat staat geselecteerd.
    await expect(choice(page).getByRole("radio", { name: /Solo/ })).toHaveCount(0);
    await expect(choice(page).getByRole("radio", { name: /Duo/ })).toHaveCount(0);
    await expect(choice(page).getByRole("radio", { name: /Trio/ })).toBeChecked();
  });

  test("een reeks die maar één persoon toelaat toont geen keuze en stuurt maximum 1", async ({ page }) => {
    const submitted = await arrange(page, 1);
    await fillLeader(page);

    await expect(page.getByText("Met hoeveel personen wil je maximaal les volgen?")).toHaveCount(0);
    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.maxGroupSize).toBe(1);
  });

  test("een reeks met maximum 2 laat geen trio toe", async ({ page }) => {
    await arrange(page, 2);
    await fillLeader(page);

    await expect(choice(page).getByRole("radio")).toHaveCount(2);
    await expect(choice(page).getByRole("radio", { name: /Trio/ })).toHaveCount(0);
  });
});

test.describe("Planning: tijdslot groter dan het gekozen maximum", () => {
  const SLOT_1 = "11111111-2222-3333-4444-555555555555";
  const SLOT_2 = "22222222-2222-3333-4444-555555555555";
  const ENROLLMENT_ID = "e1111111-1111-1111-1111-111111111111";
  const ASSIGNMENT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

  const overview: PlanningOverviewDto = {
    planningStatus: "Planning",
    planningLastEditedAt: "2026-08-05T12:00:00Z",
    timeSlots: [
      { id: SLOT_1, dayOfWeek: 0, startTime: "18:00", endTime: "19:00", courtName: "Baan 1", trainerId: null, trainerName: null, maxCapacity: 4 },
      { id: SLOT_2, dayOfWeek: 1, startTime: "18:00", endTime: "19:00", courtName: "Baan 1", trainerId: null, trainerName: null, maxCapacity: 4 },
    ],
    enrollments: [
      {
        id: ENROLLMENT_ID, studentName: "Anna Peeters", studentEmail: "anna@example.test", studentPhone: null,
        maxGroupSize: 2, groupId: null, preferences: { [SLOT_1]: "Preferred", [SLOT_2]: "Available" },
      },
    ],
    groups: [],
    assignments: [
      { id: ASSIGNMENT_ID, timeSlotId: SLOT_1, enrollmentId: ENROLLMENT_ID, groupId: null, status: "Proposed", isAutoMerged: false, isLocked: false },
    ],
  };

  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", `/lessonseries/${SERIES_ID}`, TEST_SERIES[0]);
    await mockApi(page, "GET", `/lessonseries/${SERIES_ID}/planning`, overview);
    await mockApi(page, "GET", `/lessonseries/${SERIES_ID}/enrollments`, []);
    await mockApi(page, "GET", `/lessonseries/${SERIES_ID}/planning/non-responders`, []);
    await mockApi(page, "GET", `/trainers`, []);
  });

  async function openExtraSlotChoice(page: Page) {
    await page.goto(`/dashboard/lessons/${SERIES_ID}/planning`);
    await page.getByRole("button", { name: /^Toegewezen/ }).click();
    await page.getByRole("button", { name: "Extra tijdslot" }).click();
    await page.getByRole("button", { name: /Di 18:00–19:00/ }).click();
  }

  test("vraagt bevestiging bij een 409 en plant daarna met bevestiging in", async ({ page }) => {
    const bodies: Record<string, unknown>[] = [];
    await page.route(`${API_URL}/lessonseries/${SERIES_ID}/planning/assignments`, (route) => {
      if (route.request().method() !== "POST") return route.continue();
      const body = route.request().postDataJSON() as Record<string, unknown>;
      bodies.push(body);
      if (!body.acknowledgeExceedsMaxGroupSize) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify(["Dit tijdslot zou 3 personen tellen, meer dan het gekozen maximum van: Anna Peeters (max. 2)."]),
        });
      }
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify("new-assignment-id") });
    });

    await openExtraSlotChoice(page);

    const dialog = page.getByRole("alertdialog");
    await expect(dialog.getByText("Groter dan het gekozen maximum")).toBeVisible();
    await expect(dialog.getByText("Anna Peeters (max. 2)")).toBeVisible();
    // Er is nog niets ingepland en er verscheen geen losse foutmelding.
    expect(bodies).toHaveLength(1);

    await dialog.getByRole("button", { name: "Toch inplannen" }).click();

    await expect.poll(() => bodies.length).toBe(2);
    expect(bodies[1]).toMatchObject({ weeklyTemplateEntryId: SLOT_2, acknowledgeExceedsMaxGroupSize: true });
  });

  test("annuleren plant niets in", async ({ page }) => {
    let calls = 0;
    await page.route(`${API_URL}/lessonseries/${SERIES_ID}/planning/assignments`, (route) => {
      if (route.request().method() !== "POST") return route.continue();
      calls++;
      return route.fulfill({
        status: 409,
        contentType: "application/json",
        body: JSON.stringify(["Dit tijdslot zou 3 personen tellen, meer dan het gekozen maximum van: Anna Peeters (max. 2)."]),
      });
    });

    await openExtraSlotChoice(page);
    const dialog = page.getByRole("alertdialog");
    await expect(dialog).toBeVisible();
    await dialog.getByRole("button", { name: "Annuleren" }).click();

    await expect(dialog).toHaveCount(0);
    expect(calls).toBe(1);
  });

  test("andere fouten (zoals te weinig plaats) tonen nog steeds een foutmelding in plaats van de bevestiging", async ({ page }) => {
    await page.route(`${API_URL}/lessonseries/${SERIES_ID}/planning/assignments`, (route) => {
      if (route.request().method() !== "POST") return route.continue();
      return route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify(["Tijdslot heeft geen plaats meer (4/4)."]),
      });
    });

    await openExtraSlotChoice(page);

    // Geen bevestigingsdialoog: de servermelding verschijnt zoals voorheen als foutmelding.
    await expect(page.getByText("Tijdslot heeft geen plaats meer (4/4).")).toBeVisible();
    await expect(page.getByRole("alertdialog")).toHaveCount(0);
  });
});

test.describe("Beheer: maximum per inschrijving", () => {
  const SERIES = "66666666-6666-6666-6666-666666666666";

  function enrollment(overrides: Partial<LessonSeriesEnrollmentDto>): LessonSeriesEnrollmentDto {
    return {
      id: "e0000000-0000-0000-0000-000000000001", studentName: "Anna Solo", studentEmail: "anna@example.test",
      studentPhone: "0470 12 34 56", contactEmail: "anna@example.test", hasOwnEmail: true, status: "Pending",
      enrolledAt: "2026-11-01T10:00:00Z", notes: null, dateOfBirth: "1990-01-01", category: 1, categoryLabel: "Volwassenen",
      enrollmentGroupId: null, isGroupLeader: false, maxGroupSize: 3, selectedPriceOptionId: null,
      paymentMethod: null, paymentStatus: null, paymentAmount: null, paidAt: null, molliePaymentId: null,
      isAwaitingConfirmation: false, formResponses: [], ...overrides,
    };
  }

  const visible = (page: Page, text: string) => page.getByText(text, { exact: true }).locator("visible=true").first();

  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockApi(page, "GET", `/lessonseries/${SERIES}`, TEST_SERIES[0]);
    await mockApi(page, "GET", `/lessonseries/${SERIES}/enrollments`, [
      enrollment({}),
      enrollment({ id: "e0000000-0000-0000-0000-000000000002", studentName: "Bram Eenling", studentEmail: "bram@example.test", contactEmail: "bram@example.test", maxGroupSize: 1 }),
    ]);
  });

  test("toont een chip met het maximum, enkel als iemand met anderen wil zitten", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${SERIES}`);

    await expect(visible(page, "Anna Solo")).toBeVisible();
    await expect(page.getByText("max. 3").locator("visible=true")).toHaveCount(1);
    await expect(page.getByText("max. 1")).toHaveCount(0);
    // Geen ruwe vertaalsleutels in de interface.
    await expect(page.getByText(/enrollmentsTable\.|enrollmentDetail\./)).toHaveCount(0);
  });

  test("detail en bewerken tonen het maximum met de juiste teksten en sturen de keuze mee", async ({ page }) => {
    let put: Record<string, unknown> | undefined;
    await page.route(`${API_URL}/lessonseries/${SERIES}/enrollments/e0000000-0000-0000-0000-000000000001`, (route) => {
      if (route.request().method() !== "PUT") return route.continue();
      put = route.request().postDataJSON() as Record<string, unknown>;
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(enrollment({ maxGroupSize: 4 })) });
    });
    await page.goto(`/dashboard/lessons/${SERIES}`);

    await visible(page, "Anna Solo").click();
    const detail = page.getByRole("dialog");
    await expect(detail.getByText("Max. personen per les")).toBeVisible();
    await detail.getByRole("button", { name: "Aanpassen" }).click();

    const edit = page.getByRole("dialog", { name: "Inschrijving aanpassen" });
    await expect(edit.getByText("Maximum aantal personen per les", { exact: true }).first()).toBeVisible();
    const choice = edit.getByRole("radiogroup", { name: "Maximum aantal personen per les" });
    await expect(choice.getByRole("radio")).toHaveCount(4);
    await expect(choice.getByRole("radio", { name: /Trio/ })).toBeChecked();
    await expect(page.getByText(/enrollmentsTable\./)).toHaveCount(0);

    await choice.getByText("Groep van 4").click();
    await edit.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => put).toBeDefined();
    expect(put?.maxGroupSize).toBe(4);
  });

  test("manueel toevoegen laat de admin een maximum kiezen", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${SERIES}`);

    await page.getByRole("button", { name: "Manueel toevoegen" }).locator("visible=true").first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Maximum aantal personen per les", { exact: true }).first()).toBeVisible();
    await expect(dialog.getByRole("radiogroup", { name: "Maximum aantal personen per les" }).getByRole("radio", { name: /Solo/ })).toBeChecked();
    await expect(page.getByText(/enrollmentsTable\./)).toHaveCount(0);
  });
});
