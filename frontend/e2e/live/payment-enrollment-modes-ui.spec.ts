import { test, expect } from "@playwright/test";
import {
  API_URL,
  apiLogin,
  authHeaders,
  loginUi,
  type LoginResult,
} from "./live-helpers";

/**
 * Live UI-tests voor de betaalmethodes- en inschrijfwijze-keuze:
 * - de wizard (stap 1) toont de checkboxes, met "online betalen" disabled
 *   zolang de organisatie geen Mollie-koppeling heeft (TC De Aces heeft die niet);
 * - het publieke inschrijfformulier toont enkel de toegelaten inschrijfwijze(s)
 *   voor de vaste solo-only/group-only seed-reeksen.
 */
test.describe.configure({ mode: "serial" });

test.describe("Live UI — wizard stap 1: inschrijfwijze en betaalmethodes", () => {
  let auth: LoginResult;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
  });

  test.beforeEach(async ({ page }) => {
    await loginUi(page, auth);
  });

  test("Inschrijfwijze- en Betaalmethodes-checkboxes zijn zichtbaar, online betalen is disabled zonder Mollie", async ({
    page,
  }) => {
    await page.goto("/dashboard/lessons/new");

    // Inschrijfwijze
    await expect(
      page.getByRole("group", { name: "Inschrijfwijze" }),
    ).toBeVisible();
    const soloCheckbox = page.getByRole("checkbox", {
      name: "Solo inschrijven",
    });
    const groupCheckbox = page.getByRole("checkbox", {
      name: "In groep inschrijven",
    });
    await expect(soloCheckbox).toBeVisible();
    await expect(groupCheckbox).toBeVisible();
    await expect(soloCheckbox).toBeChecked();
    await expect(groupCheckbox).toBeChecked();

    // Betaalmethodes
    await expect(
      page.getByRole("group", { name: "Betaalmethodes" }),
    ).toBeVisible();
    const onlineCheckbox = page.getByRole("checkbox", {
      name: "Online betalen (Mollie)",
    });
    const manualCheckbox = page.getByRole("checkbox", {
      name: "Overschrijving",
    });
    await expect(onlineCheckbox).toBeVisible();
    await expect(manualCheckbox).toBeVisible();

    // TC De Aces heeft geen Mollie-koppeling: online betalen moet disabled zijn
    // en de hint met doorverwijzing naar instellingen moet zichtbaar zijn.
    await expect(onlineCheckbox).toBeDisabled();
    await expect(onlineCheckbox).not.toBeChecked();
    await expect(manualCheckbox).toBeChecked();
    await expect(
      page.getByText(
        "Online betalen kan pas nadat je met Mollie verbonden bent.",
      ),
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: "Verbind Mollie in instellingen" }),
    ).toBeVisible();
  });
});

test.describe("Live UI — publiek inschrijfformulier toont enkel toegelaten inschrijfwijze", () => {
  let soloOnlySeriesId: string;
  let groupOnlySeriesId: string;

  test.beforeAll(async ({ request }) => {
    const auth = await apiLogin(request);
    const list = await request.get(`${API_URL}/lessonseries`, {
      headers: authHeaders(auth),
    });
    const series = (await list.json()) as { id: string; name: string }[];

    const soloOnly = series.find((s) => s.name === "Voorjaarslessen Beginners");
    const groupOnly = series.find(
      (s) => s.name === "Competitietraining Gevorderd",
    );
    expect(
      soloOnly,
      "Seed-reeks 'Voorjaarslessen Beginners' (solo-only) niet gevonden",
    ).toBeDefined();
    expect(
      groupOnly,
      "Seed-reeks 'Competitietraining Gevorderd' (group-only) niet gevonden",
    ).toBeDefined();

    soloOnlySeriesId = soloOnly!.id;
    groupOnlySeriesId = groupOnly!.id;
  });

  test("solo-only reeks toont geen groepskeuze op de enroll-pagina", async ({
    page,
  }) => {
    await page.goto(`/enroll/${soloOnlySeriesId}`);

    // Formulier moet effectief geladen zijn.
    await expect(page.getByLabel("Geboortedatum")).toBeVisible();

    // Bij een enkele toegelaten wijze toont de pagina de keuzekaarten
    // helemaal niet (enrollmentType ligt al vast) — dus geen "Groep"-optie.
    await expect(page.getByText("Inschrijving type")).toHaveCount(0);
    await expect(
      page.getByText("Ik schrijf meerdere personen in"),
    ).toHaveCount(0);
  });

  test("group-only reeks toont geen solokeuze op de enroll-pagina", async ({
    page,
  }) => {
    await page.goto(`/enroll/${groupOnlySeriesId}`);

    await expect(page.getByLabel("Geboortedatum")).toBeVisible();

    await expect(page.getByText("Inschrijving type")).toHaveCount(0);
    await expect(page.getByText("Ik schrijf mezelf in")).toHaveCount(0);
  });
});
