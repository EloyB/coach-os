import { useSyncExternalStore } from "react";
import { getAuthUserRaw, parseAuthUser, type AuthUser } from "../auth";

const subscribeNever = () => () => {};

/**
 * Bouwt een snapshot-functie die enkel opnieuw parset als de ruwe waarde wijzigt.
 * `useSyncExternalStore` eist een stabiele referentie zolang de bron niet verandert,
 * anders rendert React eindeloos opnieuw.
 */
export function createAuthUserSnapshot(
  readRaw: () => string | null,
): () => AuthUser | null {
  let lastRaw: string | null | undefined;
  let lastUser: AuthUser | null = null;
  return () => {
    const raw = readRaw();
    if (raw !== lastRaw) {
      lastRaw = raw;
      lastUser = parseAuthUser(raw);
    }
    return lastUser;
  };
}

const getAuthUserSnapshot = createAuthUserSnapshot(getAuthUserRaw);
const getServerSnapshot = () => undefined;

/**
 * De ingelogde gebruiker uit localStorage, zonder hydration-mismatch.
 * - `undefined`: nog onbekend (server-render en hydration)
 * - `null`: niet ingelogd
 * - `AuthUser`: ingelogd
 */
export function useAuthUser(): AuthUser | null | undefined {
  return useSyncExternalStore<AuthUser | null | undefined>(
    subscribeNever,
    getAuthUserSnapshot,
    getServerSnapshot,
  );
}
