import apiClient from '@/lib/api-client';

export type ErasureKind = 'enrollments' | 'camp-enrollments';
export interface ErasureRequest {
  early: boolean;
  newGroupLeaderId: string | null;
  apply?: boolean;
  previewHash?: string;
  confirmation?: string;
  verifiedRequest?: boolean;
}
export interface ErasureResult {
  eligible: boolean;
  deleted: boolean;
  previewHash: string | null;
  holds: string[];
  personalRecords: number;
}
export async function processPlayerErasure(kind: ErasureKind, id: string, request: ErasureRequest): Promise<ErasureResult> {
  const { data } = await apiClient.post<ErasureResult>(`/privacy/${kind}/${encodeURIComponent(id)}/erasure`, request);
  return data;
}
