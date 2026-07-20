import { test, expect } from "@playwright/test";
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

  test("prijsmatrix is zichtbaar met beide categorieën", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${seriesId}`);

    await expect(page.getByRole("heading", { name: "Prijzen" })).toBeVisible();
    await expect(
      page.getByRole("columnheader", { name: "Volwassenen" }),
    ).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "Jeugd" })).toBeVisible();
    await expect(page.getByText("Privé (1)")).toBeVisible();
    await expect(page.getByText("4 personen")).toBeVisible();
  });

  test("uitleg maakt duidelijk dat het een groepstotaal is", async ({ page }) => {
    // Cruciaal detail: het legacy prijsveld is per persoon, de matrix is per groep.
    await page.goto(`/dashboard/lessons/${seriesId}`);

    await expect(page.getByText(/totaal voor de hele groep/i)).toBeVisible();
  });

  test("prijs invullen en opslaan werkt", async ({ page, request }) => {
    await page.goto(`/dashboard/lessons/${seriesId}`);

    const cell = page.getByLabel("Volwassenen, groep van 4");
    await expect(cell).toBeVisible();
    await cell.fill("485");

    await page.getByRole("button", { name: "Opslaan", exact: true }).click();
    await expect(page.getByText("Prijstabel opgeslagen")).toBeVisible();

    // Verifieer tegen de API dat het bedrag echt is opgeslagen.
    const read = await request.get(`${API_URL}/lessonseries/${seriesId}/prices`, {
      headers: authHeaders(auth),
    });
    const prices = (await read.json()) as {
      category: number;
      groupSize: number;
      totalPrice: number;
    }[];
    expect(
      prices.find((p) => p.category === 1 && p.groupSize === 4)?.totalPrice,
    ).toBe(485);

    // Opruimen zodat de reeks weer op legacy prijs staat.
    await request.put(`${API_URL}/lessonseries/${seriesId}/prices`, {
      headers: authHeaders(auth),
      data: { prices: [] },
    });
  });

  test("knop Lesmoment toevoegen opent de dialog", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${seriesId}`);

    await page.getByRole("button", { name: /Lesmoment toevoegen/ }).click();

    await expect(
      page.getByRole("heading", { name: "Lesmoment toevoegen" }),
    ).toBeVisible();
    await expect(page.getByText("Starttijd", { exact: true })).toBeVisible();
    await expect(page.getByText("Eindtijd", { exact: true })).toBeVisible();
  });

  test("dialog blokkeert eindtijd vóór starttijd", async ({ page }) => {
    await page.goto(`/dashboard/lessons/${seriesId}`);
    await page.getByRole("button", { name: /Lesmoment toevoegen/ }).click();

    await page.locator('input[type="time"]').first().fill("15:00");
    await page.locator('input[type="time"]').nth(1).fill("14:00");

    await expect(
      page.getByText("De eindtijd moet na de starttijd liggen."),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Toevoegen" })).toBeDisabled();
  });

  test("inschrijving annuleren via de UI", async ({ page, request }) => {
    // Maak een eigen inschrijving aan zodat de test niet afhangt van seed-volgorde.
    const email = `e2e-ui-cancel-${Date.now()}@test.be`;
    const submit = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName: "E2E UI Annuleren",
          studentEmail: email,
          dateOfBirth: "1991-03-03",
          responses: [],
        },
      },
    );
    expect(submit.ok()).toBeTruthy();

    await page.goto(`/dashboard/lessons/${seriesId}`);

    const row = page.locator("div").filter({ hasText: email }).last();
    await expect(row).toBeVisible();

    await page
      .getByRole("button", { name: "Inschrijving van E2E UI Annuleren annuleren" })
      .click();

    await expect(
      page.getByRole("heading", { name: "Inschrijving annuleren?" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Annuleren", exact: true }).last().click();

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

  test("categorie wordt als badge getoond bij inschrijvingen", async ({
    page,
    request,
  }) => {
    // Naam uniek per run: de suite draait tegen een persistente database, dus
    // een vaste naam levert bij de tweede run twee treffers op.
    const stamp = Date.now();
    const studentName = `E2E Badge Jeugd ${stamp}`;
    const email = `e2e-ui-badge-${stamp}@test.be`;
    const child = new Date();
    child.setFullYear(child.getFullYear() - 11);

    await request.post(`${API_URL}/public/lessonseries/${seriesId}/enroll`, {
      data: {
        studentName,
        studentEmail: email,
        dateOfBirth: child.toISOString().slice(0, 10),
        responses: [],
      },
    });

    await page.goto(`/dashboard/lessons/${seriesId}`);

    const row = page.getByText(studentName);
    await expect(row).toBeVisible();

    // De badge staat in dezelfde rij als de naam, niet zomaar ergens op de pagina.
    await expect(
      page.locator("div").filter({ hasText: studentName }).getByText("Jeugd").last(),
    ).toBeVisible();
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
