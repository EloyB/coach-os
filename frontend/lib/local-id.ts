/**
 * Genereert een lokaal-uniek id voor client-side gebruik (React keys, draft-rijen).
 *
 * `crypto.randomUUID()` bestaat alleen in een secure context (HTTPS of localhost).
 * Bij het testen via een LAN-IP over http (bv. op een gsm) is dat géén secure
 * context en is `crypto.randomUUID` undefined — dat gooit een runtime error.
 * Daarom valt deze helper terug op een timestamp + random string.
 */
export function localId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `id-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}
