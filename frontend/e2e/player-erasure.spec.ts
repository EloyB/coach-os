import { test, expect, type Page, type Route } from "@playwright/test";
import { loginViaStorage, TEST_USER } from "./helpers";

// "Speler verwijderen" vanuit het kampinschrijvingsdetail, volledig tegen gemockte API's.
const CAMP_ID = "c1111111-1111-4111-8111-111111111111";
const ENROLLMENT_ID = "e1111111-1111-4111-8111-111111111111";
const NAME = "Synthetic Camper";

const camp = {
  id: CAMP_ID, name: "Herfstkamp", description: null, tennisClubId: "55555555-5555-5555-5555-555555555555",
  tennisClubName: "TC De Aces", level: null, price: 0, startDate: "2024-10-28", endDate: "2024-10-31",
  registrationDeadline: "2024-10-20", maxParticipants: 20, participantCount: 1, isActive: true, days: [], prices: [],
};

const enrollment = {
  id: ENROLLMENT_ID, participantName: NAME, participantEmail: "camper@example.test", participantPhone: null,
  status: "Confirmed", enrolledAt: "2024-10-01T10:00:00Z", groupName: null, enrollmentGroupId: null,
  isGroupLeader: false, paymentMethod: null, paymentStatus: null, paymentAmount: null, paidAt: null,
  molliePaymentId: null, formResponses: [], campPriceId: null, campPriceLabel: null,
};

function preview(overrides: Record<string, unknown> = {}) {
  return {
    enrollmentId: ENROLLMENT_ID, participantName: NAME, lastParticipation: "2024-10-31", retentionPeriodPassed: true,
    canErase: true, blockingReasons: [], formAnswerCount: 2, groupEffect: "None", otherGroupMembers: [],
    updatedAt: "2024-10-01T10:00:00.123456Z", ...overrides,
  };
}

interface Mocks {
  preview: Record<string, unknown>;
  eraseRequests: unknown[];
  eraseStatus: number;
}

async function mockCampPage(page: Page, mocks: Mocks): Promise<void> {
  await page.route("**/api/**", (route: Route) => {
    const url = new URL(route.request().url());
    const path = url.pathname.replace(/^\/api/, "");
    const method = route.request().method();
    if (path === `/privacy/camp-enrollments/${ENROLLMENT_ID}/erasure-preview`) return route.fulfill({ json: mocks.preview });
    if (path === `/privacy/camp-enrollments/${ENROLLMENT_ID}/erasure` && method === "POST") {
      mocks.eraseRequests.push(route.request().postDataJSON());
      return mocks.eraseStatus === 200
        ? route.fulfill({ json: { enrollmentId: ENROLLMENT_ID, erasedAt: "2026-11-10T12:00:00Z" } })
        : route.fulfill({ status: mocks.eraseStatus, json: ["De ingevoerde naam komt niet exact overeen met de naam van de speler."] });
    }
    if (path === `/camps/${CAMP_ID}`) return route.fulfill({ json: camp });
    if (path === `/camps/${CAMP_ID}/enrollments`) return route.fulfill({ json: [enrollment] });
    if (path.endsWith("/form")) return route.fulfill({ status: 404, json: [] });
    return route.fulfill({ json: [] });
  });
}

async function openErasureDialog(page: Page): Promise<void> {
  await page.goto(`/dashboard/camps/${CAMP_ID}`);
  await page.getByText(NAME, { exact: true }).filter({ visible: true }).first().click();
  await page.getByRole("button", { name: "Speler verwijderen" }).click();
  await expect(page.getByRole("heading", { name: "Speler definitief verwijderen" })).toBeVisible();
}

