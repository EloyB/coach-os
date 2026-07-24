import apiClient from '@/lib/api-client';
import type { LessonDto } from './lessonSeries';

export interface PublicLessonSeriesDto {
  id: string;
  name: string;
  description: string | null;
  trainerName: string;
  level: number;
  price: number;
  startDate: string;
  endDate: string;
  durationMinutes: number;
  tennisClubName: string;
  enrollmentCount: number;
  maxRegistrations: number | null;
  lessons: LessonDto[];
}

export interface FormFieldDto {
  id: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options: string[] | null;
}

export interface EnrollmentFormDto {
  id: string;
  lessonSeriesId: string;
  fields: FormFieldDto[];
}

export interface EnrollmentResponseItem {
  fieldLabel: string;
  value: string;
}

export interface LessonSeriesEnrollmentDto {
  id: string;
  studentName: string;
  studentEmail: string | null;
  contactEmail: string;
  hasOwnEmail: boolean;
  status: string;
  enrolledAt: string;
  notes: string | null;
  /** yyyy-MM-dd, null voor inschrijvingen van vóór de tariefcategorieën. */
  dateOfBirth: string | null;
  /** 1 = volwassenen, 2 = jeugd, null = onbekend. */
  category: number | null;
  categoryLabel: string | null;
  formResponses: EnrollmentResponseItem[];
}

export interface SaveFormFieldRequest {
  id?: string;
  label: string;
  type: number;
  isRequired: boolean;
  order: number;
  options?: string[];
}

export interface TimeSlotPreferenceRequest {
  weeklyTemplateEntryId: string;
  preference: number; // 1=Available, 2=Preferred, 3=Unavailable
}

export interface GroupMemberRequest {
  studentName: string;
  /** Weglaten of null = communicatie loopt via de groepsleider. */
  studentEmail?: string | null;
  studentPhone?: string;
  /** yyyy-MM-dd — verplicht, bepaalt het tarief (volwassene/jeugd). */
  dateOfBirth: string;
  responses: { formFieldId: string; value: string }[];
}

export interface SubmitEnrollmentRequest {
  studentName: string;
  studentEmail: string;
  studentPhone?: string;
  /** yyyy-MM-dd — verplicht, bepaalt het tarief (volwassene/jeugd). */
  dateOfBirth: string;
  responses: { formFieldId: string; value: string }[];
  timeSlotPreferences?: TimeSlotPreferenceRequest[];
  enrollmentType?: string; // "solo" | "group"
  isOpenToGrouping?: boolean;
  groupMembers?: GroupMemberRequest[];
}

export async function getPublicLessonSeries(id: string): Promise<PublicLessonSeriesDto> {
  const { data } = await apiClient.get<PublicLessonSeriesDto>(`/public/lessonseries/${id}`);
  return data;
}

export async function getEnrollmentForm(seriesId: string): Promise<EnrollmentFormDto | null> {
  const { data } = await apiClient.get<EnrollmentFormDto | null>(`/public/lessonseries/${seriesId}/form`);
  return data;
}

export async function saveEnrollmentForm(seriesId: string, fields: SaveFormFieldRequest[]): Promise<string> {
  const { data } = await apiClient.put<string>(`/lessonseries/${seriesId}/form`, { fields });
  return data;
}

export async function submitEnrollment(seriesId: string, request: SubmitEnrollmentRequest): Promise<string> {
  const { data } = await apiClient.post<string>(`/public/lessonseries/${seriesId}/enroll`, request);
  return data;
}

export async function getLessonSeriesEnrollments(seriesId: string): Promise<LessonSeriesEnrollmentDto[]> {
  const { data } = await apiClient.get<LessonSeriesEnrollmentDto[]>(`/lessonseries/${seriesId}/enrollments`);
  return data;
}

/**
 * Annuleert een inschrijving. De backend doet een soft-cancel: de status wordt
 * "Cancelled" en de plaats komt vrij, maar formulierantwoorden en planning-historiek
 * blijven bewaard.
 */
export async function cancelEnrollment(seriesId: string, enrollmentId: string): Promise<void> {
  await apiClient.delete(`/lessonseries/${seriesId}/enrollments/${enrollmentId}`);
}
