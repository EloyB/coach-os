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

  if (pathname.startsWith("/dashboard") && !hasToken) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

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
    return NextResponse.redirect(new URL("/dashboard", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/dashboard/:path*",
    "/student/:path*",
    "/login",
    "/register",
    "/invite/:path*",
    "/student-login",
    "/auth/magic",
  ],
};
