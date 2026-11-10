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

export type ErasureKind = "enrollments" | "camp-enrollments";

export type ErasureGroupEffect = "None" | "LeaveGroup" | "RemoveEmptyGroup" | "TransferLeader";

export interface ErasureGroupMember {
  id: string;
  name: string;
}

export interface PlayerErasurePreview {
  enrollmentId: string;
  participantName: string;
  lastParticipation: string | null;
  retentionPeriodPassed: boolean;
  canErase: boolean;
  blockingReasons: string[];
  formAnswerCount: number;
  groupEffect: ErasureGroupEffect;
  otherGroupMembers: ErasureGroupMember[];
  updatedAt: string;
}

export interface ErasePlayerRequest {
  confirmationName: string;
  newGroupLeaderId: string | null;
  expectedUpdatedAt: string;
}

export interface PlayerErasureResult {
  enrollmentId: string;
  erasedAt: string;
}

export async function getPlayerErasurePreview(
  kind: ErasureKind,
  id: string,
): Promise<PlayerErasurePreview> {
  const { data } = await apiClient.get<PlayerErasurePreview>(
    `/privacy/${kind}/${encodeURIComponent(id)}/erasure-preview`,
  );
  return data;
}

export async function erasePlayer(
  kind: ErasureKind,
  id: string,
  request: ErasePlayerRequest,
): Promise<PlayerErasureResult> {
  const { data } = await apiClient.post<PlayerErasureResult>(
    `/privacy/${kind}/${encodeURIComponent(id)}/erasure`,
    request,
  );
  return data;
}
