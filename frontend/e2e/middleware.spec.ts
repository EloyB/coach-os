import { test, expect } from "@playwright/test";
import { loginViaStorage } from "./helpers";

test.describe("Route Protection Middleware", () => {
  test("redirects unauthenticated user from /dashboard to /login", async ({ page }) => {
    await page.goto("/dashboard");

    await expect(page).toHaveURL(/\/login/);
  });

  test("preserves redirect query param when redirecting to login", async ({ page }) => {
    await page.goto("/dashboard/lessons");

    await expect(page).toHaveURL(/\/login\?redirect=/);
  });

  test("redirects authenticated user from /login to /dashboard", async ({ page }) => {
    await loginViaStorage(page);

    await page.goto("/login");

    await expect(page).toHaveURL(/\/dashboard/);
  });

  test("redirects authenticated user from /register to /dashboard", async ({ page }) => {
    await loginViaStorage(page);

    await page.goto("/register");

    await expect(page).toHaveURL(/\/dashboard/);
  });

  test("allows public enrollment page without auth", async ({ page }) => {
    await page.goto("/enroll/some-id");

    // Should NOT redirect to login — page loads (may show loading/error, that's fine)
    await expect(page).toHaveURL(/\/enroll\/some-id/);
  });

  test("home page redirects to /login", async ({ page }) => {
    await page.goto("/");

    await expect(page).toHaveURL(/\/login/);
  });
});
