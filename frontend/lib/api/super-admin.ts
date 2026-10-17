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
  subscriptionStatus: "Legacy" | "Trialing" | "Active";
  /** Laatste dag van de proef (yyyy-MM-dd); null buiten een proef. */
  trialEndsAt: string | null;
  /** Game/Set/Match; null zolang er geen plan is. */
  subscriptionPlan: string | null;
}

export interface SubscriptionDto {
  organizationId: string;
  plan: string;
  interval: string;
  studentLimit: number;
  price: number;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
  mollieCustomerId: string | null;
  lastPaymentId: string | null;
  lastCheckoutUrl: string | null;
  lastPaymentStatus: string | null;
  lastPaidAt: string | null;
  billingConfigured: boolean;
}

export interface SubscriptionPlanOption {
  plan: number;
  name: string;
  monthlyPrice: number;
  yearlyPrice: number;
  studentLimit: number;
}

export interface AssignSubscriptionRequest {
  plan: number;
  interval: number;
  startDate: string | null;
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

export async function getSubscriptionPlans(): Promise<SubscriptionPlanOption[]> {
  const { data } = await apiClient.get<SubscriptionPlanOption[]>(
    "/super-admin/subscription-plans"
  );
  return data;
}

export async function getSubscription(
  organizationId: string
): Promise<SubscriptionDto | null> {
  const { data } = await apiClient.get<SubscriptionDto | null>(
    `/super-admin/organizations/${organizationId}/subscription`
  );
  return data || null;
}

export async function assignSubscription(
  organizationId: string,
  request: AssignSubscriptionRequest
): Promise<SubscriptionDto> {
  const { data } = await apiClient.put<SubscriptionDto>(
    `/super-admin/organizations/${organizationId}/subscription`,
    request
  );
  return data;
}

export async function setSubscriptionActive(
  organizationId: string,
  isActive: boolean
): Promise<SubscriptionDto> {
  const { data } = await apiClient.put<SubscriptionDto>(
    `/super-admin/organizations/${organizationId}/subscription/active`,
    { isActive }
  );
  return data;
}

export async function createSubscriptionPaymentLink(
  organizationId: string
): Promise<SubscriptionDto> {
  const { data } = await apiClient.post<SubscriptionDto>(
    `/super-admin/organizations/${organizationId}/subscription/payment-link`
  );
  return data;
}

export async function refreshSubscriptionPayment(
  organizationId: string
): Promise<SubscriptionDto> {
  const { data } = await apiClient.post<SubscriptionDto>(
    `/super-admin/organizations/${organizationId}/subscription/refresh`
  );
  return data;
}

export async function markSubscriptionPaid(
  organizationId: string
): Promise<SubscriptionDto> {
  const { data } = await apiClient.post<SubscriptionDto>(
    `/super-admin/organizations/${organizationId}/subscription/mark-paid`
  );
  return data;
}

export async function extendOrganizationTrial(
  organizationId: string,
  days: number
): Promise<void> {
  await apiClient.post(`/super-admin/organizations/${organizationId}/extend-trial`, {
    days,
  });
}
