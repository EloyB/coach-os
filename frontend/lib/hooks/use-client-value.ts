import { useSyncExternalStore } from "react";

const subscribeNever = () => () => {};

/**
 * Leest een waarde die enkel in de browser bestaat (localStorage, clientklok) zonder
 * hydration-mismatch: op de server en tijdens hydration is het resultaat `null`, daarna
 * de echte waarde. `getValue` moet een primitieve (of stabiele) waarde teruggeven.
 */
export function useClientValue<T extends string | number | boolean>(
  getValue: () => T | null,
): T | null {
  return useSyncExternalStore(subscribeNever, getValue, () => null);
}
