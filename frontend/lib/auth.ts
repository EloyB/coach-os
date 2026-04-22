const TOKEN_KEY = 'token';
const USER_KEY = 'auth_user';
const AUTH_COOKIE = 'has_token';

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
}

export function isStudent(): boolean {
  return getAuthUser()?.role === "Student";
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
}

export function removeAuthUser(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(USER_KEY);
}

export function clearAuth(): void {
  removeToken();
  removeAuthUser();
  document.cookie = `${AUTH_COOKIE}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT`;
}

export function isAuthenticated(): boolean {
  return !!getToken();
}
