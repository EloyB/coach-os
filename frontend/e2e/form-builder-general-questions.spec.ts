import { test, expect, type Page } from "@playwright/test";
import type { EnrollmentFormDto, SaveFormFieldRequest } from "@/lib/api/enrollments";
import { loginViaStorage, TEST_SERIES } from "./helpers";

// Formulierbouwer van een lessenreeks: vragen per speler en algemene vragen (één keer per inschrijving).

const SERIES_ID = "66666666-6666-6666-6666-666666666666";

const EXISTING_FORM: EnrollmentFormDto = {
  id: "bbbbbbbb-0000-0000-0000-000000000009",
  lessonSeriesId: SERIES_ID,
  fields: [
    { id: "aaaaaaaa-0000-0000-0000-0000000000a1", label: "Ervaringsniveau", type: 1, isRequired: true, scope: 0, order: 0, options: null },
    { id: "aaaaaaaa-0000-0000-0000-0000000000a2", label: "Hoe heb je ons gevonden?", type: 1, isRequired: false, scope: 1, order: 1, options: null },
  ],
};

async function arrange(page: Page, form: EnrollmentFormDto | null) {
  const saved: { fields?: SaveFormFieldRequest[] } = {};
  await page.route("**/api/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    let data: unknown = [];
    if (path === `/api/lessonseries/${SERIES_ID}`) data = TEST_SERIES[0];
    if (path === `/api/public/lessonseries/${SERIES_ID}/form`) {
      if (form === null) return route.fulfill({ status: 200, body: "" });
      data = form;
    }
    if (path === `/api/lessonseries/${SERIES_ID}/form` && route.request().method() === "PUT") {
      saved.fields = route.request().postDataJSON().fields;
      data = SERIES_ID;
    }
    return route.fulfill({ json: data });
  });
  await loginViaStorage(page);
  await page.goto(`/dashboard/lessons/${SERIES_ID}`);
  const builder = page.getByRole("heading", { name: "Inschrijfformulier", exact: true })
    .locator('xpath=ancestor::div[.//button[contains(., "Algemene vraag toevoegen")]][1]');
  await expect(builder).toBeVisible();
  return { builder, saved };
}

test("bestaande vragen staan in de juiste lijst", async ({ page }) => {
  const { builder } = await arrange(page, EXISTING_FORM);

  const perPlayer = builder.locator("div.space-y-2").filter({ has: page.getByRole("heading", { name: "Vragen per speler" }) });
  const general = builder.locator("div.space-y-2").filter({ has: page.getByRole("heading", { name: "Algemene vragen" }) });
  await expect(perPlayer.locator('input[value="Ervaringsniveau"]')).toHaveCount(1);
  await expect(perPlayer.locator('input[value="Hoe heb je ons gevonden?"]')).toHaveCount(0);
  await expect(general.locator('input[value="Hoe heb je ons gevonden?"]')).toHaveCount(1);
});

test("een nieuwe vraag krijgt het bereik van de lijst waarin ze wordt toegevoegd", async ({ page }) => {
  const { builder, saved } = await arrange(page, null);

  await builder.getByRole("button", { name: "Vraag per speler toevoegen" }).click();
  await builder.getByPlaceholder(/Veldlabel/).first().fill("T-shirtmaat");
  await builder.getByRole("button", { name: "Algemene vraag toevoegen" }).click();
  await builder.getByPlaceholder(/Veldlabel/).last().fill("Opmerking voor de trainer");
  await builder.getByRole("button", { name: "Formulier opslaan" }).click();

  await expect.poll(() => saved.fields).toEqual([
    { label: "T-shirtmaat", type: 1, isRequired: false, scope: 0, order: 0 },
    { label: "Opmerking voor de trainer", type: 1, isRequired: false, scope: 1, order: 1 },
  ]);
});

test("een vraag verplaatsen naar de andere lijst wijzigt haar bereik, met een melding bij bestaande antwoorden", async ({ page }) => {
  const { builder, saved } = await arrange(page, EXISTING_FORM);

  await builder.getByRole("button", { name: "Maak algemeen" }).click();
  await expect(builder.getByText("Bestaande antwoorden blijven bewaard")).toBeVisible();
  await builder.getByRole("button", { name: "Formulier opslaan" }).click();

  await expect.poll(() => saved.fields?.map((f) => [f.label, f.scope])).toEqual([
    ["Ervaringsniveau", 1],
    ["Hoe heb je ons gevonden?", 1],
  ]);
});

test("omhoog en omlaag werken binnen de eigen lijst", async ({ page }) => {
  const form: EnrollmentFormDto = {
    ...EXISTING_FORM,
    fields: [
      { id: "aaaaaaaa-0000-0000-0000-0000000000b1", label: "A per speler", type: 1, isRequired: false, scope: 0, order: 0, options: null },
      { id: "aaaaaaaa-0000-0000-0000-0000000000b2", label: "B algemeen", type: 1, isRequired: false, scope: 1, order: 1, options: null },
      { id: "aaaaaaaa-0000-0000-0000-0000000000b3", label: "C per speler", type: 1, isRequired: false, scope: 0, order: 2, options: null },
    ],
  };
  const { builder, saved } = await arrange(page, form);

  // "A" omlaag: wisselt met "C" (de volgende per-speler-vraag), niet met de algemene vraag ertussen.
  const rowA = builder.locator('input[value="A per speler"]').locator("xpath=ancestor::div[contains(@class, 'rounded-xl')][1]");
  await rowA.locator("button").nth(2).click();
  await builder.getByRole("button", { name: "Formulier opslaan" }).click();

  await expect.poll(() => saved.fields?.filter((f) => f.scope === 0).map((f) => f.label)).toEqual(["C per speler", "A per speler"]);
});
