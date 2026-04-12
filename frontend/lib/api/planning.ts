import apiClient from "@/lib/api-client";

// ─── Response types ──────────────────────────────────────────────────────────

export interface PlanningTimeSlotDto {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courtName: string | null;
  trainerId: string | null;
  maxCapacity: number;
}

export interface PlanningEnrollmentDto {
  id: string;
  studentName: string;
  studentEmail: string;
  isOpenToGrouping: boolean;
  groupId: string | null;
  /** Map of timeSlotId → "Preferred" | "Available" | "Unavailable" */
  preferences: Record<string, string>;
}

export interface PlanningGroupDto {
  id: string;
  name: string;
  leaderEnrollmentId: string;
  memberEnrollmentIds: string[];
}

export interface PlanningAssignmentDto {
  id: string;
  timeSlotId: string;
  enrollmentId: string | null;
  groupId: string | null;
  status: string; // "Proposed" | "Confirmed"
}

export interface PlanningOverviewDto {
  planningStatus: string; // "Enrollment" | "Planning" | "Scheduled"
  timeSlots: PlanningTimeSlotDto[];
  enrollments: PlanningEnrollmentDto[];
  groups: PlanningGroupDto[];
  assignments: PlanningAssignmentDto[];
}

// ─── Request types ───────────────────────────────────────────────────────────

export interface UpdateAssignmentRequest {
  weeklyTemplateEntryId: string;
}

export interface CreateGroupRequest {
  enrollmentIds: string[];
}

// ─── API calls ───────────────────────────────────────────────────────────────

export async function getPlanningOverview(
  seriesId: string
): Promise<PlanningOverviewDto> {
  const { data } = await apiClient.get<PlanningOverviewDto>(
    `/lessonseries/${seriesId}/planning`
  );
  return data;
}

export async function generatePlanning(seriesId: string): Promise<void> {
  await apiClient.post(`/lessonseries/${seriesId}/planning/generate`);
}

export async function updateAssignment(
  seriesId: string,
  assignmentId: string,
  request: UpdateAssignmentRequest
): Promise<void> {
  await apiClient.put(
    `/lessonseries/${seriesId}/planning/assignments/${assignmentId}`,
    request
  );
}

export async function createGroup(
  seriesId: string,
  request: CreateGroupRequest
): Promise<void> {
  await apiClient.post(
    `/lessonseries/${seriesId}/planning/groups`,
    request
  );
}

export async function deleteGroup(
  seriesId: string,
  groupId: string
): Promise<void> {
  await apiClient.delete(
    `/lessonseries/${seriesId}/planning/groups/${groupId}`
  );
}

export async function confirmPlanning(seriesId: string): Promise<void> {
  await apiClient.post(`/lessonseries/${seriesId}/planning/confirm`);
}
