import { test, expect } from "@playwright/test";
import {
  API_URL,
  SEED_ADMIN,
  SEED_SECOND_ORG_ADMIN,
  apiLogin,
  authHeaders,
  createTestSeries,
  futureDate,
  uniqueCourt,
  type LoginResult,
} from "./live-helpers";

/**
 * Live API-tests tegen de draaiende backend en geseede database.
 * Dekt de vier klantvragen van Thomas plus de betaalbug.
 *
 * Draait NIET parallel: meerdere tests muteren dezelfde geseede reeks.
 */
test.describe.configure({ mode: "serial" });

test.describe("Live API — inschrijving annuleren", () => {
  let auth: LoginResult;
  let seriesId: string;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    seriesId = (await createTestSeries(request, auth, "E2E Annuleren")).id;

    // Eigen inschrijvingen aanmaken: de reeks is vers, dus leeg.
    for (let i = 0; i < 3; i++) {
      const response = await request.post(
        `${API_URL}/public/lessonseries/${seriesId}/enroll`,
        {
          data: {
            studentName: `E2E Annuleerbaar ${i}`,
            studentEmail: `e2e-cancel-${Date.now()}-${i}@test.be`,
            dateOfBirth: "1992-04-04",
            responses: [],
          },
        },
      );
      expect(
        response.ok(),
        `Aanmaken testinschrijving ${i} faalde (${response.status()})`,
      ).toBeTruthy();
    }
  });

  test("beheerder annuleert inschrijving en plaats komt vrij", async ({
    request,
  }) => {
    const before = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await before.json()) as {
      id: string;
      status: string;
    }[];
    const active = enrollments.filter((e) => e.status !== "Cancelled");
    expect(
      active.length,
      "Geen actieve inschrijvingen om te annuleren",
    ).toBeGreaterThan(0);

    const target = active[0];

    const cancel = await request.delete(
      `${API_URL}/lessonseries/${seriesId}/enrollments/${target.id}`,
      { headers: authHeaders(auth) },
    );
    expect(cancel.status()).toBe(204);

    const after = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const updated = (await after.json()) as { id: string; status: string }[];
    const cancelled = updated.find((e) => e.id === target.id);

    expect(cancelled?.status).toBe("Cancelled");
    expect(updated.filter((e) => e.status !== "Cancelled").length).toBe(
      active.length - 1,
    );
  });

  test("tweede annulering geeft 400", async ({ request }) => {
    const list = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await list.json()) as { id: string; status: string }[];
    const alreadyCancelled = enrollments.find((e) => e.status === "Cancelled");
    expect(alreadyCancelled).toBeDefined();

    const response = await request.delete(
      `${API_URL}/lessonseries/${seriesId}/enrollments/${alreadyCancelled!.id}`,
      { headers: authHeaders(auth) },
    );
    expect(response.status()).toBe(400);
  });

  test("onbekende inschrijving geeft 404", async ({ request }) => {
    const response = await request.delete(
      `${API_URL}/lessonseries/${seriesId}/enrollments/${"00000000-0000-0000-0000-000000000000"}`,
      { headers: authHeaders(auth) },
    );
    expect(response.status()).toBe(404);
  });

  test("zonder token geeft 401", async ({ request }) => {
    const response = await request.delete(
      `${API_URL}/lessonseries/${seriesId}/enrollments/${"00000000-0000-0000-0000-000000000000"}`,
    );
    expect(response.status()).toBe(401);
  });

  test("andere organisatie kan niet annuleren", async ({ request }) => {
    const otherAuth = await apiLogin(request, SEED_SECOND_ORG_ADMIN);

    const list = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await list.json()) as { id: string; status: string }[];
    const active = enrollments.find((e) => e.status !== "Cancelled");
    expect(active).toBeDefined();

    const response = await request.delete(
      `${API_URL}/lessonseries/${seriesId}/enrollments/${active!.id}`,
      { headers: authHeaders(otherAuth) },
    );
    // 404 i.p.v. 403: we lekken niet dat de inschrijving bestaat.
    expect(response.status()).toBe(404);

    const verify = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const after = (await verify.json()) as { id: string; status: string }[];
    expect(after.find((e) => e.id === active!.id)?.status).not.toBe("Cancelled");
  });
});

test.describe("Live API — lesmoment toevoegen", () => {
  let auth: LoginResult;
  let seriesId: string;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    seriesId = (await createTestSeries(request, auth)).id;
  });

  test("lesmoment buiten de reeksdatums wordt geaccepteerd", async ({
    request,
  }) => {
    // Thomas' concrete geval: extra blok in december aan een lopende reeks.
    const response = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: futureDate(200),
          startTime: "09:00",
          endTime: "13:00",
          courtName: uniqueCourt("Toevoegen"),
          maxStudents: 4,
        },
      },
    );
    expect(response.status()).toBe(201);
  });

  test("lesmoment zonder eindtijd wordt geweigerd", async ({ request }) => {
    const response = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: futureDate(201),
          startTime: "09:00",
          courtName: uniqueCourt("Zonder Eindtijd"),
          maxStudents: 4,
        },
      },
    );
    expect(response.status()).toBe(400);
  });
});

