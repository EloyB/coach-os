import { test, expect } from "@playwright/test";
import {
  API_URL,
  apiLogin,
  authHeaders,
  futureDate,
  uniqueCourt,
  type LoginResult,
} from "./live-helpers";

/**
 * Live API-tests voor de betaalmethodes- en inschrijfwijze-flags op een
 * lessenreeks (allowSoloEnrollment / allowGroupEnrollment /
 * acceptOnlinePayment / acceptManualPayment).
 *
 * TC De Aces (SEED_ADMIN) heeft GEEN Mollie-koppeling, dus acceptOnlinePayment
 * moet daar altijd 400 geven. De seed bevat twee vaste reeksen om de
 * inschrijfwijze-afdwinging te testen: "Voorjaarslessen Beginners" (solo-only)
 * en "Competitietraining Gevorderd" (group-only) — bewust niet muteren.
 */
test.describe.configure({ mode: "serial" });

/** Bouwt een minimale, geldige create-payload; overschrijf enkel wat de test test. */
function baseSeriesPayload(
  clubId: string,
  namePrefix: string,
  overrides: Record<string, unknown> = {},
) {
  return {
    name: `${namePrefix} ${Date.now()}`,
    description: "Aangemaakt door de live E2E-suite (betaalmethodes)",
    level: 1,
    price: 100,
    startDate: futureDate(1),
    endDate: futureDate(120),
    registrationDeadline: new Date(
      Date.now() + 90 * 24 * 60 * 60 * 1000,
    ).toISOString(),
    maxRegistrations: 50,
    tennisClubId: clubId,
    weeklyTemplate: [],
    lessons: [
      {
        date: futureDate(7),
        startTime: "17:00",
        endTime: "18:00",
        courtName: uniqueCourt(namePrefix),
        maxStudents: 4,
      },
    ],
    allowSoloEnrollment: true,
    allowGroupEnrollment: true,
    acceptOnlinePayment: false,
    acceptManualPayment: true,
    ...overrides,
  };
}

test.describe("Live API — betaalmethodes bij aanmaken reeks", () => {
  let auth: LoginResult;
  let clubId: string;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    const clubs = await request.get(`${API_URL}/tennisclubs`, {
      headers: authHeaders(auth),
    });
    const clubList = (await clubs.json()) as { id: string }[];
    clubId = clubList[0].id;
  });

  test("acceptOnlinePayment true zonder Mollie-koppeling geeft 400", async ({
    request,
  }) => {
    const response = await request.post(`${API_URL}/lessonseries`, {
      headers: authHeaders(auth),
      data: baseSeriesPayload(clubId, "E2E Online Zonder Mollie", {
        acceptOnlinePayment: true,
        acceptManualPayment: false,
      }),
    });
    expect(response.status()).toBe(400);
    const body = (await response.json()) as string[];
    expect(body.join(" ")).toMatch(/Mollie/i);
  });

  test("acceptOnlinePayment false + acceptManualPayment true slaagt en GET toont de 4 flags", async ({
    request,
  }) => {
    const create = await request.post(`${API_URL}/lessonseries`, {
      headers: authHeaders(auth),
      data: baseSeriesPayload(clubId, "E2E Overschrijving Only", {
        allowSoloEnrollment: true,
        allowGroupEnrollment: false,
        acceptOnlinePayment: false,
        acceptManualPayment: true,
      }),
    });
    expect(
      create.ok(),
      `Aanmaken faalde (${create.status()}): ${await create.text()}`,
    ).toBeTruthy();
    const id = (await create.json()) as string;

    const read = await request.get(`${API_URL}/lessonseries/${id}`, {
      headers: authHeaders(auth),
    });
    expect(read.ok()).toBeTruthy();
    const dto = (await read.json()) as {
      allowSoloEnrollment: boolean;
      allowGroupEnrollment: boolean;
      acceptOnlinePayment: boolean;
      acceptManualPayment: boolean;
    };
    expect(dto.allowSoloEnrollment).toBe(true);
    expect(dto.allowGroupEnrollment).toBe(false);
    expect(dto.acceptOnlinePayment).toBe(false);
    expect(dto.acceptManualPayment).toBe(true);
  });

  test("beide inschrijfwijzes uit geeft 400", async ({ request }) => {
    const response = await request.post(`${API_URL}/lessonseries`, {
      headers: authHeaders(auth),
      data: baseSeriesPayload(clubId, "E2E Geen Inschrijfwijze", {
        allowSoloEnrollment: false,
        allowGroupEnrollment: false,
      }),
    });
    expect(response.status()).toBe(400);
    const body = (await response.json()) as string[];
    expect(body.join(" ")).toMatch(/inschrijfwijze/i);
  });

  test("beide betaalmethodes uit geeft 400", async ({ request }) => {
    const response = await request.post(`${API_URL}/lessonseries`, {
      headers: authHeaders(auth),
      data: baseSeriesPayload(clubId, "E2E Geen Betaalmethode", {
        acceptOnlinePayment: false,
        acceptManualPayment: false,
      }),
    });
    expect(response.status()).toBe(400);
    const body = (await response.json()) as string[];
    expect(body.join(" ")).toMatch(/betaalmethode/i);
  });
});

