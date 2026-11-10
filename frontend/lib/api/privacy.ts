import apiClient from "@/lib/api-client";
import type { SubscriptionStatusName } from "@/lib/api/billing";

export const CLUB_EXPORT_FILENAME = "coachos-club-export.zip";

export type OffboardingBlockerCode = "subscription_running" | "retention_period" | "financial_records";

export interface OffboardingBlocker {
  code: OffboardingBlockerCode;
  message: string;
  date: string | null;
}

export interface OrganizationDataCounts {
  tennisClubs: number;
  lessonSeries: number;
  lessons: number;
  enrollments: number;
  camps: number;
  campEnrollments: number;
  payments: number;
  memberships: number;
  accountsToDelete: number;
  subscriptionPayments: number;
}

export interface OrganizationOffboardingPreview {
  organizationId: string;
  organizationName: string;
  subscriptionStatus: SubscriptionStatusName;
  endedOn: string | null;
  deletableFrom: string | null;
  eligible: boolean;
  blockers: OffboardingBlocker[];
  counts: OrganizationDataCounts;
}

export interface OrganizationDeleted {
  organizationId: string;
  deletedAccounts: number;
  erasedAtUtc: string;
}

/** ZIP met alle gegevens van de eigen club (enkel clubbeheerders). */
export async function downloadClubExport(): Promise<Blob> {
  const { data } = await apiClient.get<Blob>("/privacy/organization/export", { responseType: "blob" });
  return data;
}

export async function getOrganizationOffboarding(organizationId: string): Promise<OrganizationOffboardingPreview> {
  const { data } = await apiClient.get<OrganizationOffboardingPreview>(
    `/super-admin/organizations/${encodeURIComponent(organizationId)}/offboarding`,
  );
  return data;
}

export async function deleteOrganization(organizationId: string, confirmationName: string): Promise<OrganizationDeleted> {
  const { data } = await apiClient.post<OrganizationDeleted>(
    `/super-admin/organizations/${encodeURIComponent(organizationId)}/offboarding/delete`,
    { confirmationName },
  );
  return data;
}
