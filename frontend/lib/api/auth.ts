import apiClient from '@/lib/api-client';

export interface AuthResponse {
  token: string;
  expiresAt: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  organizationId: string | null;
  role: string;
}

export interface RegisterStudentRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
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

export async function registerStudent(request: RegisterStudentRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/register-student', request);
  return data;
}

export interface StudentAuthResponse {
  token: string;
  expiresAt: string;
  email: string;
  role: string;
}

export async function requestMagicLink(email: string): Promise<void> {
  await apiClient.post('/auth/magic/request', { email });
}

export async function redeemMagicLink(token: string): Promise<StudentAuthResponse> {
  const { data } = await apiClient.post<StudentAuthResponse>('/auth/magic/redeem', { token });
  return data;
}
