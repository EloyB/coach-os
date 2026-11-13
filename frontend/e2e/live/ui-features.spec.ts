import { test, expect, type Page } from "@playwright/test";
import type { LessonSeriePriceDto } from "@/lib/api/lessonSeriePrices";
import {
  API_URL,
  apiLogin,
  authHeaders,
  createTestSeries,
  loginUi,
  type LoginResult,
} from "./live-helpers";

/**
 * Live UI-tests: klikken door de echte interface tegen de echte backend.
 * Vult de API-tests aan — die bewijzen de logica, deze bewijzen dat de
 * beheerder er ook bij kan.
 */
test.describe.configure({ mode: "serial" });

/**
 * Opent het actiemenu (⋮) van een inschrijving en klikt een menu-item aan.
 * De naam staat twee keer in de DOM (kaart voor mobiel en tabel voor desktop);
 * getByRole negeert de verborgen variant, dus dit raakt enkel de zichtbare knop.
 */
async function openEnrollmentAction(
  page: Page,
  studentName: string,
  action: string,
): Promise<void> {
  await page
    .getByRole("button", { name: `Acties voor inschrijving van ${studentName}` })
    .click();
  await page.getByRole("button", { name: action, exact: true }).click();
}

test.describe("Live UI — lessenreeks detailpagina", () => {
  let auth: LoginResult;
  let seriesId: string;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    seriesId = (await createTestSeries(request, auth)).id;
  });

  test.beforeEach(async ({ page }) => {
    await loginUi(page, auth);
  });

  test("prijsopties-sectie is zichtbaar zonder opties", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${seriesId}`);

    await expect(page.getByRole("heading", { name: "Prijsopties" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Optie toevoegen" })).toBeVisible();
    await expect(page.getByText("Nog geen prijsopties.")).toBeVisible();
  });

  test("uitleg maakt duidelijk dat de prijs per deelnemer geldt", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${seriesId}`);

    await expect(page.getByText(/De prijs geldt per deelnemer/)).toBeVisible();
  });

  test("prijsoptie toevoegen en opslaan werkt", async ({ page, request }) => {
    await page.goto(`/dashboard/lessons/${seriesId}`);

    await page.getByRole("button", { name: "Optie toevoegen" }).click();
    const dialog = page.getByRole("dialog", { name: "Prijsoptie toevoegen" });
    await expect(dialog).toBeVisible();

    // De labels hebben geen htmlFor-koppeling, dus via input-locators binnen de dialog.
    await dialog.locator('input:not([type="number"])').fill("Jeugdtarief");
    await dialog.locator('input[type="number"]').fill("95");
    await dialog.locator("textarea").fill("Tot en met 17 jaar");
    await dialog.getByRole("button", { name: "Toevoegen", exact: true }).click();

    await expect(page.getByText("Prijsopties opgeslagen")).toBeVisible();
    await expect(dialog).toBeHidden();

    // Verifieer tegen de API dat de optie echt is opgeslagen.
    const read = await request.get(`${API_URL}/lessonseries/${seriesId}/prices`, {
      headers: authHeaders(auth),
    });
    const prices = (await read.json()) as LessonSeriePriceDto[];
    expect(prices).toHaveLength(1);
    expect(prices[0].label).toBe("Jeugdtarief");
    expect(prices[0].totalPrice).toBe(95);
    expect(prices[0].description).toBe("Tot en met 17 jaar");

    // Opruimen zodat de reeks weer op de standaardprijs staat.
    const clear = await request.put(`${API_URL}/lessonseries/${seriesId}/prices`, {
      headers: authHeaders(auth),
      data: { prices: [] },
    });
    expect(clear.ok()).toBeTruthy();
  });

  test("knop Weekslot toevoegen op de planning opent de dialog", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${seriesId}/planning`);

    await page.getByRole("button", { name: "Weekslot toevoegen" }).click();

    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: "Weekslot toevoegen" }),
    ).toBeVisible();
    await expect(dialog.getByText("Starttijd", { exact: true })).toBeVisible();
    await expect(dialog.getByText("Eindtijd", { exact: true })).toBeVisible();
  });

  test("dialog blokkeert eindtijd vóór starttijd", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${seriesId}/planning`);
    await page.getByRole("button", { name: "Weekslot toevoegen" }).click();

    const dialog = page.getByRole("dialog");
    await dialog.locator('input[type="time"]').first().fill("15:00");
    await dialog.locator('input[type="time"]').nth(1).fill("14:00");

    await expect(
      dialog.getByText("De eindtijd moet na de starttijd liggen."),
    ).toBeVisible();
    await expect(
      dialog.getByRole("button", { name: "Toevoegen", exact: true }),
    ).toBeDisabled();
  });

  test("inschrijving annuleren via de UI", async ({ page, request }) => {
    // Maak een eigen inschrijving aan zodat de test niet afhangt van seed-volgorde.
    const stamp = Date.now();
    const studentName = `E2E UI Annuleren ${stamp}`;
    const email = `e2e-ui-cancel-${stamp}@test.be`;
    const submit = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName,
          studentEmail: email,
          dateOfBirth: "1991-03-03",
          responses: [],
        },
      },
    );
    expect(submit.ok()).toBeTruthy();

    await page.goto(`/dashboard/lessons/${seriesId}`);

    await openEnrollmentAction(page, studentName, "Inschrijving annuleren");

    const confirm = page.getByRole("alertdialog");
    await expect(
      confirm.getByRole("heading", { name: "Inschrijving annuleren?" }),
    ).toBeVisible();
    await confirm.getByRole("button", { name: "Annuleren", exact: true }).click();

    await expect(page.getByText("Inschrijving geannuleerd")).toBeVisible();

    const list = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await list.json()) as {
      studentEmail: string;
      status: string;
    }[];
    expect(enrollments.find((e) => e.studentEmail === email)?.status).toBe(
      "Cancelled",
    );
  });

  test("categorie staat in de details van een inschrijving", async ({
    page,
    request,
  }) => {
    // Naam uniek per run: de suite draait tegen een persistente database, dus
    // een vaste naam levert bij de tweede run twee treffers op.
    const stamp = Date.now();
    const studentName = `E2E Categorie Jeugd ${stamp}`;
    const email = `e2e-ui-category-${stamp}@test.be`;
    const child = new Date();
    child.setFullYear(child.getFullYear() - 11);

    const submit = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName,
          studentEmail: email,
          dateOfBirth: child.toISOString().slice(0, 10),
          responses: [],
        },
      },
    );
    expect(submit.ok()).toBeTruthy();

    await page.goto(`/dashboard/lessons/${seriesId}`);

    await openEnrollmentAction(page, studentName, "Details bekijken");

    // De categorie wordt niet meer in de rij getoond, wel in de detaildialog.
    const details = page.getByRole("dialog");
    await expect(details.getByText(studentName).first()).toBeVisible();
    await expect(details.getByText("Categorie", { exact: true })).toBeVisible();
    await expect(details.getByText("Jeugd", { exact: true })).toBeVisible();
  });
});

