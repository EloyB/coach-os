import apiClient from '@/lib/api-client';

export interface AuthResponse {
  token: string;
  expiresAt: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  organizationId: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  organizationName: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export async function login(request: LoginRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/login', request);
  return data;
}

export async function register(request: RegisterRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/register', request);
  return data;
}
