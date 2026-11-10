import apiClient from "@/lib/api-client";

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
