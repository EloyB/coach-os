import apiClient from '@/lib/api-client';
import publicApiClient from '@/lib/public-api-client';
import type { LessonDto } from './lessonSeries';
import type { LessonSeriePriceDto } from './lessonSeriePrices';

export interface PublicLessonSeriesDto {
  id: string;
  name: string;
  description: string | null;
  trainerName: string;
  level: number | null;
  price: number;
  startDate: string;
  endDate: string;
  durationMinutes: number;
  tennisClubName: string;
  enrollmentCount: number;
  maxRegistrations: number | null;
  minAge: number;
  maxAge: number;
  allowSoloEnrollment: boolean;
  allowGroupEnrollment: boolean;
  priceOptions: LessonSeriePriceDto[];
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
  studentPhone: string | null;
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
  /** Null = solo-inschrijving; anders de groep waartoe deze inschrijving hoort. */
  enrollmentGroupId: string | null;
  /** True als deze inschrijving de groepsleider is (draagt de gedeelde betaling). */
  isGroupLeader: boolean;
  isOpenToGrouping: boolean;
  /** Gekozen prijsoptie (null = geen/legacy). */
  selectedPriceOptionId: string | null;
  formResponses: EnrollmentResponseItem[];
}

export interface UpdateBasicEnrollmentRequest {
  studentName: string;
  contactEmail: string;
  studentEmail?: string | null;
  studentPhone?: string | null;
  dateOfBirth: string;
  isOpenToGrouping: boolean;
  /** Nieuwe prijsoptie (weglaten = ongemoeid). */
  selectedPriceOptionId?: string | null;
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
  selectedPriceOptionId?: string;
  groupMembers?: GroupMemberRequest[];
}

export async function getPublicLessonSeries(id: string): Promise<PublicLessonSeriesDto> {
  const { data } = await publicApiClient.get<PublicLessonSeriesDto>(`/public/lessonseries/${id}`);
  return data;
}

export async function getEnrollmentForm(seriesId: string): Promise<EnrollmentFormDto | null> {
  const { data } = await publicApiClient.get<EnrollmentFormDto | null>(`/public/lessonseries/${seriesId}/form`);
  return data;
}

export async function saveEnrollmentForm(seriesId: string, fields: SaveFormFieldRequest[]): Promise<string> {
  const { data } = await apiClient.put<string>(`/lessonseries/${seriesId}/form`, { fields });
  return data;
}

export async function submitEnrollment(seriesId: string, request: SubmitEnrollmentRequest): Promise<string> {
  const { data } = await publicApiClient.post<string>(`/public/lessonseries/${seriesId}/enroll`, request);
  return data;
}

export async function getLessonSeriesEnrollments(seriesId: string): Promise<LessonSeriesEnrollmentDto[]> {
  const { data } = await apiClient.get<LessonSeriesEnrollmentDto[]>(`/lessonseries/${seriesId}/enrollments`);
  return data;
}

/** Tijdslot-voorkeur: 1 = Beschikbaar, 2 = Voorkeur, 3 = Niet beschikbaar. */
export interface EnrollmentTimeSlotPreferenceDto {
  weeklyTemplateEntryId: string;
  preference: number;
}

export interface EnrollmentWithPreferencesDto {
  id: string;
  studentName: string;
  studentEmail: string | null;
  status: string;
  isOpenToGrouping: boolean;
  enrollmentGroupId: string | null;
  groupName: string | null;
  isGroupLeader: boolean;
  preferences: EnrollmentTimeSlotPreferenceDto[];
}

/** Inschrijvingen mét hun tijdslot-voorkeuren (admin) — voor de detail-weergave. */
export async function getEnrollmentsWithPreferences(
  seriesId: string,
): Promise<EnrollmentWithPreferencesDto[]> {
  const { data } = await apiClient.get<EnrollmentWithPreferencesDto[]>(
    `/lessonseries/${seriesId}/enrollments/planning`,
  );
  return data;
}

export async function updateBasicEnrollment(
  seriesId: string,
  enrollmentId: string,
  request: UpdateBasicEnrollmentRequest,
): Promise<LessonSeriesEnrollmentDto> {
  const { data } = await apiClient.put<LessonSeriesEnrollmentDto>(
    `/lessonseries/${seriesId}/enrollments/${enrollmentId}`,
    request,
  );
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

/** Annuleert een volledige groep in één atomaire backend-transactie (alles-of-niets). */
export async function cancelEnrollmentGroup(seriesId: string, groupId: string): Promise<void> {
  await apiClient.delete(`/lessonseries/${seriesId}/enrollment-groups/${groupId}`);
}

/**
 * Markeert de openstaande overschrijving van een reeksinschrijving als betaald.
 * Bevestigt de inschrijving en verstuurt de bevestigingsmail. Faalt met NotFound
 * als er geen openstaande cash-betaling is voor deze inschrijving.
 */
export async function markEnrollmentCashPaid(enrollmentId: string): Promise<void> {
  await apiClient.post(`/enrollments/${enrollmentId}/mark-cash-paid`);
}