test.describe("Live API — inschrijfwijze afdwingen op solo/group-only reeksen", () => {
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

  test("GET publieke reeks toont allowSoloEnrollment/allowGroupEnrollment voor de solo-only reeks", async ({
    request,
  }) => {
    const response = await request.get(
      `${API_URL}/public/lessonseries/${soloOnlySeriesId}`,
    );
    expect(response.ok()).toBeTruthy();
    const dto = (await response.json()) as {
      allowSoloEnrollment: boolean;
      allowGroupEnrollment: boolean;
    };
    expect(dto.allowSoloEnrollment).toBe(true);
    expect(dto.allowGroupEnrollment).toBe(false);
  });

  test("GET publieke reeks toont allowSoloEnrollment/allowGroupEnrollment voor de group-only reeks", async ({
    request,
  }) => {
    const response = await request.get(
      `${API_URL}/public/lessonseries/${groupOnlySeriesId}`,
    );
    expect(response.ok()).toBeTruthy();
    const dto = (await response.json()) as {
      allowSoloEnrollment: boolean;
      allowGroupEnrollment: boolean;
    };
    expect(dto.allowSoloEnrollment).toBe(false);
    expect(dto.allowGroupEnrollment).toBe(true);
  });

  test("GROUP-inschrijving op de solo-only reeks geeft 400", async ({
    request,
  }) => {
    const response = await request.post(
      `${API_URL}/public/lessonseries/${soloOnlySeriesId}/enroll`,
      {
        data: {
          studentName: "E2E Groep Op Solo-Only",
          studentEmail: `e2e-group-on-solo-${Date.now()}@test.be`,
          dateOfBirth: "1990-01-01",
          responses: [],
          enrollmentType: "group",
          groupMembers: [
            {
              studentName: "E2E Groepslid",
              dateOfBirth: "1991-01-01",
              responses: [],
            },
          ],
        },
      },
    );
    expect(response.status()).toBe(400);
    const body = (await response.json()) as string[];
    expect(body.join(" ")).toMatch(/groep/i);
  });

  test("SOLO-inschrijving op de group-only reeks geeft 400", async ({
    request,
  }) => {
    const response = await request.post(
      `${API_URL}/public/lessonseries/${groupOnlySeriesId}/enroll`,
      {
        data: {
          studentName: "E2E Solo Op Group-Only",
          studentEmail: `e2e-solo-on-group-${Date.now()}@test.be`,
          dateOfBirth: "1990-01-01",
          responses: [],
          enrollmentType: "solo",
        },
      },
    );
    expect(response.status()).toBe(400);
    const body = (await response.json()) as string[];
    expect(body.join(" ")).toMatch(/solo/i);
  });
});
