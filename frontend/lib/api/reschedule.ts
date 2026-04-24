import apiClient from "@/lib/api-client";

export interface CreateRescheduleRequest {
  alternativeWeeklyTemplateEntryId?: string;
  reason: string;
}

export interface RescheduleRequestDto {
  id: string;
  enrollmentId: string;
  studentName: string;
  studentEmail: string;
  scheduleAssignmentId: string;
  currentSlotDay: string;
  currentSlotTime: string;
  alternativeWeeklyTemplateEntryId?: string;
  alternativeSlotDay?: string;
  alternativeSlotTime?: string;
  reason: string;
  status: string;
  createdAt: string;
  resolvedAt?: string;
}

export interface ResolveRescheduleRequest {
  state: string;
  note?: string;
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