test.describe("Live API — baan-bezettingscheck", () => {
  let auth: LoginResult;
  let seriesId: string;
  const courtName = uniqueCourt("Conflictbaan");
  const conflictDate = futureDate(210);

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    seriesId = (await createTestSeries(request, auth)).id;

    const first = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: conflictDate,
          startTime: "10:00",
          endTime: "11:00",
          courtName,
          maxStudents: 4,
        },
      },
    );
    expect(first.status()).toBe(201);
  });

  test("overlappende les op dezelfde baan wordt geweigerd", async ({
    request,
  }) => {
    const response = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: conflictDate,
          startTime: "10:30",
          endTime: "11:30",
          courtName,
          maxStudents: 4,
        },
      },
    );
    expect(response.status()).toBe(409);
  });

  test("baannaam met andere hoofdletters en spaties botst ook", async ({
    request,
  }) => {
    // Bewijst dat de trim + lower echt naar SQL vertaalt — dat kon een unit test
    // met een gemockte repository niet aantonen.
    const response = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: conflictDate,
          startTime: "10:15",
          endTime: "10:45",
          courtName: `  ${courtName.toUpperCase()}  `,
          maxStudents: 4,
        },
      },
    );
    expect(response.status()).toBe(409);
  });

  test("zelfde baan op ander tijdstip mag wel", async ({ request }) => {
    const response = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: conflictDate,
          startTime: "14:00",
          endTime: "15:00",
          courtName,
          maxStudents: 4,
        },
      },
    );
    expect(response.status()).toBe(201);
  });

  test("parallelle les op andere baan mag wel", async ({ request }) => {
    // Kern van Thomas' vraag 4: twee velden per avond.
    const response = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: conflictDate,
          startTime: "10:00",
          endTime: "11:00",
          courtName: uniqueCourt("Conflictbaan 2"),
          maxStudents: 4,
        },
      },
    );
    expect(response.status()).toBe(201);
  });

  test("les zonder baannaam omzeilt de check", async ({ request }) => {
    const response = await request.post(
      `${API_URL}/lessonseries/${seriesId}/lessons`,
      {
        headers: authHeaders(auth),
        data: {
          date: conflictDate,
          startTime: "10:00",
          endTime: "11:00",
          maxStudents: 4,
        },
      },
    );
    expect(response.status()).toBe(201);
  });
});

test.describe("Live API — prijsmatrix", () => {
  let auth: LoginResult;
  let seriesId: string;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    seriesId = (await createTestSeries(request, auth)).id;
  });

  test.afterAll(async ({ request }) => {
    // Matrix weer wissen zodat andere tests de legacy prijs zien.
    await request.put(`${API_URL}/lessonseries/${seriesId}/prices`, {
      headers: authHeaders(auth),
      data: { prices: [] },
    });
  });

  test("matrix opslaan en teruglezen", async ({ request }) => {
    const save = await request.put(
      `${API_URL}/lessonseries/${seriesId}/prices`,
      {
        headers: authHeaders(auth),
        data: {
          prices: [
            { category: 1, groupSize: 4, totalPrice: 480 },
            { category: 1, groupSize: 2, totalPrice: 400 },
            { category: 1, groupSize: 1, totalPrice: 350 },
            { category: 2, groupSize: 4, totalPrice: 360 },
            { category: 2, groupSize: 2, totalPrice: 300 },
          ],
        },
      },
    );
    expect(save.ok()).toBeTruthy();

    const read = await request.get(`${API_URL}/lessonseries/${seriesId}/prices`, {
      headers: authHeaders(auth),
    });
    const prices = (await read.json()) as {
      category: number;
      groupSize: number;
      totalPrice: number;
    }[];

    expect(prices).toHaveLength(5);
    const adultFour = prices.find((p) => p.category === 1 && p.groupSize === 4);
    expect(adultFour?.totalPrice).toBe(480);
    const youthTwo = prices.find((p) => p.category === 2 && p.groupSize === 2);
    expect(youthTwo?.totalPrice).toBe(300);
  });

  test("dubbele cel wordt geweigerd", async ({ request }) => {
    const response = await request.put(
      `${API_URL}/lessonseries/${seriesId}/prices`,
      {
        headers: authHeaders(auth),
        data: {
          prices: [
            { category: 1, groupSize: 4, totalPrice: 480 },
            { category: 1, groupSize: 4, totalPrice: 500 },
          ],
        },
      },
    );
    expect(response.status()).toBe(400);
  });

  test("negatieve prijs wordt geweigerd", async ({ request }) => {
    const response = await request.put(
      `${API_URL}/lessonseries/${seriesId}/prices`,
      {
        headers: authHeaders(auth),
        data: { prices: [{ category: 1, groupSize: 4, totalPrice: -10 }] },
      },
    );
    expect(response.status()).toBe(400);
  });

  test("ongeldige groepsgrootte wordt geweigerd", async ({ request }) => {
    const response = await request.put(
      `${API_URL}/lessonseries/${seriesId}/prices`,
      {
        headers: authHeaders(auth),
        data: { prices: [{ category: 1, groupSize: 99, totalPrice: 100 }] },
      },
    );
    expect(response.status()).toBe(400);
  });

  test("andere organisatie krijgt geen toegang tot de matrix", async ({
    request,
  }) => {
    const otherAuth = await apiLogin(request, SEED_SECOND_ORG_ADMIN);
    const response = await request.get(
      `${API_URL}/lessonseries/${seriesId}/prices`,
      { headers: authHeaders(otherAuth) },
    );
    expect(response.status()).toBe(404);
  });

  test("matrix wissen valt terug op legacy prijs", async ({ request }) => {
    const clear = await request.put(
      `${API_URL}/lessonseries/${seriesId}/prices`,
      { headers: authHeaders(auth), data: { prices: [] } },
    );
    expect(clear.ok()).toBeTruthy();

    const read = await request.get(`${API_URL}/lessonseries/${seriesId}/prices`, {
      headers: authHeaders(auth),
    });
    expect((await read.json()) as unknown[]).toHaveLength(0);
  });
});

