import { NextRequest, NextResponse } from "next/server";

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

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const hasToken = request.cookies.has("has_token");
  const hasSuperAdmin = request.cookies.has("has_super_admin");

  // Super-admin sectie: enkel toegankelijk met super-admin cookie.
  if (pathname.startsWith("/super-admin")) {
    if (!hasToken) {
      const loginUrl = new URL("/login", request.url);
      loginUrl.searchParams.set("redirect", pathname);
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
    loginUrl.searchParams.set("redirect", pathname);
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

  if (pathname.startsWith("/student/") && !hasToken) {
    const loginUrl = new URL("/student-login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  if (isPublicRoute(pathname) && hasToken) {
    if (pathname === "/student-login") {
      return NextResponse.redirect(new URL("/student/lessons", request.url));
    }
    if (pathname.startsWith("/auth/magic")) {
      return NextResponse.next();
    }
    if (pathname.startsWith("/invite/")) {
      // Een accept-invite link mag altijd doorgaan — anders kan een super-admin
      // die toevallig nog ingelogd is geen nieuwe admin meer accepteren in dezelfde browser.
      return NextResponse.next();
    }
    if (hasSuperAdmin) {
      return NextResponse.redirect(new URL("/super-admin/dashboard", request.url));
    }
    return NextResponse.redirect(new URL("/dashboard", request.url));
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
    "/invite/:path*",
    "/student-login",
    "/auth/magic",
  ],
};
