import { Page, APIRequestContext, expect } from "@playwright/test";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

/**
 * Helpers voor de LIVE end-to-end suite.
 *
 * In tegenstelling tot `e2e/helpers.ts` — dat alle API-calls mockt — praat deze
 * suite met de echte backend op :5142 en de geseede demo-database. Dat maakt
 * hem trager en afhankelijk van een draaiende stack, maar hij bewijst wél dat
 * migraties, validators, multi-tenancy en de UI samen kloppen.
 *
 * Vereist: docker-stack draait en `bash Scripts/seed-demo-data.sh` is gedraaid.
 */

export const API_URL = "http://localhost:5142/api";

export const SEED_ADMIN = {
  email: "jan@deaces.be",
  password: "Demo1234!",
};

export const SEED_SECOND_ORG_ADMIN = {
  email: "maria@padelbrugge.be",
  password: "Demo1234!",
};

export interface LoginResult {
  token: string;
  userId: string;
  organizationId: string;
  role: string;
  firstName: string;
  lastName: string;
  email: string;
}

/**
 * Tokencache op schijf.
 *
 * De login-endpoint staat 5 pogingen per minuut toe (fixed window, policy
 * "auth"). Een cache in het geheugen is niet genoeg: Playwright laadt de modules
 * per spec-bestand opnieuw, dus elk bestand zou opnieuw inloggen — en twee
 * runs kort na elkaar lopen sowieso tegen de limiet. Een bestandscache met
 * respect voor `expiresAt` lost beide op.
 */
const CACHE_FILE = path.join(os.tmpdir(), "coachos-e2e-tokens.json");

type TokenCache = Record<string, LoginResult>;

function readCache(): TokenCache {
  try {
    return JSON.parse(fs.readFileSync(CACHE_FILE, "utf8")) as TokenCache;
  } catch {
    return {};
  }
}

function writeCache(cache: TokenCache): void {
  try {
    fs.writeFileSync(CACHE_FILE, JSON.stringify(cache), "utf8");
  } catch {
    // Cache is een optimalisatie; falen mag de test niet breken.
  }
}

function isStillValid(entry: LoginResult | undefined): entry is LoginResult {
  if (!entry?.token || !entry.expiresAt) return false;
  // Marge van 5 minuten zodat een token niet halverwege de run verloopt.
  return new Date(entry.expiresAt).getTime() - Date.now() > 5 * 60 * 1000;
}

/** Logt in tegen de API (of hergebruikt een geldig token) en geeft userinfo terug. */
export async function apiLogin(
  request: APIRequestContext,
  credentials: { email: string; password: string } = SEED_ADMIN,
): Promise<LoginResult> {
  const cache = readCache();
  const cached = cache[credentials.email];
  if (isStillValid(cached)) return cached;

  const response = await request.post(`${API_URL}/auth/login`, {
    data: credentials,
  });

  expect(
    response.status(),
    `Login gaf 429 voor ${credentials.email}: de rate limit van 5/min is geraakt. ` +
      `Verwijder ${CACHE_FILE} niet tussen runs, of wacht een minuut.`,
  ).not.toBe(429);
  expect(
    response.ok(),
    `Login faalde voor ${credentials.email} (${response.status()}). Draait de backend en is er geseed?`,
  ).toBeTruthy();

  const result = (await response.json()) as LoginResult;
  cache[credentials.email] = result;
  writeCache(cache);
  return result;
}

/**
 * Zet een écht token in de browserstorage zodat de UI als ingelogde admin laadt.
 * De cookie `has_token` is wat middleware.ts leest voor route-protection.
 */
export async function loginUi(page: Page, auth: LoginResult): Promise<void> {
  await page.goto("/login");
  await page.evaluate(
    ({ token, user }) => {
      localStorage.setItem("token", token);
      localStorage.setItem("auth_user", JSON.stringify(user));
      document.cookie = "has_token=1; path=/; SameSite=Lax";
    },
    {
      token: auth.token,
      user: {
        userId: auth.userId,
        email: auth.email,
        firstName: auth.firstName,
        lastName: auth.lastName,
        organizationId: auth.organizationId,
        role: auth.role,
      },
    },
  );
}

