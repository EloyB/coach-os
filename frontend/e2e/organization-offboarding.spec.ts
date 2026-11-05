import { test, expect, type Page } from "@playwright/test";
const org = "11111111-1111-4111-8111-111111111111";
async function login(page: Page, superadmin = false, role = "Admin") {
  await page
    .context()
    .addCookies([
      { name: "has_token", value: "1", domain: "localhost", path: "/" },
      { name: "user_role", value: role, domain: "localhost", path: "/" },
      ...(superadmin
        ? [
            {
              name: "has_super_admin",
              value: "1",
              domain: "localhost",
              path: "/",
            },
          ]
        : []),
    ]);
  await page.addInitScript(
    ({ superadmin, role }) => {
      localStorage.setItem("token", "synthetic-browser-token");
      localStorage.setItem(
        "auth_user",
        JSON.stringify({
          role,
          email: "admin@example.test",
          firstName: "Synthetic",
          lastName: "Admin",
          organizationId: "synthetic",
          emailVerified: true,
          isSuperAdmin: superadmin,
        }),
      );
    },
    { superadmin, role },
  );
  await page.route("**/api/**", (r) => r.fulfill({ json: {} }));
}
test("clubadmin downloads an authenticated ZIP; server errors never show success", async ({
  page,
}) => {
  await login(page);
  await page.route("**/privacy/organization/export", (r) =>
    r.fulfill({
      contentType: "application/zip",
      body: Buffer.from(
        "mocked UI download only; real archive verified by HTTP/PostgreSQL integration",
      ),
    }),
  );
  await page.goto("/dashboard/offboarding");
  const download = page.waitForEvent("download");
  await page.getByRole("button", { name: "Clubexport downloaden" }).click();
  expect((await download).suggestedFilename()).toBe("coachos-club-export.zip");
  await expect(
    page.getByRole("status").filter({ hasText: "Download gestart" }),
  ).toBeVisible();
  await page.route("**/privacy/organization/export", (r) =>
    r.fulfill({ status: 403, json: {} }),
  );
  await page.getByRole("button", { name: "Clubexport downloaden" }).click();
  await expect(
    page.getByRole("alert").filter({ hasText: "Export mislukt" }),
  ).toBeVisible();
});
test("trainer has no clubexport affordance", async ({ page }) => {
  await login(page, false, "Trainer");
  await page.goto("/dashboard/offboarding");
  await expect(
    page.getByRole("button", { name: "Clubexport downloaden" }),
  ).not.toBeVisible();
});
test("superadmin verified request, preview, exact-name confirmation; financial holds remain blocking", async ({
  page,
}) => {
  await login(page, true);
  const requests: Record<string, unknown>[] = [];
  let blocked = true;
  await page.route(`**/api/super-admin/organizations/${org}/erasure`, (r) => {
    const body = r.request().postDataJSON();
    requests.push(body);
    return r.fulfill({
      json: {
        eligible: !blocked,
        deleted: body.apply === true && !blocked,
        previewHash: "b".repeat(64),
        holds: blocked ? ["financial_or_legal_evidence_review_required"] : [],
        enrollmentCount: 2,
        campEnrollmentCount: 1,
        membershipCount: 3,
      },
    });
  });
  await page.goto(`/super-admin/organizations/${org}/erasure`);
  await page
    .getByLabel("ID van de bevoegde aanvragende clubbeheerder")
    .fill(org);
  await page
    .getByLabel("Ik heb identiteit en bevoegd verzoek gecontroleerd")
    .check();
  await page
    .getByRole("button", { name: "Organisatieverwijdering beoordelen" })
    .click();
  await expect(
    page.getByText("Financiële of juridische beoordeling vereist", {
      exact: true,
    }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Organisatie definitief verwijderen" }),
  ).not.toBeVisible();
  blocked = false;
  await page
    .getByRole("button", { name: "Organisatieverwijdering beoordelen" })
    .click();
  await expect(
    page.getByText(
      "2 lesinschrijvingen · 1 kampinschrijvingen · 3 lidmaatschappen",
    ),
  ).toBeVisible();
  await page.getByLabel("Exacte organisatienaam").fill("Synthetic club");
  await page.screenshot({
    path: "test-results/mocked-organization-review.png",
    fullPage: true,
  });
  await page
    .getByRole("button", { name: "Organisatie definitief verwijderen" })
    .click();
  await expect(
    page
      .getByRole("status")
      .filter({ hasText: "Organisatieverwijdering voltooid" }),
  ).toBeVisible();
  expect(requests.at(-1)).toMatchObject({
    apply: true,
    early: true,
    verifiedRequest: true,
    requestingAdminId: org,
    confirmation: "Synthetic club",
    previewHash: "b".repeat(64),
  });
});
