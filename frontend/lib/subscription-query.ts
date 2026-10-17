/**
 * React Query-key voor de abonnementsstatus van de actieve organisatie. De API leest de
 * organisatie uit het token, dus de key moet ze bevatten: anders toont de cache na inloggen
 * of registreren in dezelfde tab (zonder volledige reload) de status van de vorige organisatie.
 */
export function subscriptionStatusQueryKey(organizationId: string | null | undefined) {
  return ["subscription", "status", organizationId ?? null] as const;
}
