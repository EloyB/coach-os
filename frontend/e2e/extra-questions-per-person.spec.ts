import { test, expect, type Page } from "@playwright/test";
import type { EnrollmentFormDto, SubmitEnrollmentRequest } from "@/lib/api/enrollments";
import type { CampEnrollmentFormDto, PublicCampDto, SubmitCampEnrollmentRequest } from "@/lib/api/camps";
import { API_URL, TEST_PUBLIC_SERIES, mockApi } from "./helpers";

// Extra vragen van het inschrijfformulier (#411): elke persoon van een groepsinschrijving
// beantwoordt ze, direct onder de eigen vaste vragen.

const SERIES_ID = "66666666-6666-6666-6666-666666666666";
const CAMP_ID = "77777777-7777-7777-7777-777777777777";
const FIELD_ID = "aaaaaaaa-0000-0000-0000-0000000000f1";

const LESSON_FORM: EnrollmentFormDto = {
  id: "bbbbbbbb-0000-0000-0000-000000000001",
  lessonSeriesId: SERIES_ID,
  fields: [{ id: FIELD_ID, label: "Ervaring", type: 1, isRequired: true, scope: 0, order: 0, options: null }],
};

const CAMP_FORM: CampEnrollmentFormDto = {
  id: "bbbbbbbb-0000-0000-0000-000000000002",
  campId: CAMP_ID,
  fields: [{ id: FIELD_ID, label: "Shirtmaat", type: 1, isRequired: true, order: 0, options: null }],
};

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

/** Het tekstveld van een extra vraag: de input direct onder het label, in de volgorde van de pagina (leider eerst). */
function answerInputs(page: Page, label: string) {
  return page.locator("div").filter({ has: page.locator("> label", { hasText: label }) }).locator("> input");
}

test.describe("Extra vragen per groepslid: lessenreeks", () => {
  async function arrange(page: Page) {
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/timeslots`, (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: "[]" }));
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}`, TEST_PUBLIC_SERIES);
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}/form`, LESSON_FORM);

    const submitted: { body?: SubmitEnrollmentRequest } = {};
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/enroll`, (route) => {
      if (route.request().method() !== "POST") return route.continue();
      submitted.body = route.request().postDataJSON() as SubmitEnrollmentRequest;
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify("enrollment-id") });
    });
    return submitted;
  }

  async function fillLeaderAndAddMember(page: Page) {
    await page.goto(`/enroll/${SERIES_ID}`);
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill("Els");
    await inputs.nth(1).fill("Peeters");
    await page.locator('input[type="email"]').fill("ouder@example.com");
    await page.locator('input[type="tel"]').fill("0470 12 34 56");
    await page.getByLabel(/geboortedatum/i).first().fill("1985-01-01");

    await page.getByText("Ik schrijf meerdere personen in").click();
    await page.getByRole("button", { name: "Lid toevoegen" }).click();
    await page.getByPlaceholder("Naam").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum 1/i).fill("2015-03-04");
  }

  test("elke persoon krijgt de vraag, onder de eigen vaste velden", async ({ page }) => {
    await arrange(page);
    await fillLeaderAndAddMember(page);

    // Eén vraag voor de leider en één voor het groepslid; geen losse sectie meer onderaan.
    await expect(answerInputs(page, "Ervaring")).toHaveCount(2);
    await expect(page.getByText("Extra vragen", { exact: true })).toHaveCount(0);

    // Leider: de vraag staat onder de geboortedatum en boven de keuze solo/groep.
    const leaderDob = await page.locator("#dateOfBirth").boundingBox();
    const leaderQuestion = await answerInputs(page, "Ervaring").nth(0).boundingBox();
    const enrollmentType = await page.getByText("Ik schrijf meerdere personen in").boundingBox();
    expect(leaderDob && leaderQuestion && enrollmentType).toBeTruthy();
    expect(leaderQuestion!.y).toBeGreaterThan(leaderDob!.y);
    expect(leaderQuestion!.y).toBeLessThan(enrollmentType!.y);

    // Groepslid: de vraag staat onder de geboortedatum van het lid.
    const memberDob = await page.getByLabel(/geboortedatum 1/i).boundingBox();
    const memberQuestion = await answerInputs(page, "Ervaring").nth(1).boundingBox();
    expect(memberQuestion!.y).toBeGreaterThan(memberDob!.y);
  });

  test("een verplichte vraag moet ook door het groepslid beantwoord worden", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeaderAndAddMember(page);
    await answerInputs(page, "Ervaring").nth(0).fill("Gevorderd");

    await page.getByRole("button", { name: "Inschrijven" }).click();

    // Enkel het groepslid mist een antwoord; de leider heeft geen fout.
    await expect(page.getByText("Ervaring is verplicht")).toHaveCount(1);
    expect(submitted.body).toBeUndefined();
  });

  test("verstuurt de antwoorden per persoon", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeaderAndAddMember(page);
    await answerInputs(page, "Ervaring").nth(0).fill("Gevorderd");
    await answerInputs(page, "Ervaring").nth(1).fill("Beginner");

    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.responses).toEqual([{ formFieldId: FIELD_ID, value: "Gevorderd" }]);
    expect(submitted.body?.groupMembers?.[0].responses).toEqual([{ formFieldId: FIELD_ID, value: "Beginner" }]);
  });
});