test.describe("speler verwijderen vanuit kampinschrijving", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaStorage(page);
  });

  test("toont overzicht en vereist exacte naam vóór verwijderen", async ({ page }) => {
    const mocks: Mocks = { preview: preview(), eraseRequests: [], eraseStatus: 200 };
    await mockCampPage(page, mocks);
    await openErasureDialog(page);

    await expect(page.getByText("2 formulierantwoorden")).toBeVisible();
    const confirm = page.getByRole("button", { name: "Definitief verwijderen" });
    await expect(confirm).toBeDisabled();
    await page.getByLabel(`Typ ter bevestiging de naam van de speler: ${NAME}`).fill("synthetic camper");
    await expect(confirm).toBeDisabled();
    await page.getByLabel(`Typ ter bevestiging de naam van de speler: ${NAME}`).fill(NAME);
    await confirm.click();

    await expect(page.getByText("Speler verwijderd")).toBeVisible();
    expect(mocks.eraseRequests).toEqual([
      { confirmationName: NAME, newGroupLeaderId: null, expectedUpdatedAt: "2024-10-01T10:00:00.123456Z" },
    ]);
  });

  test("geblokkeerde speler toont redenen en geen verwijderknop", async ({ page }) => {
    const mocks: Mocks = {
      preview: preview({ canErase: false, blockingReasons: ["financial_records_present", "pending_mail"] }),
      eraseRequests: [], eraseStatus: 200,
    };
    await mockCampPage(page, mocks);
    await openErasureDialog(page);

    await expect(page.getByText("Er zijn betalingen gekoppeld; financiële gegevens moeten bewaard blijven")).toBeVisible();
    await expect(page.getByText("Er wordt nog een mail verstuurd. Probeer het binnen enkele minuten opnieuw")).toBeVisible();
    await expect(page.getByRole("button", { name: "Definitief verwijderen" })).toHaveCount(0);
  });

  test("groepsleider moet een nieuwe leider kiezen", async ({ page }) => {
    const other = { id: "e2222222-2222-4222-8222-222222222222", name: "Other Camper" };
    const mocks: Mocks = {
      preview: preview({ groupEffect: "TransferLeader", otherGroupMembers: [other] }),
      eraseRequests: [], eraseStatus: 200,
    };
    await mockCampPage(page, mocks);
    await openErasureDialog(page);

    await page.getByLabel(`Typ ter bevestiging de naam van de speler: ${NAME}`).fill(NAME);
    const confirm = page.getByRole("button", { name: "Definitief verwijderen" });
    await expect(confirm).toBeDisabled();
    await page.getByLabel("Nieuwe groepsleider").selectOption(other.id);
    await confirm.click();

    await expect.poll(() => mocks.eraseRequests).toEqual([
      { confirmationName: NAME, newGroupLeaderId: other.id, expectedUpdatedAt: "2024-10-01T10:00:00.123456Z" },
    ]);
  });

  test("serverfout toont melding en geen succes", async ({ page }) => {
    const mocks: Mocks = { preview: preview(), eraseRequests: [], eraseStatus: 400 };
    await mockCampPage(page, mocks);
    await openErasureDialog(page);

    await page.getByLabel(`Typ ter bevestiging de naam van de speler: ${NAME}`).fill(NAME);
    await page.getByRole("button", { name: "Definitief verwijderen" }).click();

    await expect(page.getByText("De ingevoerde naam komt niet exact overeen met de naam van de speler.")).toBeVisible();
    await expect(page.getByText("Speler verwijderd")).toHaveCount(0);
  });

  test("trainer ziet de actie niet", async ({ page }) => {
    await page.evaluate((user) => {
      localStorage.setItem("auth_user", JSON.stringify({ ...user, role: "Trainer" }));
    }, { userId: TEST_USER.userId, email: TEST_USER.email, organizationId: TEST_USER.organizationId });
    await mockCampPage(page, { preview: preview(), eraseRequests: [], eraseStatus: 200 });
    await page.goto(`/dashboard/camps/${CAMP_ID}`);
    await page.getByText(NAME, { exact: true }).filter({ visible: true }).first().click();

    await expect(page.getByRole("button", { name: "Speler verwijderen" })).toHaveCount(0);
  });
});
