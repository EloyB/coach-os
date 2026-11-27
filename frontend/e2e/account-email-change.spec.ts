import { test, expect, type Page } from "@playwright/test";
import { TEST_USER, mockAllDashboardApis } from "./helpers";

const newEmail = "nieuw+coach@example.be";
const evidenceDir = process.env.ACCOUNT_EMAIL_EVIDENCE_DIR;

async function staffSession(page: Page, role = "Trainer") {
  await mockAllDashboardApis(page);
  await page.route("**/api/inbox**", route => route.fulfill({ json: { items: [], unreadCount: 0 } }));
  await page.route("**/api/subscription", route => route.fulfill({ json: { status: "Active", accessMode: "Full" } }));
  await page.context().addCookies([
    { name: "has_token", value: "1", domain: "localhost", path: "/" },
    { name: "user_role", value: role, domain: "localhost", path: "/" },
  ]);
  await page.addInitScript(({ user, role }) => {
    if (location.pathname === "/login") return;
    if (!localStorage.getItem("token")) {
      localStorage.setItem("token", user.token);
      localStorage.setItem("auth_user", JSON.stringify({ ...user, role, emailVerified: true }));
    }
    document.cookie = "has_token=1; path=/; SameSite=Lax";
    document.cookie = `user_role=${role}; path=/; SameSite=Lax`;
  }, { user: TEST_USER, role });
}

test("trainer can request an account-wide email change without changing the current address", async ({ page }) => {
  await staffSession(page);
  let body: unknown;
  await page.route("**/api/auth/email-change/request", async route => {
    body = route.request().postDataJSON();
    await route.fulfill({ status: 204 });
  });
  await page.goto("/dashboard");
  await page.getByRole("link", { name: "Mijn account", exact: true }).filter({ visible: true }).click();
  await expect(page.getByRole("heading", { name: "Mijn account", exact: true })).toBeVisible();
  await expect(page.getByText("Dit e-mailadres geldt voor je account bij alle clubs.")).toBeVisible();
  await page.getByLabel("Nieuw e-mailadres", { exact: true }).fill(newEmail);
  await page.getByLabel("Huidig wachtwoord", { exact: true }).fill("existing-password");
  await page.getByRole("button", { name: "Bevestigingsmail versturen", exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: "Controleer je nieuwe e-mailadres" })).toBeVisible();
  expect(body).toEqual({ newEmail, currentPassword: "existing-password" });
  expect(await page.evaluate(() => JSON.parse(localStorage.getItem("auth_user")!).email)).toBe(TEST_USER.email);
  await expect(page.getByText("Je huidige e-mailadres blijft actief totdat je de wijziging bevestigt.")).toBeVisible();
  await expect(page.getByLabel("Huidig wachtwoord", { exact: true })).toHaveValue("");
});

async function fillRequest(page: Page) {
  await page.getByLabel("Nieuw e-mailadres", { exact: true }).fill(newEmail);
  await page.getByLabel("Huidig wachtwoord", { exact: true }).fill("existing-password");
}

for (const status of [400, 409, 429, 500]) {
  test(`request failure ${status} preserves the current account and allows retry`, async ({ page }) => {
    await staffSession(page);
    let calls = 0;
    await page.route("**/api/auth/email-change/request", route => {
      calls++;
      return route.fulfill({ status: calls === 1 ? status : 204, json: calls === 1 ? { title: "Rejected" } : undefined });
    });
    await page.goto("/dashboard/account");
    await fillRequest(page);
    await page.getByRole("button", { name: "Bevestigingsmail versturen", exact: true }).click();
    await expect(page.getByRole("main").getByRole("alert")).toContainText(status === 429 ? "Te veel pogingen" : "De aanvraag is niet gelukt");
    expect(await page.evaluate(() => JSON.parse(localStorage.getItem("auth_user")!).email)).toBe(TEST_USER.email);
    await page.getByRole("button", { name: "Bevestigingsmail versturen", exact: true }).click();
    await expect(page.getByRole("status").filter({ hasText: "Controleer je nieuwe e-mailadres" })).toBeVisible();
    expect(calls).toBe(2);
  });
}

test("invalid request fields are localized and never sent", async ({ page }) => {
  await staffSession(page);
  let calls = 0;
  await page.route("**/api/auth/email-change/request", route => { calls++; return route.fulfill({ status: 204 }); });
  await page.goto("/dashboard/account");
  await page.getByLabel("Nieuw e-mailadres", { exact: true }).fill("invalid");
  await page.getByRole("button", { name: "Bevestigingsmail versturen", exact: true }).click();
  await expect(page.getByText("Vul een geldig e-mailadres in.")).toBeVisible();
  await expect(page.getByText("Vul je huidige wachtwoord in.")).toBeVisible();
  expect(calls).toBe(0);
});

for (const status of [400, 403, 409, 429, 500]) {
  test(`confirmation failure ${status} leaves session untouched and offers a new request`, async ({ page }) => {
    await staffSession(page);
    let authorization: string | undefined;
    await page.route("**/api/auth/email-change/confirm", route => {
      authorization = route.request().headers().authorization;
      return route.fulfill({ status, json: { title: "Rejected" } });
    });
    await page.goto(confirmationLink());
    await page.getByRole("button", { name: "E-mailadres bevestigen", exact: true }).click();
    await expect(page.getByRole("main").getByRole("alert")).toContainText(status === 429 ? "Te veel pogingen" : "De link is mogelijk verlopen");
    expect(authorization).toBe(`Bearer ${TEST_USER.token}`);
    expect(await page.evaluate(() => localStorage.getItem("token"))).toBe(TEST_USER.token);
    expect(await page.evaluate(() => JSON.parse(localStorage.getItem("auth_user")!).email)).toBe(TEST_USER.email);
    await page.getByRole("button", { name: "Annuleren", exact: true }).click();
    await expect(page.getByLabel("Nieuw e-mailadres", { exact: true })).toBeVisible();
    await expect(page).toHaveURL(/\/dashboard\/account$/);
  });
}

