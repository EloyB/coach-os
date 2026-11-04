import apiClient from "@/lib/api-client";
import type { BillingProfile, SubscriptionPayment, SubscriptionStatusName } from "@/lib/api/billing";

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
  subscriptionStatus: SubscriptionStatusName;
  /** Laatste dag van de proef (yyyy-MM-dd); null buiten een proef. */
  trialEndsAt: string | null;
  /** Game/Set/Match; null zolang er geen plan is. */
  subscriptionPlan: string | null;
}

export interface SubscriptionDto {
  organizationId: string;
  status: SubscriptionStatusName;
  plan: string | null;
  interval: "Monthly" | "Yearly" | null;
  paymentMethod: "Mollie" | "BankTransfer" | null;
  studentLimit: number | null;
  price: number;
  priceInclVat: number;
  startDate: string;
  trialEndsAt: string | null;
  currentPeriodStart: string | null;
  currentPeriodEnd: string | null;
  cancelAt: string | null;
  isDeal: boolean;
  mollieCustomerId: string | null;
  mollieSubscriptionId: string | null;
  /** false = fake-Mollie (geen echte betalingen). */
  billingConfigured: boolean;
  /** false = Billit nog niet gekoppeld (fake-facturen). */
  invoicingConfigured: boolean;
  profile: BillingProfile | null;
  payments: SubscriptionPayment[];
}

export interface SubscriptionPlanOption {
  plan: number;
  name: string;
  monthlyPrice: number;
  yearlyPrice: number;
  studentLimit: number;
}

/** Deal: plan, prijs excl. btw (mag 0) en einddatum (yyyy-MM-dd); verlengt niet automatisch. */
export interface AssignDealRequest {
  plan: number;
  price: number;
  endDate: string;
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

export async function getSubscription(organizationId: string): Promise<SubscriptionDto | null> {
  const { data } = await apiClient.get<SubscriptionDto | null>(
    `/super-admin/organizations/${organizationId}/subscription`
  );
  return data || null;
}

function subscriptionUrl(organizationId: string, action: string): string {
  return `/super-admin/organizations/${organizationId}/subscription/${action}`;
}

export async function assignDeal(organizationId: string, request: AssignDealRequest): Promise<SubscriptionDto> {
  const { data } = await apiClient.post<SubscriptionDto>(subscriptionUrl(organizationId, "deal"), request);
  return data;
}

export async function markSubscriptionPaymentPaid(organizationId: string, paymentId: string): Promise<SubscriptionDto> {
  const { data } = await apiClient.post<SubscriptionDto>(
    subscriptionUrl(organizationId, `payments/${paymentId}/mark-paid`)
  );
  return data;
}

export async function cancelOrganizationSubscription(organizationId: string): Promise<SubscriptionDto> {
  const { data } = await apiClient.post<SubscriptionDto>(subscriptionUrl(organizationId, "cancel"));
  return data;
}

export async function sendPaymentInvitation(organizationId: string): Promise<void> {
  await apiClient.post(subscriptionUrl(organizationId, "invite"));
}

export async function extendOrganizationTrial(
  organizationId: string,
  days: number
): Promise<void> {
  await apiClient.post(`/super-admin/organizations/${organizationId}/extend-trial`, {
    days,
  });
}
