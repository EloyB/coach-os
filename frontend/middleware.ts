import { NextRequest, NextResponse } from "next/server";
import { homePathFor, roleRedirectFor, STUDENT_HOME } from "@/lib/role-routing";

const PUBLIC_ROUTES = [
  "/login",
  "/register",
  "/invite",
  "/enroll",
  "/student-login",
  "/auth/magic",
];

// Dashboard-paden die alleen voor Admin toegankelijk zijn. De sidebar verbergt
// deze al via `adminOnly`, maar deze redirect dekt directe URL-toegang. De backend
// blijft de uiteindelijke autoriteit (RequireRole("Admin")) — dit is UX, geen security.
const ADMIN_ONLY_DASHBOARD_PATHS = ["/dashboard/settings", "/dashboard/trainers"];

function isPublicRoute(pathname: string): boolean {
  return PUBLIC_ROUTES.some(
    (route) => pathname === route || pathname.startsWith(`${route}/`)
  );
}

function isAdminOnlyDashboardPath(pathname: string): boolean {
  return ADMIN_ONLY_DASHBOARD_PATHS.some(
    (route) => pathname === route || pathname.startsWith(`${route}/`)
  );
}

/** Rol uit de `user_role`-cookie (URI-gecodeerd door `setAuthUser`); `null` als onbekend of onleesbaar. */
function readRole(request: NextRequest): string | null {
  const raw = request.cookies.get("user_role")?.value;
  if (!raw) return null;
  try {
    return decodeURIComponent(raw);
  } catch {
    return null;
  }
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const hasToken = request.cookies.has("has_token");
  const hasSuperAdmin = request.cookies.has("has_super_admin");
  // Enkel UX-routing: de API blijft de echte toegangscontrole. Zonder sessie negeren we de
  // rolcookie, zodat een achtergebleven cookie de loginpagina's nooit kan blokkeren.
  const role = hasToken ? readRole(request) : null;

  // Verkeerde kant voor deze rol (staff op /student/*, student op staffpagina's): naar de
  // eigen startpagina, zonder uitloggen.
  const roleRedirect = roleRedirectFor(pathname, role, hasSuperAdmin);
  if (roleRedirect) {
    return NextResponse.redirect(new URL(roleRedirect, request.url));
  }

  // Super-admin sectie: enkel toegankelijk met super-admin cookie.
  if (pathname.startsWith("/super-admin")) {
    if (!hasToken) {
      const loginUrl = new URL("/login", request.url);
      loginUrl.searchParams.set("redirect", `${pathname}${request.nextUrl.search}`);
      return NextResponse.redirect(loginUrl);
    }
    if (!hasSuperAdmin) {
      // Ingelogd maar geen super admin → naar gewoon dashboard.
      return NextResponse.redirect(new URL("/dashboard", request.url));
    }
    return NextResponse.next();
  }

  if (pathname.startsWith("/dashboard") && !hasToken) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", `${pathname}${request.nextUrl.search}`);
    return NextResponse.redirect(loginUrl);
  }

  // Super admin die per ongeluk op /dashboard belandt → terug naar super-admin panel.
  if (pathname.startsWith("/dashboard") && hasSuperAdmin) {
    return NextResponse.redirect(new URL("/super-admin/dashboard", request.url));
  }

  // Admin-only dashboard-paden: niet-admins worden teruggestuurd naar /dashboard.
  if (isAdminOnlyDashboardPath(pathname) && hasToken) {
    const role = request.cookies.get("user_role")?.value;
    if (role && role !== "Admin") {
      return NextResponse.redirect(new URL("/dashboard", request.url));
    }
  }

  // E-mailverificatie hoort bij een bestaand account: zonder sessie valt er niets te bevestigen.
  if (pathname.startsWith("/verify-email") && !hasToken) {
    return NextResponse.redirect(new URL("/login", request.url));
  }

  if (pathname.startsWith("/student/") && !hasToken) {
    const loginUrl = new URL("/student-login", request.url);
    loginUrl.searchParams.set("redirect", `${pathname}${request.nextUrl.search}`);
    return NextResponse.redirect(loginUrl);
  }

  if (isPublicRoute(pathname) && hasToken) {
    if (pathname.startsWith("/auth/magic")) {
      return NextResponse.next();
    }
    if (pathname.startsWith("/invite/")) {
      // Een accept-invite link mag altijd doorgaan — anders kan een super-admin
      // die toevallig nog ingelogd is geen nieuwe admin meer accepteren in dezelfde browser.
      return NextResponse.next();
    }
    // Zonder rolcookie (oudere sessie) blijft /student-login naar de studentkant gaan, zoals voorheen.
    const home =
      !role && pathname === "/student-login" ? STUDENT_HOME : homePathFor(role, hasSuperAdmin);
    return NextResponse.redirect(new URL(home, request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/dashboard/:path*",
    "/super-admin/:path*",
    "/student/:path*",
    "/login",
    "/register",
    "/verify-email",
    "/invite/:path*",
    "/student-login",
    "/auth/magic",
  ],
};
