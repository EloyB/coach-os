import apiClient from "@/lib/api-client";

export interface LessonDto {
  id: string;
  lessonSeriesId: string;
  date: string;
  startTime: string;
  endTime: string;
  courtName: string;
  maxStudents: number;
  notes?: string;
  isCancelled: boolean;
}

export interface LessonSeriesDto {
  id: string;
  organizationId: string;
  trainerId: string;
  trainerName: string;
  name: string;
  description?: string;
  level: number;
  price: number;
  startDate: string;
  endDate: string;
  durationMinutes: number;
  isActive: boolean;
  tennisClubId: string;
  tennisClubName: string;
  tennisClubAddress: string;
  lessonCount: number;
  createdAt: string;
  lessons: LessonDto[];
}

export interface OrgMemberDto {
  id: string;
  fullName: string;
}

export interface CreateLessonSeriesRequest {
  trainerId: string;
  name: string;
  description?: string;
  level: number;
  price: number;
  startDate: string;
  endDate: string;
  durationMinutes: number;
  tennisClubId: string;
}

export interface UpdateLessonSeriesRequest {
  trainerId: string;
  name: string;
  description?: string;
  level: number;
  price: number;
  isActive: boolean;
  tennisClubId: string;
}

export interface CreateLessonRequest {
  date: string;
  startTime: string;
  courtName: string;
  notes?: string;
}

export const LESSON_LEVELS: Record<number, string> = {
  1: "Beginner",
  2: "Gevorderd beginner",
  3: "Intermediate",
  4: "Gevorderd",
  5: "Expert",
};

export async function getLessonSeries(): Promise<LessonSeriesDto[]> {
  const { data } = await apiClient.get<LessonSeriesDto[]>("/lessonseries");
  return data;
}

export async function getLessonSeriesById(id: string): Promise<LessonSeriesDto> {
  const { data } = await apiClient.get<LessonSeriesDto>(`/lessonseries/${id}`);
  return data;
}

export async function getOrganizationMembers(): Promise<OrgMemberDto[]> {
  const { data } = await apiClient.get<OrgMemberDto[]>("/lessonseries/members");
  return data;
}

export async function createLessonSeries(request: CreateLessonSeriesRequest): Promise<string> {
  const { data } = await apiClient.post<string>("/lessonseries", request);
  return data;
}

export async function updateLessonSeries(id: string, request: UpdateLessonSeriesRequest): Promise<LessonSeriesDto> {
  const { data } = await apiClient.put<LessonSeriesDto>(`/lessonseries/${id}`, request);
  return data;
}

export async function deleteLessonSeries(id: string): Promise<void> {
  await apiClient.delete(`/lessonseries/${id}`);
}

export async function createLesson(seriesId: string, request: CreateLessonRequest): Promise<string> {
  const { data } = await apiClient.post<string>(`/lessonseries/${seriesId}/lessons`, request);
  return data;
}

export async function deleteLesson(seriesId: string, lessonId: string): Promise<void> {
  await apiClient.delete(`/lessonseries/${seriesId}/lessons/${lessonId}`);
}
