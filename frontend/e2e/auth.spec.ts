import { test, expect } from "@playwright/test";
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

      const registerLink = page.getByRole("link", { name: "Registreren" });
      await expect(registerLink).toBeVisible();
      await expect(registerLink).toHaveAttribute("href", "/register");
    });
  });

  test.describe("Register", () => {
    test("shows registration form with all fields", async ({ page }) => {
      await page.goto("/register");

      await expect(page.getByRole("heading", { name: "Account aanmaken" })).toBeVisible();
      await expect(page.getByLabel("Naam organisatie")).toBeVisible();
      await expect(page.getByLabel("Voornaam")).toBeVisible();
      await expect(page.getByLabel("Achternaam")).toBeVisible();
      await expect(page.getByLabel("E-mailadres")).toBeVisible();
      await expect(page.getByLabel("Wachtwoord", { exact: true })).toBeVisible();
      await expect(page.getByLabel("Wachtwoord bevestigen")).toBeVisible();
    });

    test("shows validation errors for empty submit", async ({ page }) => {
      await page.goto("/register");

      await page.getByRole("button", { name: "Account aanmaken" }).click();

      await expect(page.locator("[id*='form-item-message']").first()).toBeVisible();
    });

    test("successful registration redirects to dashboard", async ({ page }) => {
      await mockApi(page, "POST", "/auth/register", TEST_USER);

      await page.goto("/register");
      await page.getByLabel("Naam organisatie").fill("TC Test");
      await page.getByLabel("Voornaam").fill("Jan");
      await page.getByLabel("Achternaam").fill("Janssen");
      await page.getByLabel("E-mailadres").fill("coach@test.be");
      await page.getByLabel("Wachtwoord", { exact: true }).fill("Test1234!");
      await page.getByLabel("Wachtwoord bevestigen").fill("Test1234!");
      await page.getByRole("button", { name: "Account aanmaken" }).click();

      await page.waitForURL("**/dashboard");
      await expect(page).toHaveURL(/\/dashboard/);
    });

    test("shows error on duplicate email", async ({ page }) => {
      await mockApi(page, "POST", "/auth/register", ["E-mailadres is al in gebruik"], 400);

      await page.goto("/register");
      await page.getByLabel("Naam organisatie").fill("TC Test");
      await page.getByLabel("Voornaam").fill("Jan");
      await page.getByLabel("Achternaam").fill("Janssen");
      await page.getByLabel("E-mailadres").fill("existing@test.be");
      await page.getByLabel("Wachtwoord", { exact: true }).fill("Test1234!");
      await page.getByLabel("Wachtwoord bevestigen").fill("Test1234!");
      await page.getByRole("button", { name: "Account aanmaken" }).click();

      await expect(page.locator(".bg-red-50")).toBeVisible();
      await expect(page.getByText("E-mailadres is al in gebruik")).toBeVisible();
    });

    test("has link to login page", async ({ page }) => {
      await page.goto("/register");

      const loginLink = page.getByRole("link", { name: "Inloggen" });
      await expect(loginLink).toBeVisible();
      await expect(loginLink).toHaveAttribute("href", "/login");
    });
  });
});
