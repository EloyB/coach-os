import apiClient from "@/lib/api-client";

// ─── Response types ──────────────────────────────────────────────────────────

export interface PlanningTimeSlotDto {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  courtName: string | null;
  trainerId: string | null;
  trainerName: string | null;
  maxCapacity: number;
}

export interface PlanningEnrollmentDto {
  id: string;
  studentName: string;
  studentEmail: string;
  studentPhone: string | null;
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
  isAutoMerged: boolean;
  isLocked: boolean;
}

export interface PlanningOverviewDto {
  planningStatus: string; // "Enrollment" | "Planning" | "Scheduled"
  planningLastEditedAt: string | null;
  timeSlots: PlanningTimeSlotDto[];
  enrollments: PlanningEnrollmentDto[];
  groups: PlanningGroupDto[];
  assignments: PlanningAssignmentDto[];
}

export interface TrainerPlanningEnrollmentDto {
  id: string;
  studentName: string;
  isOpenToGrouping: boolean;
  groupId: string | null;
}

export interface TrainerPlanningDto {
  lessonSerieId: string;
  lessonSerieName: string;
  planningStatus: string;
  planningLastEditedAt: string | null;
  timeSlots: PlanningTimeSlotDto[];
  enrollments: TrainerPlanningEnrollmentDto[];
  groups: PlanningGroupDto[];
  assignments: PlanningAssignmentDto[];
}

// ─── Request types ───────────────────────────────────────────────────────────

export interface UpdateAssignmentRequest {
  weeklyTemplateEntryId: string;
}

export interface CreateAssignmentRequest {
  enrollmentId?: string;
  groupId?: string;
  weeklyTemplateEntryId: string;
}

export interface CreateGroupRequest {
  enrollmentIds: string[];
}

// ─── API calls ───────────────────────────────────────────────────────────────

export async function getTrainerPlanning(): Promise<TrainerPlanningDto[]> {
  const { data } = await apiClient.get<TrainerPlanningDto[]>("/trainer/planning");
  return data;
}

export async function getPlanningOverview(
  seriesId: string
): Promise<PlanningOverviewDto> {
  const { data } = await apiClient.get<PlanningOverviewDto>(
    `/lessonseries/${seriesId}/planning`
  );
  return data;
}

export async function generatePlanning(
  seriesId: string,
  force = false
): Promise<void> {
  await apiClient.post(
    `/lessonseries/${seriesId}/planning/generate${force ? "?force=true" : ""}`
  );
}

export async function deleteAssignment(
  seriesId: string,
  assignmentId: string
): Promise<void> {
  await apiClient.delete(
    `/lessonseries/${seriesId}/planning/assignments/${assignmentId}`
  );
}

export async function createAssignment(
  seriesId: string,
  request: CreateAssignmentRequest
): Promise<void> {
  await apiClient.post(
    `/lessonseries/${seriesId}/planning/assignments`,
    request
  );
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

export async function lockAssignment(
  seriesId: string,
  assignmentId: string
): Promise<PlanningAssignmentDto> {
  const { data } = await apiClient.post<PlanningAssignmentDto>(
    `/lessonseries/${seriesId}/planning/assignments/${assignmentId}/lock`
  );
  return data;
}

export async function unlockAssignment(
  seriesId: string,
  assignmentId: string
): Promise<PlanningAssignmentDto> {
  const { data } = await apiClient.delete<PlanningAssignmentDto>(
    `/lessonseries/${seriesId}/planning/assignments/${assignmentId}/lock`
  );
  return data;
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

export async function resendConfirmation(
  seriesId: string,
  assignmentId: string
): Promise<void> {
  await apiClient.post(
    `/lessonseries/${seriesId}/planning/assignments/${assignmentId}/resend-confirmation`
  );
}

export async function sendAssignmentConfirmation(
  seriesId: string,
  assignmentId: string
): Promise<void> {
  await apiClient.post(
    `/lessonseries/${seriesId}/planning/assignments/${assignmentId}/send-confirmation`
  );
}

export async function adminConfirm(
  seriesId: string,
  assignmentId: string
): Promise<void> {
  await apiClient.post(
    `/lessonseries/${seriesId}/planning/assignments/${assignmentId}/admin-confirm`
  );
}

export async function confirmPlanning(seriesId: string): Promise<void> {
  await apiClient.post(`/lessonseries/${seriesId}/planning/confirm`);
}
