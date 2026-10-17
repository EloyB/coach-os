/** Enkel interne paden ("/…") als doel na het inloggen: geen externe of protocol-relatieve adressen. */
export function safeRedirectPath(value: string | null): string | null {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) return null;
  return value;
}

/** Loginpagina die na het inloggen terugkeert naar het huidige pad, inclusief query (bv. ?return=1). */
export function loginPathFor(pathname: string, search: string): string {
  if (pathname === "/login") return "/login";
  return `/login?redirect=${encodeURIComponent(`${pathname}${search}`)}`;
}