/** Geautoriseerde headers voor directe API-calls. */
export function authHeaders(auth: LoginResult): Record<string, string> {
  return {
    Authorization: `Bearer ${auth.token}`,
    "Content-Type": "application/json",
  };
}

/**
 * Maakt een verse lessenreeks aan voor deze testrun.
 *
 * Bewust géén hergebruik van de geseede reeks: die heeft een MaxRegistrations,
 * en herhaalde runs vulden hem op tot "volzet". Een eigen reeks per run maakt
 * de suite idempotent en onafhankelijk van de seed-volgorde.
 */
export async function createTestSeries(
  request: APIRequestContext,
  auth: LoginResult,
  namePrefix = "E2E Reeks",
): Promise<{ id: string; price: number }> {
  const clubs = await request.get(`${API_URL}/tennisclubs`, {
    headers: authHeaders(auth),
  });
  expect(clubs.ok(), "Kon tennisclubs niet ophalen").toBeTruthy();
  const clubList = (await clubs.json()) as { id: string }[];
  expect(clubList.length, "Geen tennisclub in de seed").toBeGreaterThan(0);

  const price = 120;
  const response = await request.post(`${API_URL}/lessonseries`, {
    headers: authHeaders(auth),
    data: {
      name: `${namePrefix} ${RUN_ID}`,
      description: "Aangemaakt door de live E2E-suite",
      level: 1,
      price,
      startDate: futureDate(1),
      endDate: futureDate(120),
      registrationDeadline: new Date(
        Date.now() + 90 * 24 * 60 * 60 * 1000,
      ).toISOString(),
      // Ruim genoeg zodat de suite niet tegen "volzet" aanloopt.
      maxRegistrations: 500,
      tennisClubId: clubList[0].id,
      weeklyTemplate: [],
      // Minstens één les is verplicht volgens de validator. Baannaam uniek per
      // run, anders botst een tweede run op de baan-bezettingscheck.
      lessons: [
        {
          date: futureDate(7),
          startTime: "18:00",
          endTime: "19:00",
          courtName: uniqueCourt(namePrefix),
          maxStudents: 4,
        },
      ],
    },
  });
  expect(
    response.ok(),
    `Aanmaken testreeks faalde (${response.status()}): ${await response.text()}`,
  ).toBeTruthy();

  const id = (await response.json()) as string;
  return { id, price };
}

/** Haalt de eerste lessenreeks op uit de geseede data. */
export async function firstSeries(
  request: APIRequestContext,
  auth: LoginResult,
): Promise<{ id: string; name: string; price: number }> {
  const response = await request.get(`${API_URL}/lessonseries`, {
    headers: authHeaders(auth),
  });
  expect(response.ok()).toBeTruthy();
  const series = (await response.json()) as {
    id: string;
    name: string;
    price: number;
  }[];
  expect(
    series.length,
    "Geen lessenreeksen gevonden — is seed-demo-data.sh gedraaid?",
  ).toBeGreaterThan(0);
  return series[0];
}

/** Datum over N dagen als yyyy-MM-dd, voor lesmomenten in de toekomst. */
export function futureDate(daysFromNow: number): string {
  const d = new Date();
  d.setDate(d.getDate() + daysFromNow);
  return d.toISOString().slice(0, 10);
}

/**
 * Uniek achtervoegsel per testrun. Nodig omdat de suite tegen een persistente
 * database draait: zonder unieke baannamen botst een tweede run op de
 * baan-bezettingscheck die de eerste run zelf heeft gevuld.
 */
export const RUN_ID = `${Date.now().toString(36)}`;

/** Unieke baannaam voor deze run. */
export function uniqueCourt(label: string): string {
  return `E2E ${label} ${RUN_ID}`;
}
