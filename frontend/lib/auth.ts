const TOKEN_KEY = 'token';
const USER_KEY = 'auth_user';
const AUTH_COOKIE = 'has_token';
const ROLE_COOKIE = 'user_role';
const SUPER_ADMIN_COOKIE = 'has_super_admin';

export interface OrganizationMembershipInfo {
  organizationId: string;
  organizationName: string;
  role: string;
  isActive: boolean;
}

export interface AuthUser {
  userId?: string;
  email: string;
  firstName?: string;
  lastName?: string;
  organizationId?: string | null;
  role: string;
  memberships?: OrganizationMembershipInfo[];
  isSuperAdmin?: boolean;
  /** Hoofdtrainer in de actieve org: read-only toegang tot inschrijvingen + planning. */
  isHeadTrainer?: boolean;
}

export function isStudent(): boolean {
  return getAuthUser()?.role === "Student";
}

/**
 * Hoofdtrainer-viewer: een trainer met read-only rechten op inschrijvingen + planning.
 * De frontend gebruikt dit om bewerk-/actieknoppen te verbergen (de backend blijft
 * de echte poortwachter).
 */
export function isHeadTrainerViewer(): boolean {
  const u = getAuthUser();
  return u?.role === "Trainer" && u?.isHeadTrainer === true;
}

export function isSuperAdmin(): boolean {
  return getAuthUser()?.isSuperAdmin === true;
}

export function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  if (typeof window === 'undefined') return;
  localStorage.setItem(TOKEN_KEY, token);
  document.cookie = `${AUTH_COOKIE}=1; path=/; SameSite=Lax`;
}

export function setSuperAdminCookie(value: boolean): void {
  if (typeof window === 'undefined') return;
  if (value) {
    document.cookie = `${SUPER_ADMIN_COOKIE}=1; path=/; SameSite=Lax`;
  } else {
    document.cookie = `${SUPER_ADMIN_COOKIE}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
  }
}

export function removeToken(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(TOKEN_KEY);
  document.cookie = `${AUTH_COOKIE}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
}

export function getAuthUser(): AuthUser | null {
  if (typeof window === 'undefined') return null;
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function setAuthUser(user: AuthUser): void {
  if (typeof window === 'undefined') return;
  localStorage.setItem(USER_KEY, JSON.stringify(user));
  // Spiegel de rol in een cookie zodat de Next-middleware role-based redirects
  // kan doen zonder localStorage te kunnen lezen. NIET security-kritisch — de
  // backend blijft de uiteindelijke autoriteit (RequireRole/RequireAuthorization).
  document.cookie = `${ROLE_COOKIE}=${encodeURIComponent(user.role)}; path=/; SameSite=Lax`;
}

export function removeAuthUser(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(USER_KEY);
  document.cookie = `${ROLE_COOKIE}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
}

export function clearAuth(): void {
  removeToken();
  removeAuthUser();
  document.cookie = `${AUTH_COOKIE}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
  document.cookie = `${ROLE_COOKIE}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
  document.cookie = `${SUPER_ADMIN_COOKIE}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
}

export function isAuthenticated(): boolean {
  return !!getToken();
}
