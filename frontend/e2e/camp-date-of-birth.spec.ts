import { test, expect, type Page } from "@playwright/test";
import type {
  CampDetailDto,
  CampEnrollmentDto,
  PublicCampDto,
  SubmitCampEnrollmentRequest,
  UpdateCampEnrollmentRequest,
} from "@/lib/api/camps";
import { API_URL, loginViaStorage, mockAllDashboardApis, mockApi } from "./helpers";

const CAMP_ID = "77777777-7777-7777-7777-777777777777";

const PUBLIC_CAMP: PublicCampDto = {
  id: CAMP_ID,
  name: "Zomerkamp",
  description: null,
  level: null,
  price: 0,
  startDate: "2099-07-01",
  endDate: "2099-07-05",
  registrationDeadline: "2099-06-20T00:00:00Z",
  tennisClubName: "TC De Aces",
  maxParticipants: null,
  participantCount: 0,
  days: [],
  prices: [],
};

const CAMP_DETAIL: CampDetailDto = {
  ...PUBLIC_CAMP,
  tennisClubId: "33333333-3333-3333-3333-333333333333",
  isActive: true,
};

function buildEnrollment(overrides: Partial<CampEnrollmentDto>): CampEnrollmentDto {
  return {
    id: "88888888-8888-8888-8888-888888888881",
    participantName: "Lotte Peeters",
    participantEmail: "lotte@example.be",
    participantPhone: "0471234567",
    dateOfBirth: "2015-03-14",
    status: "Confirmed",
    enrolledAt: "2099-01-10T10:00:00Z",
    groupName: null,
    enrollmentGroupId: null,
    isGroupLeader: false,
    paymentMethod: null,
    paymentStatus: null,
    paymentAmount: null,
    paidAt: null,
    molliePaymentId: null,
    formResponses: [],
    campPriceId: null,
    campPriceLabel: null,
    ...overrides,
  };
}

async function mockPublicCamp(page: Page) {
  await mockApi(page, "GET", `/public/camps/${CAMP_ID}`, PUBLIC_CAMP);
  await mockApi(page, "GET", `/public/camps/${CAMP_ID}/form`, null);
}

async function captureSubmittedEnrollment(page: Page) {
  const submitted: { body?: SubmitCampEnrollmentRequest } = {};
  await page.route(`${API_URL}/public/camps/${CAMP_ID}/enroll`, (route) => {
    submitted.body = route.request().postDataJSON() as SubmitCampEnrollmentRequest;
    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ campEnrollmentId: "99999999-9999-9999-9999-999999999999", requiresPayment: false }),
    });
  });
  return submitted;
}

function inputBelowLabel(page: Page, labelText: string) {
  return page.locator("div").filter({ has: page.locator(`> label:text-is("${labelText} *")`) }).locator("> input").first();
}

async function fillContactFields(page: Page) {
  await inputBelowLabel(page, "Voornaam").fill("Lotte");
  await inputBelowLabel(page, "Achternaam").fill("Peeters");
  await inputBelowLabel(page, "E-mailadres").fill("lotte@example.be");
  await inputBelowLabel(page, "Telefoonnummer").fill("0471234567");
}

test.describe("Kamp-inschrijving: geboortedatum (publiek)", () => {
  test.beforeEach(async ({ page }) => {
    await mockPublicCamp(page);
  });

  test("geboortedatum is verplicht en er wordt niets verstuurd zonder", async ({ page }) => {
    const submitted = await captureSubmittedEnrollment(page);
    await page.goto(`/camp/${CAMP_ID}`);
    await fillContactFields(page);

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect(page.getByText("Dit veld is verplicht")).toBeVisible();
    expect(submitted.body).toBeUndefined();
  });

  test("een geboortedatum in de toekomst wordt geweigerd", async ({ page }) => {
    await page.goto(`/camp/${CAMP_ID}`);
    await fillContactFields(page);
    await page.locator("#dateOfBirth").fill("2099-01-01");

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect(page.getByText("Geboortedatum kan niet in de toekomst liggen")).toBeVisible();
  });

  test("solo: geboortedatum zit in de verstuurde payload", async ({ page }) => {
    const submitted = await captureSubmittedEnrollment(page);
    await page.goto(`/camp/${CAMP_ID}`);
    await fillContactFields(page);
    await page.locator("#dateOfBirth").fill("2015-03-14");

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect(page).toHaveURL(/thank-you/);
    expect(submitted.body?.dateOfBirth).toBe("2015-03-14");
  });

  test("groep: elk groepslid heeft een verplichte geboortedatum", async ({ page }) => {
    const submitted = await captureSubmittedEnrollment(page);
    await page.goto(`/camp/${CAMP_ID}`);
    await fillContactFields(page);
    await page.locator("#dateOfBirth").fill("1985-05-20");
    await page.getByText("Ik schrijf meerdere personen in").click();
    await page.getByRole("button", { name: "Lid toevoegen" }).click();
    await page.getByPlaceholder("Naam").fill("Tom Peeters");
    await page.getByPlaceholder("E-mailadres").fill("tom@example.be");

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();
    await expect(page.getByText("Dit veld is verplicht")).toBeVisible();
    expect(submitted.body).toBeUndefined();

    await page.getByLabel("Geboortedatum 1").fill("2012-09-01");
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect(page).toHaveURL(/thank-you/);
    expect(submitted.body?.dateOfBirth).toBe("1985-05-20");
    expect(submitted.body?.groupMembers?.[0].dateOfBirth).toBe("2012-09-01");
  });
});

test.describe("Kamp-inschrijving: geboortedatum (dashboard)", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
    await mockAllDashboardApis(page);
    await mockApi(page, "GET", `/camps/${CAMP_ID}`, CAMP_DETAIL);
    await mockApi(page, "GET", `/public/camps/${CAMP_ID}/form`, null);
  });

  test("detail toont de geboortedatum als dd/MM/yyyy", async ({ page }) => {
    await mockApi(page, "GET", `/camps/${CAMP_ID}/enrollments`, [buildEnrollment({})]);
    await page.goto(`/dashboard/camps/${CAMP_ID}`);

    await page.getByText("Lotte Peeters").locator("visible=true").first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("14/03/2015")).toBeVisible();
  });

  test("oude inschrijving zonder geboortedatum blijft bewerkbaar zonder geboortedatum", async ({ page }) => {
    await mockApi(page, "GET", `/camps/${CAMP_ID}/enrollments`, [buildEnrollment({ dateOfBirth: null })]);
    const updates: UpdateCampEnrollmentRequest[] = [];
    await page.route(`${API_URL}/camps/${CAMP_ID}/enrollments/*`, (route) => {
      if (route.request().method() !== "PUT") return route.continue();
      updates.push(route.request().postDataJSON() as UpdateCampEnrollmentRequest);
      return route.fulfill({ status: 204 });
    });
    await page.goto(`/dashboard/camps/${CAMP_ID}`);

    await page.getByRole("button", { name: /Acties/ }).first().click();
    await page.getByRole("button", { name: "Inschrijving aanpassen" }).click();
    const dialog = page.getByRole("dialog");
    const dateOfBirthInput = dialog.getByLabel("Geboortedatum");
    await expect(dateOfBirthInput).toHaveValue("");
    await dialog.getByRole("button", { name: "Opslaan" }).click();

    await expect.poll(() => updates.length).toBe(1);
    expect(updates[0].dateOfBirth ?? null).toBeNull();
  });
});
