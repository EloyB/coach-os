import { test, expect } from "@playwright/test";

test.describe("Trial lock", () => {
  test("expired-trial user is routed to the billing lock screen", async ({ page }) => {
    test.skip(
      !process.env.E2E_EXPIRED_EMAIL || !process.env.E2E_EXPIRED_PASSWORD,
      "E2E_EXPIRED_EMAIL/E2E_EXPIRED_PASSWORD not set — requires a seeded expired-trial org (see Task 8 seed script)."
    );

    await page.goto("/login");
    await page.getByLabel(/e-mailadres/i).fill(process.env.E2E_EXPIRED_EMAIL!);
    await page.getByLabel(/wachtwoord/i).fill(process.env.E2E_EXPIRED_PASSWORD!);
    await page.getByRole("button", { name: /inloggen/i }).click();

    // Any protected navigation should bounce to the billing lock screen once the
    // backend returns 403 { code: "subscription_required" } for a locked org.
    await page.goto("/dashboard");
    await expect(page).toHaveURL(/\/billing/);
    await expect(page.getByText(/proefperiode is afgelopen/i)).toBeVisible();
  });
});
