import apiClient from "@/lib/api-client";

export interface CampDayTrainerDto {
  trainerId: string;
  trainerName: string;
  startTime: string; // "HH:mm"
  endTime: string;
}

export interface CampDayDto {
  id: string;
  date: string; // "yyyy-MM-dd"
  startTime: string;
  endTime: string;
  trainers: CampDayTrainerDto[];
}

export interface CampDto {
  id: string;
  name: string;
  tennisClubId: string;
  tennisClubName: string;
  level: number | null;
  price: number;
  startDate: string;
  endDate: string;
  maxParticipants: number | null;
  participantCount: number;
  dayCount: number;
  isActive: boolean;
}

export interface CampDetailDto {
  id: string;
  name: string;
  description: string | null;
  tennisClubId: string;
  tennisClubName: string;
  level: number | null;
  price: number;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  maxParticipants: number | null;
  participantCount: number;
  isActive: boolean;
  days: CampDayDto[];
}

export interface PublicCampDto {
  id: string;
  name: string;
  description: string | null;
  level: number | null;
  price: number;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  tennisClubName: string;
  maxParticipants: number | null;
  participantCount: number;
  days: CampDayDto[];
}

export interface CreateCampDayTrainerRequest {
  trainerId: string;
  startTime: string;
  endTime: string;
}

export interface CreateCampDayRequest {
  date: string;
  startTime: string;
  endTime: string;
  trainers: CreateCampDayTrainerRequest[];
}

export interface CreateCampRequest {
  name: string;
  description?: string;
  tennisClubId: string;
  level?: number | null;
  price: number;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  maxParticipants?: number | null;
  days: CreateCampDayRequest[];
}

export type UpdateCampRequest = CreateCampRequest & { isActive: boolean };

export interface CampFormFieldDto {
  id: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options: string[] | null;
}

export interface CampEnrollmentFormDto {
  id: string;
  campId: string;
  fields: CampFormFieldDto[];
}

export interface SaveCampFormFieldRequest {
  id?: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options?: string[];
}

export interface CampGroupMemberRequest {
  participantName: string;
  participantEmail: string;
  participantPhone?: string;
  responses: { campFormFieldId: string; value: string }[];
}

export interface SubmitCampEnrollmentRequest {
  participantName: string;
  participantEmail: string;
  participantPhone?: string;
  responses: { campFormFieldId: string; value: string }[];
  enrollmentType?: string; // "solo" | "group"
  groupMembers?: CampGroupMemberRequest[];
}

export interface SubmitCampEnrollmentResult {
  campEnrollmentId: string;
  checkoutUrl: string | null;
}

export interface CampEnrollmentDto {
  id: string;
  participantName: string;
  participantEmail: string;
  participantPhone: string | null;
  status: string;
  enrolledAt: string;
  groupName: string | null;
  formResponses: { fieldLabel: string; value: string }[];
}

// ── Admin ──
export async function getCamps(): Promise<CampDto[]> {
  const { data } = await apiClient.get<CampDto[]>("/camps");
  return data;
}
export async function getCampById(id: string): Promise<CampDetailDto> {
  const { data } = await apiClient.get<CampDetailDto>(`/camps/${id}`);
  return data;
}
export async function createCamp(request: CreateCampRequest): Promise<string> {
  const { data } = await apiClient.post<string>("/camps", request);
  return data;
}
export async function updateCamp(id: string, request: UpdateCampRequest): Promise<void> {
  await apiClient.put(`/camps/${id}`, request);
}
export async function deleteCamp(id: string): Promise<void> {
  await apiClient.delete(`/camps/${id}`);
}
export async function getCampForm(campId: string): Promise<CampEnrollmentFormDto | null> {
  const { data } = await apiClient.get<CampEnrollmentFormDto | null>(`/public/camps/${campId}/form`);
  return data;
}
export async function saveCampForm(campId: string, fields: SaveCampFormFieldRequest[]): Promise<string> {
  const { data } = await apiClient.put<string>(`/camps/${campId}/form`, { fields });
  return data;
}
export async function getCampEnrollments(campId: string): Promise<CampEnrollmentDto[]> {
  const { data } = await apiClient.get<CampEnrollmentDto[]>(`/camps/${campId}/enrollments`);
  return data;
}

// ── Public ──
export async function getPublicCamp(id: string): Promise<PublicCampDto> {
  const { data } = await apiClient.get<PublicCampDto>(`/public/camps/${id}`);
  return data;
}
export async function submitCampEnrollment(
  campId: string, request: SubmitCampEnrollmentRequest
): Promise<SubmitCampEnrollmentResult> {
  const { data } = await apiClient.post<SubmitCampEnrollmentResult>(`/public/camps/${campId}/enroll`, request);
  return data;
}
