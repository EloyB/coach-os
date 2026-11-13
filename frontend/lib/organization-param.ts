import type { AuthUser } from "./auth";

/** Query-parameter waarmee een link (bv. uit een mail) een bepaalde organisatie opent. */
export const ORGANIZATION_PARAM = "org";

/**
 * Naar welke organisatie gewisseld moet worden voor een `?org=`-link: enkel als de gebruiker er
 * een actief lidmaatschap heeft en ze niet al de huidige organisatie is. Anders null.
 */
export function organizationToSwitchTo(requested: string | null, user: AuthUser | null): string | null {
  if (!requested || !user) return null;
  if (user.organizationId === requested) return null;
  const member = user.memberships?.some((m) => m.organizationId === requested && m.isActive) ?? false;
  return member ? requested : null;
}
