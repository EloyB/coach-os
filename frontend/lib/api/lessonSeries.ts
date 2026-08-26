import apiClient from "@/lib/api-client";

export interface LessonDto {
  id: string;
  lessonSeriesId: string;
  trainerId?: string | null;
  date: string;
  startTime: string;
  endTime: string;
  courtName: string;
  maxStudents: number;
  notes?: string;
  isCancelled: boolean;
  cancellationReason?: string | null;
  /** Het weekslot waaruit deze les komt; null voor losse lessen. */
  weeklyTemplateEntryId?: string | null;
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
  registrationDeadline?: string;
  durationMinutes: number;
  isActive: boolean;
  tennisClubId: string;
  tennisClubName: string;
  tennisClubAddress: string;
  lessonCount: number;
  enrolledCount: number;
  totalCapacity: number;
  minAge: number;
  maxAge: number;
  createdAt: string;
  lessons: LessonDto[];
  allowSoloEnrollment: boolean;
  allowGroupEnrollment: boolean;
  acceptOnlinePayment: boolean;
  acceptManualPayment: boolean;
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
  allowSoloEnrollment: boolean;
  allowGroupEnrollment: boolean;
  acceptOnlinePayment: boolean;
  acceptManualPayment: boolean;
}

export interface UpdateLessonSeriesRequest {
  name: string;
  description?: string;
  price: number;
  isActive: boolean;
  tennisClubId: string;
  registrationDeadline?: string;
  minAge: number;
  maxAge: number;
  allowSoloEnrollment: boolean;
  allowGroupEnrollment: boolean;
  acceptOnlinePayment: boolean;
  acceptManualPayment: boolean;
}

export interface CreateLessonRequest {
  trainerId?: string | null;
  date: string;
  startTime: string;
  /** Verplicht in de backend (formaat HH:mm) — zonder eindtijd faalt de validatie. */
  endTime: string;
  courtName?: string;
  maxStudents: number;
  level?: number | null;
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

export async function exportSeriePlanning(seriesId: string): Promise<Blob> {
  const { data } = await apiClient.get(`/lessonseries/${seriesId}/planning/export`, {
    responseType: "blob",
  });
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

export interface AddWeeklyTemplateEntryRequest {
  /** 0 = maandag … 6 = zondag. */
  dayOfWeek: number;
  startTime: string;
  /** Verplicht in de backend (formaat HH:mm). */
  endTime: string;
  trainerId?: string | null;
  courtName?: string;
  maxStudents: number;
  level?: number | null;
}

/**
 * Voegt een wekelijks terugkerend weekslot toe aan een reeks. De backend maakt de weekindeling-entry
 * (zichtbaar in de planning) én genereert de concrete lesmomenten vanaf vandaag tot de einddatum.
 */
export async function addWeeklyTemplateEntry(
  seriesId: string,
  request: AddWeeklyTemplateEntryRequest,
): Promise<string> {
  const { data } = await apiClient.post<string>(`/lessonseries/${seriesId}/weekly-template`, request);
  return data;
}

export interface UpdateLessonRequest {
  trainerId?: string | null;
  date?: string;
  startTime?: string;
  endTime?: string;
  courtName?: string;
  maxStudents?: number;
  notes?: string;
  isCancelled?: boolean;
  cancellationReason?: string;
  /** "slot" past tijd/trainer/baan/capaciteit toe op het hele weekslot; "lesson" (of leeg) enkel deze les. */
  applyTo?: "lesson" | "slot";
}

export async function updateLesson(seriesId: string, lessonId: string, request: UpdateLessonRequest): Promise<LessonDto> {
  const { data } = await apiClient.patch<LessonDto>(`/lessonseries/${seriesId}/lessons/${lessonId}`, request);
  return data;
}

export async function deleteLesson(
  seriesId: string,
  lessonId: string,
  wholeSlot = false,
): Promise<void> {
  const qs = wholeSlot ? "?applyTo=slot" : "";
  await apiClient.delete(`/lessonseries/${seriesId}/lessons/${lessonId}${qs}`);
}

/** Verwijdert een weekslot rechtstreeks uit de weektemplate (incl. lessen/voorkeuren/voorgestelde toewijzingen). */
export async function deleteWeekSlot(
  seriesId: string,
  weeklyTemplateEntryId: string,
): Promise<void> {
  await apiClient.delete(`/lessonseries/${seriesId}/weekly-template/${weeklyTemplateEntryId}`);
}

// ─── Wizard API ───────────────────────────────────────────────────────────────

export interface WizardSlotRequest {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  trainerId?: string;
  courtName?: string;
  maxStudents: number;
}

export interface LessonRequest {
  date: string; // ISO date
  startTime: string;
  endTime: string;
  trainerId?: string;
  courtName?: string;
  maxStudents: number;
  level?: number;
}

export interface CreateLessonSeriesWizardRequest {
  name: string;
  price: number;
  maxRegistrations: number;
  minAge: number;
  maxAge: number;
  tennisClubId: string;
  startDate: string;
  endDate: string;
  registrationDeadline: string;
  weeklyTemplate: WizardSlotRequest[];
  lessons: LessonRequest[];
  allowSoloEnrollment: boolean;
  allowGroupEnrollment: boolean;
  acceptOnlinePayment: boolean;
  acceptManualPayment: boolean;
}

export async function createLessonSeriesWizard(
  request: CreateLessonSeriesWizardRequest
): Promise<string> {
  const { data } = await apiClient.post<string>("/lessonseries", request);
  return data;
}