test.describe("Algemene vragen: één keer per inschrijving", () => {
  const GENERAL_ID = "aaaaaaaa-0000-0000-0000-0000000000f2";
  const FORM: EnrollmentFormDto = {
    id: "bbbbbbbb-0000-0000-0000-000000000003",
    lessonSeriesId: SERIES_ID,
    fields: [
      { id: FIELD_ID, label: "Ervaring", type: 1, isRequired: true, scope: 0, order: 0, options: null },
      { id: GENERAL_ID, label: "Hoe heb je ons gevonden?", type: 1, isRequired: true, scope: 1, order: 1, options: null },
    ],
  };

  async function arrange(page: Page) {
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/timeslots`, (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: "[]" }));
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}`, TEST_PUBLIC_SERIES);
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}/form`, FORM);
    const submitted: { body?: SubmitEnrollmentRequest } = {};
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/enroll`, (route) => {
      if (route.request().method() !== "POST") return route.continue();
      submitted.body = route.request().postDataJSON() as SubmitEnrollmentRequest;
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify("enrollment-id") });
    });
    return submitted;
  }

  async function fillLeaderAndAddMember(page: Page) {
    await page.goto(`/enroll/${SERIES_ID}`);
    const inputs = page.locator('input[type="text"]');
    await inputs.nth(0).fill("Els");
    await inputs.nth(1).fill("Peeters");
    await page.locator('input[type="email"]').fill("ouder@example.com");
    await page.locator('input[type="tel"]').fill("0470 12 34 56");
    await page.getByLabel(/geboortedatum/i).first().fill("1985-01-01");
    await page.getByText("Ik schrijf meerdere personen in").click();
    await page.getByRole("button", { name: "Lid toevoegen" }).click();
    await page.getByPlaceholder("Naam").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum 1/i).fill("2015-03-04");
  }

  test("de algemene vraag staat één keer, onderaan; de vraag per speler bij elke persoon", async ({ page }) => {
    await arrange(page);
    await fillLeaderAndAddMember(page);

    await expect(answerInputs(page, "Ervaring")).toHaveCount(2);
    await expect(answerInputs(page, "Hoe heb je ons gevonden?")).toHaveCount(1);
    await expect(page.getByRole("heading", { name: "Over deze inschrijving" })).toBeVisible();

    // De algemene vraag staat onder het groepslid en boven de verzendknop.
    const member = await answerInputs(page, "Ervaring").nth(1).boundingBox();
    const general = await answerInputs(page, "Hoe heb je ons gevonden?").boundingBox();
    const submit = await page.getByRole("button", { name: "Inschrijven", exact: true }).boundingBox();
    expect(general!.y).toBeGreaterThan(member!.y);
    expect(general!.y).toBeLessThan(submit!.y);
  });

  test("een verplichte algemene vraag blokkeert het versturen, ook als alles per speler ingevuld is", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeaderAndAddMember(page);
    await answerInputs(page, "Ervaring").nth(0).fill("Gevorderd");
    await answerInputs(page, "Ervaring").nth(1).fill("Beginner");

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect(page.getByText("Hoe heb je ons gevonden? is verplicht")).toBeVisible();
    // Het groepslid krijgt geen fout voor de algemene vraag.
    await expect(page.getByText("is verplicht")).toHaveCount(1);
    expect(submitted.body).toBeUndefined();
  });

  test("het antwoord op de algemene vraag gaat enkel mee met de leider", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeaderAndAddMember(page);
    await answerInputs(page, "Ervaring").nth(0).fill("Gevorderd");
    await answerInputs(page, "Ervaring").nth(1).fill("Beginner");
    await answerInputs(page, "Hoe heb je ons gevonden?").fill("Via de club");

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect.poll(() => submitted.body).toBeDefined();
    expect(submitted.body?.responses).toEqual([
      { formFieldId: FIELD_ID, value: "Gevorderd" },
      { formFieldId: GENERAL_ID, value: "Via de club" },
    ]);
    expect(submitted.body?.groupMembers?.[0].responses).toEqual([{ formFieldId: FIELD_ID, value: "Beginner" }]);
  });

  test("solo: beide vragen staan gewoon op de pagina", async ({ page }) => {
    await arrange(page);
    await page.goto(`/enroll/${SERIES_ID}`);

    await expect(answerInputs(page, "Ervaring")).toHaveCount(1);
    await expect(answerInputs(page, "Hoe heb je ons gevonden?")).toHaveCount(1);
  });
});

