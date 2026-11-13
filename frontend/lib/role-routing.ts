/**
 * Rolgebaseerde routing: welke startpagina hoort bij een rol en welke paden horen bij
 * de student- of de staffkant. Gedeeld door de Next-middleware (rol uit cookie) en de
 * layouts (rol uit localStorage).
 *
 * Dit is enkel UX-routing, geen beveiliging: de backend blijft de echte poortwachter
 * (RequireRole/RequireAuthorization). Een verouderde of vervalste rolcookie levert
 * hoogstens een verkeerde redirect op, nooit toegang tot data.
 */

export const STUDENT_ROLE = "Student";
export const STUDENT_HOME = "/student/lessons";
export const STAFF_HOME = "/dashboard";
export const SUPER_ADMIN_HOME = "/super-admin/dashboard";

/** Paden die enkel voor staff (admin, trainer, hoofdtrainer, super-admin) bedoeld zijn. */
const STAFF_AREAS = ["/dashboard", "/super-admin", "/verify-email"];
/** Paden die enkel voor studenten bedoeld zijn. `/student-login` hoort hier bewust niet bij. */
const STUDENT_AREAS = ["/student"];

function isInArea(pathname: string, areas: readonly string[]): boolean {
  return areas.some((area) => pathname === area || pathname.startsWith(`${area}/`));
}

export function isStudentRole(role: string | null | undefined): boolean {
  return role === STUDENT_ROLE;
}

/**
 * Startpagina van een ingelogde gebruiker. De rol gaat voor op de super-admin-vlag:
 * een student met een achtergebleven super-admin-cookie hoort nog altijd bij zijn lessen.
 */
export function homePathFor(role: string | null | undefined, isSuperAdmin: boolean): string {
  if (isStudentRole(role)) return STUDENT_HOME;
  if (isSuperAdmin) return SUPER_ADMIN_HOME;
  return STAFF_HOME;
}

/**
 * Doorverwijzing voor een ingelogde gebruiker die op de pagina van de andere rol belandt,
 * of `null` als het pad bij de rol past. Zonder bekende rol beslissen we niets: dan valt
 * de controle terug op de layouts, die de rol uit localStorage lezen.
 */
export function roleRedirectFor(
  pathname: string,
  role: string | null | undefined,
  isSuperAdmin: boolean,
): string | null {
  if (!role) return null;
  if (isStudentRole(role)) {
    return isInArea(pathname, STAFF_AREAS) ? STUDENT_HOME : null;
  }
  return isInArea(pathname, STUDENT_AREAS) ? homePathFor(role, isSuperAdmin) : null;
}
