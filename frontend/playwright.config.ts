import { defineConfig, devices } from "@playwright/test";

// Mocked specs intercept calls to this URL (see e2e/helpers.ts); live specs talk
// to the real backend on it. Pinned here so the suite does not depend on an
// untracked .env.local — without it apiClient would call /api on the Next
// server itself and every mock would miss.
const API_URL = "http://localhost:5142/api";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "html",
  use: {
    baseURL: "http://localhost:5317",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      // Default run: all API calls are mocked, no backend needed.
      name: "chromium",
      testIgnore: "live/**",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      // Opt-in: `bun run test:e2e:live`. Needs the docker stack + seed data.
      name: "live",
      testMatch: "live/**/*.spec.ts",
      // Publieke endpoints delen één rate-limitvenster van 30/min; de live-helpers
      // wachten bij een 429 tot het volgende venster, wat tot een minuut kan duren.
      timeout: 120_000,
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    command: "bun run dev",
    url: "http://localhost:5317",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: { NEXT_PUBLIC_API_URL: API_URL },
  },
});
