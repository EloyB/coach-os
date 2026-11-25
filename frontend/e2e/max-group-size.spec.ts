import { test, expect, type Page } from "@playwright/test";
import type { LessonSeriesEnrollmentDto, SubmitEnrollmentRequest } from "@/lib/api/enrollments";
import type { PlanningOverviewDto } from "@/lib/api/planning";
import { API_URL, TEST_PUBLIC_SERIES, answerNotTogether, loginViaStorage, mockApi, TEST_SERIES } from "./helpers";

// Maximum aantal personen per tijdslot (#387): de deelnemer kiest het bij het inschrijven, en de admin krijgt een
// waarschuwing (met doorgaan) als hij iemand in een groter slot zet.

const SERIES_ID = "66666666-6666-6666-6666-666666666666";

test.describe("Inschrijven: maximum aantal personen per les", () => {
  async function arrange(page: Page, overrides: Record<string, unknown> = {}) {
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/timeslots`, (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: "[]" }));
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}`, { ...TEST_PUBLIC_SERIES, ...overrides });
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

  async function addPerson(page: Page, name = "Lotte Peeters") {
    await page.getByRole("button", { name: "Nog iemand inschrijven" }).click();
    await page.getByPlaceholder("Naam").last().fill(name);
    await page.getByLabel(/geboortedatum \d/i).last().fill("2015-03-04");
  }

  const together = (page: Page) => page.getByRole("radiogroup", { name: "Samen met andere spelers?" });
  const summary = (page: Page) => page.locator("p[aria-live='polite']");

  test("solo: de vraag staat zonder voorselectie, met liever niet en tot 2, 3 of 4 personen", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeader(page);

    // Zolang de vraag niet beantwoord is, zegt de zin enkel hoeveel personen je inschrijft.
    await expect(summary(page)).toHaveText("Je schrijft 1 persoon in (jezelf).");
    await expect(together(page).getByRole("radio")).toHaveCount(4);
    await expect(together(page).getByRole("radio", { checked: true })).toHaveCount(0);

    await together(page).getByText("Tot 3 personen").click();
    await expect(summary(page)).toHaveText("Je schrijft 1 persoon in (jezelf). In je les mogen er maximaal 3 zitten.");
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.maxGroupSize).toBe(3);
    expect(submitted.body?.enrollmentType).toBe("solo");
  });

  test("de vraag is verplicht: zonder antwoord wordt er niets verstuurd", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeader(page);

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    const error = page.getByRole("alert").filter({ hasText: "Kies of je liever niet samen met andere spelers" });
    await expect(error).toBeVisible();
    expect(submitted.body).toBeUndefined();
    // De pagina scrolt naar de vraag, zodat de fout in beeld staat.
    await expect(error).toBeInViewport();

    // Na een keuze verdwijnt de fout en kan het wel, ook met "Liever niet".
    await together(page).getByText("Liever niet").click();
    await expect(page.getByText("Kies of je liever niet samen met andere spelers")).toHaveCount(0);
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.maxGroupSize).toBe(1);
  });

  test("een extra persoon maakt er vanzelf een groepsinschrijving van en de keuze volgt de groepsgrootte", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeader(page);
    await addPerson(page);

    await expect(summary(page)).toHaveText("Je schrijft 2 personen in.");
    // Twee personen: "Liever niet" is de eigen groep, daarna tot 3 of 4.
    await expect(together(page).getByRole("radio")).toHaveCount(3);
    await expect(together(page).getByRole("radio", { name: "Tot 3 personen" })).toHaveCount(1);

    await together(page).getByText("Liever niet").click();
    await expect(summary(page)).toHaveText("Je schrijft 2 personen in. Jullie zitten alleen met jullie groep in de les.");
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.enrollmentType).toBe("group");
    expect(submitted.body?.maxGroupSize).toBe(2);
  });

  test("een beantwoorde vraag blijft beantwoord als er een persoon bijkomt of weggaat", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeader(page);
    await together(page).getByText("Liever niet").click();

    await addPerson(page);
    await expect(summary(page)).toHaveText("Je schrijft 2 personen in. Jullie zitten alleen met jullie groep in de les.");
    await expect(together(page).getByRole("radio", { name: "Liever niet" })).toBeChecked();

    await page.getByRole("button", { name: "Nog iemand inschrijven" }).click();
    await expect(summary(page)).toHaveText("Je schrijft 3 personen in. Jullie zitten alleen met jullie groep in de les.");

    await page.getByRole("button", { name: "Verwijderen" }).last().click();
    await expect(summary(page)).toHaveText("Je schrijft 2 personen in. Jullie zitten alleen met jullie groep in de les.");

    // Het antwoord telt nog: opnieuw kiezen is niet nodig.
    await page.getByPlaceholder("Naam").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum 1/i).fill("2015-03-04");
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();
    await expect.poll(() => submitted.body).toBeDefined();
  });

  test("een gekozen maximum blijft staan als er een persoon bijkomt", async ({ page }) => {
    await arrange(page);
    await fillLeader(page);
    await addPerson(page);
    await together(page).getByText("Tot 4 personen").click();

    await page.getByRole("button", { name: "Nog iemand inschrijven" }).click();

    await expect(summary(page)).toHaveText("Je schrijft 3 personen in. In jullie les mogen er maximaal 4 zitten.");
    await expect(together(page).getByRole("radio", { name: "Tot 4 personen" })).toBeChecked();
  });

  test("de laatste persoon verwijderen maakt er weer een solo-inschrijving van", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeader(page);
    await addPerson(page);

    await page.getByRole("button", { name: "Verwijderen" }).click();
    await expect(summary(page)).toHaveText("Je schrijft 1 persoon in (jezelf).");

    await answerNotTogether(page);
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();
    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.enrollmentType).toBe("solo");
    expect(submitted.body?.groupMembers).toBeUndefined();
  });

  test("er is geen apart type meer om te kiezen", async ({ page }) => {
    await arrange(page);
    await fillLeader(page);

    await expect(page.getByText("Wie schrijf je in?")).toBeVisible();
    await expect(page.getByText("Ik schrijf meerdere personen in")).toHaveCount(0);
    await expect(page.getByText("Inschrijving type")).toHaveCount(0);
  });

  test("een reeks die maar één persoon toelaat toont de vraag niet en stuurt maximum 1", async ({ page }) => {
    const submitted = await arrange(page, { maxGroupSize: 1 });
    await fillLeader(page);

    await expect(page.getByText("Samen met andere spelers?")).toHaveCount(0);
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.maxGroupSize).toBe(1);
  });

  test("een reeks met maximum 2 laat geen trio toe", async ({ page }) => {
    await arrange(page, { maxGroupSize: 2 });
    await fillLeader(page);

    await expect(together(page).getByRole("radio")).toHaveCount(2);
    await expect(together(page).getByRole("radio", { name: "Tot 3 personen" })).toHaveCount(0);
  });

  test("een reeks die enkel solo toelaat toont geen 'Nog iemand inschrijven'", async ({ page }) => {
    await arrange(page, { allowGroupEnrollment: false });
    await fillLeader(page);

    await expect(page.getByRole("button", { name: "Nog iemand inschrijven" })).toHaveCount(0);
    await expect(summary(page)).toBeVisible();
  });

  test("een reeks die enkel groepen toelaat begint met een extra persoon die je niet kunt verwijderen", async ({ page }) => {
    const submitted = await arrange(page, { allowSoloEnrollment: false });
    await fillLeader(page);

    await expect(page.getByText("Extra persoon")).toHaveCount(1);
    await expect(page.getByRole("button", { name: "Verwijderen" })).toHaveCount(0);
    await expect(summary(page)).toContainText("Je schrijft 2 personen in.");

    await page.getByPlaceholder("Naam").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum 1/i).fill("2015-03-04");
    await answerNotTogether(page);
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.enrollmentType).toBe("group");
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
