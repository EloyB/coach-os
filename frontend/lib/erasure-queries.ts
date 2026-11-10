/**
 * Na het verwijderen van een speler mag de app geen queries meer ophalen die naar die
 * inschrijving verwijzen: ze geven een 404 en de globale API-interceptor toont die als fout.
 */
export function isAboutErasedEnrollment(queryKey: readonly unknown[], enrollmentId: string): boolean {
  return queryKey[0] === "playerErasurePreview" || queryKey.includes(enrollmentId);
}