for (const action of ["request", "confirm"]) {
  test(`expired session during ${action} clears authentication without leaking challenge`, async ({ page }) => {
    await staffSession(page);
    await page.route(`**/api/auth/email-change/${action}`, route => route.fulfill({ status: 401 }));
    await page.goto(action === "confirm" ? confirmationLink() : "/dashboard/account");
    if (action === "request") await fillRequest(page);
    await page.getByRole("button", { name: action === "confirm" ? "E-mailadres bevestigen" : "Bevestigingsmail versturen", exact: true }).click();
    await expect(page).toHaveURL(/\/login\?redirect=%2Fdashboard%2Faccount$/);
    expect(await page.evaluate(() => localStorage.getItem("token"))).toBeNull();
    expect(page.url()).not.toContain("sensitive");
  });
}

test("malformed confirmation is scrubbed without an API call", async ({ page }) => {
  await staffSession(page);
  let calls = 0;
  await page.route("**/api/auth/email-change/confirm", route => { calls++; return route.fulfill({ status: 400 }); });
  await page.goto("/dashboard/account#email=not-an-email&token=secret");
  await expect(page.getByRole("main").getByRole("alert")).toContainText("De link is mogelijk verlopen");
  await expect(page).toHaveURL(/\/dashboard\/account$/);
  await expect(page.getByLabel("Nieuw e-mailadres", { exact: true })).toBeVisible();
  expect(calls).toBe(0);
});

test("admin desktop account page matches the dashboard shell", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await staffSession(page, "Admin");
  await page.goto("/dashboard/account");
  await expect(page.getByText(TEST_USER.email, { exact: true })).toBeVisible();
  await expect(page.getByRole("main")).toHaveCount(1);
  await expect(page.getByRole("link", { name: "Mijn account", exact: true }).filter({ visible: true })).toBeVisible();
  if (evidenceDir) await page.screenshot({ path: `${evidenceDir}/account-email-desktop.png`, fullPage: true });
});

test("mobile account link, request and confirmation fit without horizontal overflow", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await staffSession(page);
  await page.route("**/api/auth/email-change/request", route => route.fulfill({ status: 204 }));
  await page.goto("/dashboard");
  await page.getByRole("link", { name: "Mijn account", exact: true }).filter({ visible: true }).click();
  await expect(page.getByLabel("Nieuw e-mailadres", { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  if (evidenceDir) await page.screenshot({ path: `${evidenceDir}/account-email-mobile.png`, fullPage: true });
  await fillRequest(page);
  await page.getByRole("button", { name: "Bevestigingsmail versturen", exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: "Controleer je nieuwe e-mailadres" })).toBeVisible();
  await page.goto(confirmationLink());
  await expect(page.getByRole("button", { name: "E-mailadres bevestigen", exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  if (evidenceDir) await page.screenshot({ path: `${evidenceDir}/account-email-confirmation.png`, fullPage: true });
});

function confirmationLink(token = "sensitive+/=token") {
  return `/dashboard/account#email=${encodeURIComponent(newEmail)}&token=${encodeURIComponent(token)}`;
}

test("email link requires explicit confirmation, cleans its fragment and refreshes the complete session", async ({ page }) => {
  await staffSession(page);
  let calls = 0;
  let body: unknown;
  const response = { ...TEST_USER, email: newEmail, token: "new-session-token", role: "Trainer", headTrainerClubIds: ["club-1"], emailVerified: true };
  await page.route("**/api/auth/email-change/confirm", async route => {
    calls++;
    body = route.request().postDataJSON();
    await route.fulfill({ json: response });
  });
  await page.goto(confirmationLink());
  await expect(page.getByRole("heading", { name: "Nieuw e-mailadres bevestigen" })).toBeVisible();
  await expect(page).toHaveURL(/\/dashboard\/account$/);
  expect(calls).toBe(0);
  await expect(page.getByText(newEmail, { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "E-mailadres bevestigen", exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: "Je e-mailadres is gewijzigd." })).toBeVisible();
  expect(calls).toBe(1);
  expect(body).toEqual({ newEmail, token: "sensitive+/=token" });
  expect(await page.evaluate(() => localStorage.getItem("token"))).toBe(response.token);
  const stored = await page.evaluate(() => JSON.parse(localStorage.getItem("auth_user")!));
  expect(stored).toMatchObject({ email: newEmail, role: "Trainer", organizationId: response.organizationId, memberships: response.memberships, headTrainerClubIds: response.headTrainerClubIds, emailVerified: true });
  await expect(page.getByText(newEmail, { exact: true })).toBeVisible();
});

test("logged-out email link never leaks the fragment into login or its redirect", async ({ page }) => {
  let confirmCalls = 0;
  await page.route("**/api/auth/email-change/confirm", route => { confirmCalls++; return route.fulfill({ status: 400 }); });
  await page.goto(confirmationLink());
  await expect(page).toHaveURL(/\/login\?redirect=%2Fdashboard%2Faccount$/);
  await expect(page.getByText("Log eerst in met je huidige e-mailadres. Open daarna opnieuw de bevestigingslink uit je e-mail.")).toBeVisible();
  expect(confirmCalls).toBe(0);
  expect(await page.evaluate(() => JSON.stringify({ local: { ...localStorage }, session: { ...sessionStorage } }))).not.toContain("sensitive");
});
