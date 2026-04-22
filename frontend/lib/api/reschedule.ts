import apiClient from "@/lib/api-client";

export interface CreateRescheduleRequest {
  reason?: string;
}

export interface RescheduleRequestDto {
  id: string;
  assignmentId: string;
  studentName: string;
  seriesName: string;
  currentSlot: string;
  reason: string;
  status: string;
  createdAt: string;
}

export interface ResolveRescheduleRequest {
  status: string;
  newSlotId?: string;
}

export async function createRescheduleRequest(
  assignmentId: string,
  request: CreateRescheduleRequest
): Promise<void> {
  await apiClient.post(`/student/lessons/${assignmentId}/reschedule`, request);
}

export async function getRescheduleRequests(): Promise<RescheduleRequestDto[]> {
  const { data } = await apiClient.get<RescheduleRequestDto[]>("/reschedule-requests");
  return data;
}

export async function resolveRescheduleRequest(
  id: string,
  request: ResolveRescheduleRequest
): Promise<void> {
  await apiClient.patch(`/reschedule-requests/${id}`, request);
}