test.describe("Live API — geboortedatum en tariefcategorie", () => {
  let auth: LoginResult;
  let seriesId: string;

  test.beforeAll(async ({ request }) => {
    auth = await apiLogin(request);
    seriesId = (await createTestSeries(request, auth)).id;
  });

  test("inschrijving zonder geboortedatum wordt geweigerd", async ({
    request,
  }) => {
    const response = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName: "E2E Zonder Geboortedatum",
          studentEmail: `e2e-nodob-${Date.now()}@test.be`,
          responses: [],
        },
      },
    );
    expect(response.status()).toBe(400);
  });

  test("toekomstige geboortedatum wordt geweigerd", async ({ request }) => {
    const response = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName: "E2E Toekomst",
          studentEmail: `e2e-future-${Date.now()}@test.be`,
          dateOfBirth: futureDate(30),
          responses: [],
        },
      },
    );
    expect(response.status()).toBe(400);
  });

  test("volwassene krijgt categorie Volwassenen", async ({ request }) => {
    const email = `e2e-adult-${Date.now()}@test.be`;
    const submit = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName: "E2E Volwassene",
          studentEmail: email,
          dateOfBirth: "1990-06-15",
          responses: [],
        },
      },
    );
    expect(submit.ok()).toBeTruthy();

    const list = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await list.json()) as {
      studentEmail: string;
      categoryLabel: string | null;
      dateOfBirth: string | null;
    }[];
    const created = enrollments.find((e) => e.studentEmail === email);

    expect(created?.categoryLabel).toBe("Volwassenen");
    expect(created?.dateOfBirth).toBe("1990-06-15");
  });

  test("minderjarige krijgt categorie Jeugd", async ({ request }) => {
    const email = `e2e-youth-${Date.now()}@test.be`;
    const twelveYearsAgo = new Date();
    twelveYearsAgo.setFullYear(twelveYearsAgo.getFullYear() - 12);
    const dob = twelveYearsAgo.toISOString().slice(0, 10);

    const submit = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName: "E2E Jeugdlid",
          studentEmail: email,
          dateOfBirth: dob,
          responses: [],
        },
      },
    );
    expect(submit.ok()).toBeTruthy();

    const list = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await list.json()) as {
      studentEmail: string;
      categoryLabel: string | null;
    }[];

    expect(
      enrollments.find((e) => e.studentEmail === email)?.categoryLabel,
    ).toBe("Jeugd");
  });

  test("groepsinschrijving legt per lid een eigen categorie vast", async ({
    request,
  }) => {
    const stamp = Date.now();
    const leaderEmail = `e2e-leader-${stamp}@test.be`;
    const childEmail = `e2e-child-${stamp}@test.be`;

    const tenYearsAgo = new Date();
    tenYearsAgo.setFullYear(tenYearsAgo.getFullYear() - 10);

    const submit = await request.post(
      `${API_URL}/public/lessonseries/${seriesId}/enroll`,
      {
        data: {
          studentName: "E2E Ouder",
          studentEmail: leaderEmail,
          dateOfBirth: "1985-02-20",
          enrollmentType: "group",
          isOpenToGrouping: false,
          responses: [],
          groupMembers: [
            {
              studentName: "E2E Kind",
              studentEmail: childEmail,
              dateOfBirth: tenYearsAgo.toISOString().slice(0, 10),
              responses: [],
            },
          ],
        },
      },
    );
    expect(submit.ok()).toBeTruthy();

    const list = await request.get(
      `${API_URL}/lessonseries/${seriesId}/enrollments`,
      { headers: authHeaders(auth) },
    );
    const enrollments = (await list.json()) as {
      studentEmail: string;
      categoryLabel: string | null;
    }[];

    expect(
      enrollments.find((e) => e.studentEmail === leaderEmail)?.categoryLabel,
    ).toBe("Volwassenen");
    expect(
      enrollments.find((e) => e.studentEmail === childEmail)?.categoryLabel,
    ).toBe("Jeugd");
  });
});