test.describe("Reeks of kamp zonder formulier (echte API: 200 met lege body)", () => {
  // De echte API geeft voor een reeks of kamp zonder formulier 200 met een lege body terug: axios levert dan ""
  // i.p.v. null. De pagina mag daar niet op crashen.
  test("lessenreeks: de pagina laadt en toont het formulier", async ({ page }) => {
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/timeslots`, (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: "[]" }));
    await mockApi(page, "GET", `/public/lessonseries/${SERIES_ID}`, TEST_PUBLIC_SERIES);
    await page.route(`${API_URL}/public/lessonseries/${SERIES_ID}/form`, (route) =>
      route.fulfill({ status: 200, body: "" }));

    await page.goto(`/enroll/${SERIES_ID}`);

    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.getByText("This page couldn")).toHaveCount(0);
  });

  test("kamp: de pagina laadt en toont het formulier", async ({ page }) => {
    await mockApi(page, "GET", `/public/camps/${CAMP_ID}`, PUBLIC_CAMP);
    await page.route(`${API_URL}/public/camps/${CAMP_ID}/form`, (route) =>
      route.fulfill({ status: 200, body: "" }));

    await page.goto(`/camp/${CAMP_ID}`);

    await expect(page.locator('input[type="email"]')).toBeVisible();
  });
});

test.describe("Extra vragen per groepslid: kamp", () => {
  async function arrange(page: Page) {
    await mockApi(page, "GET", `/public/camps/${CAMP_ID}`, PUBLIC_CAMP);
    await mockApi(page, "GET", `/public/camps/${CAMP_ID}/form`, CAMP_FORM);

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

  async function fillLeaderAndAddMember(page: Page) {
    await page.goto(`/camp/${CAMP_ID}`);
    const below = (label: string) =>
      page.locator("div").filter({ has: page.locator(`> label:text-is("${label} *")`) }).locator("> input").first();
    await below("Voornaam").fill("Lotte");
    await below("Achternaam").fill("Peeters");
    await below("E-mailadres").fill("lotte@example.be");
    await below("Telefoonnummer").fill("0471234567");
    await page.locator("#dateOfBirth").fill("1985-05-20");

    await page.getByText("Ik schrijf meerdere personen in").click();
    await page.getByRole("button", { name: "Lid toevoegen" }).click();
    await page.getByPlaceholder("Naam").fill("Tom Peeters");
    await page.getByPlaceholder("E-mailadres").fill("tom@example.be");
    await page.getByLabel("Geboortedatum 1").fill("2012-09-01");
  }

  test("elke persoon krijgt de vraag, onder de eigen vaste velden", async ({ page }) => {
    await arrange(page);
    await fillLeaderAndAddMember(page);

    await expect(answerInputs(page, "Shirtmaat")).toHaveCount(2);
    await expect(page.getByText("Extra vragen", { exact: true })).toHaveCount(0);

    const leaderDob = await page.locator("#dateOfBirth").boundingBox();
    const leaderQuestion = await answerInputs(page, "Shirtmaat").nth(0).boundingBox();
    const memberDob = await page.getByLabel("Geboortedatum 1").boundingBox();
    const memberQuestion = await answerInputs(page, "Shirtmaat").nth(1).boundingBox();
    expect(leaderQuestion!.y).toBeGreaterThan(leaderDob!.y);
    expect(memberQuestion!.y).toBeGreaterThan(memberDob!.y);
  });

  test("een verplichte vraag moet ook door het groepslid beantwoord worden en wordt per persoon verstuurd", async ({ page }) => {
    const submitted = await arrange(page);
    await fillLeaderAndAddMember(page);
    await answerInputs(page, "Shirtmaat").nth(0).fill("M");

    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();
    await expect(page.getByText("Dit veld is verplicht")).toHaveCount(1);
    expect(submitted.body).toBeUndefined();

    await answerInputs(page, "Shirtmaat").nth(1).fill("XL");
    await page.getByRole("button", { name: "Inschrijven", exact: true }).click();

    await expect(page).toHaveURL(/thank-you/);
    expect(submitted.body?.responses).toEqual([{ campFormFieldId: FIELD_ID, value: "M" }]);
    expect(submitted.body?.groupMembers?.[0].responses).toEqual([{ campFormFieldId: FIELD_ID, value: "XL" }]);
  });
});
