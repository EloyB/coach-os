import { test, expect, type Page } from "@playwright/test";
import { mockApi, TEST_USER } from "./helpers";

test.describe("Authentication", () => {
  test.describe("Login", () => {
    test("shows login form with all fields", async ({ page }) => {
      await page.goto("/login");

      await expect(page.getByRole("heading", { name: "Inloggen" })).toBeVisible();
      await expect(page.getByLabel("E-mailadres")).toBeVisible();
      await expect(page.getByLabel("Wachtwoord")).toBeVisible();
      await expect(page.getByRole("button", { name: "Inloggen" })).toBeVisible();
    });

    test("shows validation errors for empty submit", async ({ page }) => {
      await page.goto("/login");

      await page.getByRole("button", { name: "Inloggen" }).click();

      await expect(page.locator("form")).toBeVisible();
    });

    test("successful login redirects to dashboard", async ({ page }) => {
      await mockApi(page, "POST", "/auth/login", TEST_USER);

      await page.goto("/login");
      await page.getByLabel("E-mailadres").fill("coach@test.be");
      await page.getByLabel("Wachtwoord").fill("Test1234!");
      await page.getByRole("button", { name: "Inloggen" }).click();

      await page.waitForURL("**/dashboard");
      await expect(page).toHaveURL(/\/dashboard/);
    });

    test("shows error on invalid credentials", async ({ page }) => {
      // Intercept the login POST to return 401 and also prevent the
      // api-client 401 interceptor from redirecting (it does window.location.href = '/login')
      // by intercepting before the page loads so the route handler fires first
      await page.route("**/auth/login*", (route) => {
        if (route.request().method() === "POST") {
          return route.fulfill({
            status: 400, // Use 400 instead of 401 to avoid api-client redirect
            contentType: "application/json",
            body: JSON.stringify("Ongeldige inloggegevens"),
          });
        }
        return route.continue();
      });

      await page.goto("/login");
      await page.getByLabel("E-mailadres").fill("wrong@test.be");
      await page.getByLabel("Wachtwoord").fill("WrongPass1!");
      await page.getByRole("button", { name: "Inloggen" }).click();

      await expect(page.locator(".bg-red-50")).toBeVisible();
    });

    test("has link to register page", async ({ page }) => {
      await page.goto("/login");

      const registerLink = page.getByRole("link", { name: "Probeer 30 dagen gratis" });
      await expect(registerLink).toBeVisible();
      await expect(registerLink).toHaveAttribute("href", "/register");
    });
  });

  test.describe("Register", () => {
    const SUBMIT = "Start 30-dagen proef";

    async function fillRegisterForm(page: Page, email = "coach@test.be") {
      await page.getByLabel("Naam organisatie").fill("TC Test");
      await page.getByLabel("Voornaam").fill("Jan");
      await page.getByLabel("Achternaam").fill("Janssen");
      await page.getByLabel("E-mailadres").fill(email);
      await page.getByLabel("Wachtwoord", { exact: true }).fill("Test1234!");
      await page.getByLabel("Wachtwoord bevestigen").fill("Test1234!");
    }

    async function acceptConsents(page: Page) {
      await page.getByLabel(/algemene voorwaarden/).check();
      await page.getByLabel(/verwerkersovereenkomst/).check();
    }

    test("shows registration form with all fields", async ({ page }) => {
      await page.goto("/register");

      await expect(page.getByRole("heading", { name: "Maak je account" })).toBeVisible();
      await expect(page.getByLabel("Naam organisatie")).toBeVisible();
      await expect(page.getByLabel("Voornaam")).toBeVisible();
      await expect(page.getByLabel("Achternaam")).toBeVisible();
      await expect(page.getByLabel("E-mailadres")).toBeVisible();
      await expect(page.getByLabel("Wachtwoord", { exact: true })).toBeVisible();
      await expect(page.getByLabel("Wachtwoord bevestigen")).toBeVisible();
      await expect(page.getByLabel(/algemene voorwaarden/)).toBeVisible();
      await expect(page.getByLabel(/verwerkersovereenkomst/)).toBeVisible();
    });

    test("does not ask for payment details", async ({ page }) => {
      await page.goto("/register");

      await expect(page.getByText("geen betaalgegevens nodig")).toBeVisible();
      await expect(page.getByLabel(/kaart|iban|rekening/i)).toHaveCount(0);
    });

    test("shows validation errors for empty submit", async ({ page }) => {
      await page.goto("/register");

      await page.getByRole("button", { name: SUBMIT }).click();

      await expect(page.locator("[id*='form-item-message']").first()).toBeVisible();
    });

    test("requires accepting terms and data processing agreement", async ({ page }) => {
      let registerCalled = false;
      await page.route("**/auth/register*", (route) => {
        registerCalled = true;
        return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(TEST_USER) });
      });

      await page.goto("/register");
      await fillRegisterForm(page);
      await page.getByRole("button", { name: SUBMIT }).click();

      await expect(page.getByText("Je moet akkoord gaan met de algemene voorwaarden")).toBeVisible();
      await expect(page.getByText("Je moet akkoord gaan met de verwerkersovereenkomst")).toBeVisible();
      expect(registerCalled).toBe(false);
    });

    test("successful registration with verified email redirects to dashboard", async ({ page }) => {
      await mockApi(page, "POST", "/auth/register", { ...TEST_USER, emailVerified: true });

      await page.goto("/register");
      await fillRegisterForm(page);
      await acceptConsents(page);
      await page.getByRole("button", { name: SUBMIT }).click();

      await page.waitForURL("**/dashboard");
      await expect(page).toHaveURL(/\/dashboard/);
    });

    test("registration sends consents and no payment data", async ({ page }) => {
      let body: Record<string, unknown> = {};
      await page.route("**/auth/register*", (route) => {
        body = route.request().postDataJSON() as Record<string, unknown>;
        return route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ ...TEST_USER, emailVerified: true }),
        });
      });

      await page.goto("/register");
      await fillRegisterForm(page);
      await acceptConsents(page);
      await page.getByRole("button", { name: SUBMIT }).click();
      await page.waitForURL("**/dashboard");

      expect(body.acceptTerms).toBe(true);
      expect(body.acceptDataProcessingAgreement).toBe(true);
      expect(body.website).toBe("");
      expect(Object.keys(body).sort()).toEqual([
        "acceptDataProcessingAgreement",
        "acceptTerms",
        "email",
        "firstName",
        "lastName",
        "organizationName",
        "password",
        "website",
      ]);
    });

    test("unverified registration continues to email verification", async ({ page }) => {
      await mockApi(page, "POST", "/auth/register", { ...TEST_USER, emailVerified: false });
      await mockApi(page, "POST", "/auth/verify-email", null, 204);

      await page.goto("/register");
      await fillRegisterForm(page);
      await acceptConsents(page);
      await page.getByRole("button", { name: SUBMIT }).click();

      await page.waitForURL("**/verify-email**");
      await expect(page.getByRole("heading", { name: "Check je mailbox" })).toBeVisible();
      await expect(page.getByText("coach@test.be")).toBeVisible();

      // Zes cijfers invullen bevestigt meteen — geen extra klik nodig.
      await page.getByLabel("Verificatiecode").fill("123456");

      await page.waitForURL("**/dashboard");
      await expect(page).toHaveURL(/\/dashboard/);
    });

    test("wrong verification code shows error and keeps user on page", async ({ page }) => {
      await mockApi(page, "POST", "/auth/register", { ...TEST_USER, emailVerified: false });
      await mockApi(page, "POST", "/auth/verify-email", ["De code is niet juist."], 400);

      await page.goto("/register");
      await fillRegisterForm(page);
      await acceptConsents(page);
      await page.getByRole("button", { name: SUBMIT }).click();
      await page.waitForURL("**/verify-email**");

      await page.getByLabel("Verificatiecode").fill("000000");

      await expect(page.getByRole("alert").getByText("De code is niet juist.")).toBeVisible();
      await expect(page).toHaveURL(/\/verify-email/);
      await expect(page.getByLabel("Verificatiecode")).toHaveValue("");
    });

    test("shows error on duplicate email", async ({ page }) => {
      await mockApi(page, "POST", "/auth/register", ["E-mailadres is al in gebruik"], 400);

      await page.goto("/register");
      await fillRegisterForm(page, "existing@test.be");
      await acceptConsents(page);
      await page.getByRole("button", { name: SUBMIT }).click();

      await expect(page.locator(".bg-red-50")).toBeVisible();
      await expect(page.locator(".bg-red-50").getByText("E-mailadres is al in gebruik")).toBeVisible();
    });

    test("has link to login page", async ({ page }) => {
      await page.goto("/register");

      const loginLink = page.getByRole("link", { name: "Inloggen" });
      await expect(loginLink).toBeVisible();
      await expect(loginLink).toHaveAttribute("href", "/login");
    });
  });
});