test.describe("Live UI — publiek inschrijfformulier", () => {
  let auth: LoginResult;
  let seriesId: string;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    seriesId = (await createTestSeries(request, auth)).id;
  });

  test("geboortedatumveld is aanwezig met uitleg", async ({ page }) => {
    await page.goto(`/enroll/${seriesId}`);

    await expect(page.getByLabel("Geboortedatum")).toBeVisible();
    await expect(
      page.getByText("Bepaalt het tarief (volwassene of jeugd)"),
    ).toBeVisible();
  });

  test("lege geboortedatum blokkeert verzenden", async ({ page }) => {
    await page.goto(`/enroll/${seriesId}`);

    // Voornaam/achternaam/e-mail hebben geen htmlFor-koppeling op deze pagina,
    // dus net als in e2e/enrollment.spec.ts via input-locators.
    const textInputs = page.locator('input[type="text"]');
    await textInputs.nth(0).fill("E2E");
    await textInputs.nth(1).fill("Validatie");
    await page.locator('input[type="email"]').fill(`e2e-val-${Date.now()}@test.be`);

    await page.getByRole("button", { name: "Inschrijven" }).click();

    await expect(page.getByText("Geboortedatum is verplicht")).toBeVisible();
  });

  test("volledige inschrijving via het formulier slaagt", async ({
    page,
    request,
  }) => {
    const email = `e2e-form-${Date.now()}@test.be`;

    await page.goto(`/enroll/${seriesId}`);

    const textInputs = page.locator('input[type="text"]');
    await textInputs.nth(0).fill("E2E");
    await textInputs.nth(1).fill("Formulier");
    await page.locator('input[type="email"]').fill(email);
    await page.getByLabel("Geboortedatum").fill("1988-09-09");

    await page.getByRole("button", { name: "Inschrijven" }).click();

    // Wacht op de bevestiging: zonder dit bevragen we de API vóór de POST rond is.
    await expect(page.getByText("Ingeschreven!")).toBeVisible();

    const list = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await list.json()) as {
      studentEmail: string;
      categoryLabel: string | null;
    }[];
    expect(
      enrollments.find((e) => e.studentEmail === email)?.categoryLabel,
    ).toBe("Volwassenen");
  });
});
