import apiClient from "@/lib/api-client";

export interface AdminOrganizationDto {
  organizationId: string;
  organizationName: string;
  isEarlyBird: boolean;
  membershipActive: boolean;
}

export interface AdminListItemDto {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  invitePending: boolean;
  createdAt: string;
  organizations: AdminOrganizationDto[];
}

export interface OrganizationListItemDto {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
  isEarlyBird: boolean;
  adminCount: number;
  createdAt: string;
}

export interface CreateAdminRequest {
  organizationName: string;
  firstName: string;
  lastName: string;
  email: string;
  isEarlyBird: boolean;
}

export async function listAdmins(): Promise<AdminListItemDto[]> {
  const { data } = await apiClient.get<AdminListItemDto[]>("/super-admin/admins");
  return data;
}

export async function createAdmin(request: CreateAdminRequest): Promise<{ userId: string }> {
  const { data } = await apiClient.post<{ userId: string }>("/super-admin/admins", request);
  return data;
}

export async function disableAdmin(userId: string): Promise<void> {
  await apiClient.post(`/super-admin/admins/${userId}/disable`);
}

export async function enableAdmin(userId: string): Promise<void> {
  await apiClient.post(`/super-admin/admins/${userId}/enable`);
}

export async function resendAdminInvite(userId: string): Promise<void> {
  await apiClient.post(`/super-admin/admins/${userId}/resend-invite`);
}

export async function listOrganizations(): Promise<OrganizationListItemDto[]> {
  const { data } = await apiClient.get<OrganizationListItemDto[]>("/super-admin/organizations");
  return data;
}

export async function setOrganizationEarlyBird(
  organizationId: string,
  isEarlyBird: boolean
): Promise<void> {
  await apiClient.put(`/super-admin/organizations/${organizationId}/early-bird`, {
    isEarlyBird,
  });
}
